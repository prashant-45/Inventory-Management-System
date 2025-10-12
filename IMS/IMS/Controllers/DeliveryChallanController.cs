using AutoMapper;
using IMS.Data;
using IMS.Models;
using IMS.Models.DTO;
using IMS.Repositories;
using IMS.Repositories.Interfaces;
using IMS.Services;
using IMS.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;

namespace IMS.Controllers
{
    public class DeliveryChallanController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IDeliveryChallanRepository _deliveryChallanRepo;
        private readonly IChallanPdfService _challanPdf;
        private readonly IUserRepository _userRepo;

        public DeliveryChallanController(IMapper mapper, IDeliveryChallanRepository deliveryChallanRepo,
            IChallanPdfService challanPdfService, IUserRepository userRepository)
        {
            _mapper = mapper;
            _deliveryChallanRepo = deliveryChallanRepo;
            _challanPdf = challanPdfService;
            _userRepo = userRepository;
        }

        public async Task<IActionResult> Index(int? page = 1, int? pageSize = 10, string? searchTerm = "")
        {
            // Call repo method (already handles paging + search)
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var (challans, totalItems) = await _deliveryChallanRepo.GetChallansAsync(page, pageSize, Convert.ToInt32(userId), searchTerm, role);

            // Compute pagination metadata
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.StartRecord = (page - 1) * pageSize + 1;
            ViewBag.EndRecord = Math.Min((byte)(page * pageSize), totalItems);
            ViewBag.SearchTerm = searchTerm;

            // ✅ Pass success message to view
            if (TempData["SuccessMessage"] != null)
                ViewBag.SuccessMessage = TempData["SuccessMessage"].ToString();

            return View(challans);
        }

        // GET: DeliveryChallan/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var challanNo = await _deliveryChallanRepo.GenerateChallanNumberAsync(); // system generated

            var model = new DeliveryChallanViewModel
            {
                ChallanNumber = challanNo,
                Date = DateTime.Now,
                Items = new List<DeliveryItemViewModel>
                {
                    new DeliveryItemViewModel() // at least one item row
                }
            };

            return View(model);
        }

        // POST: DeliveryChallan/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DeliveryChallanViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = User.FindFirst(ClaimTypes.Name)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    // User not logged in or claim missing
                    throw new Exception("User ID not found in claims.");
                }

                int uId = Convert.ToInt32(userId);

                var user = _userRepo.GetUserByUserId(uId);

                model.createdByName = userName;
                model.createdBy = Convert.ToInt32(userId);
                model.BranchName = user?.BranchName;
                var challan = await _deliveryChallanRepo.CreateAsync(model);

                // 2️⃣ Generate PDF (outside transaction)
                var challanDto = _mapper.Map<DeliveryChallanDto>(challan);
                //dto.createdByName = createdByName;
                var pdfPath = await _challanPdf.GenerateChallanPdfAsync(challanDto);
                string pdfUrl = _challanPdf.GetPublicPdfUrl(pdfPath);

                // Enqueue WhatsApp message with link
                await _deliveryChallanRepo.EnqueueWhatsAppMessageAsync(challan, pdfUrl);


                TempData["SuccessMessage"] = $"Challan #{challan.ChallanNo} created successfully! ✅";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                return View(model);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var challan = await _deliveryChallanRepo.GetAsync(id);

            if (challan == null)
            {
                return NotFound();
            }

            var viewModel = _mapper.Map<DeliveryChallanViewModel>(challan); // if AutoMapper is used
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var challan = await _deliveryChallanRepo.GetAsync(id);
            if (challan == null)
            {
                return NotFound();
            }

            var viewModel = _mapper.Map<DeliveryChallanViewModel>(challan);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DeliveryChallanViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var entity = await _deliveryChallanRepo.GetAsync(id); // include items
            if (entity == null)
            {
                return NotFound();
            }

            // Map scalar props (challan header)
            //_mapper.Map(model, entity);

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int uId = Convert.ToInt32(userId);

                var user = _userRepo.GetUserByUserId(uId);

                entity.BranchName = user?.BranchName;

                // Let repo handle updating child items
                await _deliveryChallanRepo.UpdateAsync(entity, model.Items);

                // 2️⃣ Generate PDF (outside transaction)
                var challanDto = _mapper.Map<DeliveryChallanDto>(entity);
                //dto.createdByName = createdByName;
                var pdfPath = await _challanPdf.GenerateChallanPdfAsync(challanDto);
                string pdfUrl = _challanPdf.GetPublicPdfUrl(pdfPath);

                // Enqueue WhatsApp message with link
                await _deliveryChallanRepo.EnqueueWhatsAppMessageAsync(entity, pdfUrl);

                // Store message in TempData so it survives redirect
                TempData["SuccessMessage"] = "✅ Delivery Challan updated successfully!";

                // Redirect to Index (listing/entry page)
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _deliveryChallanRepo.ExistsAsync(id))
                {
                    return NotFound();
                }
                ModelState.AddModelError("", "Unable to update record.");
                return View(model);
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var challan = await _deliveryChallanRepo.GetAsync(id);
            if (challan == null)
            {
                return NotFound();
            }
                
            await _deliveryChallanRepo.DeleteAsync(challan.Id);
            return RedirectToAction(nameof(Index));
        }

    }
}
