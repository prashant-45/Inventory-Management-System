using AutoMapper;
using IMS.Data;
using IMS.Models.DTO;
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

        public DeliveryChallanController(IMapper mapper, IDeliveryChallanRepository deliveryChallanRepo,IChallanPdfService challanPdfService)
        {
            _mapper = mapper;
            _deliveryChallanRepo = deliveryChallanRepo;
            _challanPdf = challanPdfService;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string searchTerm = "")
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
            ViewBag.EndRecord = Math.Min(page * pageSize, totalItems);
            ViewBag.SearchTerm = searchTerm;

            //TempData["SuccessMessage"] = $"Challan created successfully! ✅";
            return View(challans);
        }

        // GET: DeliveryChallan/Create
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

                model.createdByName = userName;
                model.createdBy = Convert.ToInt32(userId);
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
            _mapper.Map(model, entity);

            try
            {
                // Let repo handle updating child items
                await _deliveryChallanRepo.UpdateAsync(entity, model.Items);
                TempData["Success"] = "✅ Delivery Challan updated successfully!";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _deliveryChallanRepo.ExistsAsync(id))
                {
                    return NotFound();
                }
                ModelState.AddModelError("", "Unable to update record.");
            }
            return View(model);

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
