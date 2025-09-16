using AutoMapper;
using IMS.Data;
using IMS.Repositories.Interfaces;
using IMS.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace IMS.Controllers
{
    public class DeliveryChallanController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IDeliveryChallanRepository _deliveryChallanRepo;

        public DeliveryChallanController(IMapper mapper, IDeliveryChallanRepository deliveryChallanRepo)
        {
            _mapper = mapper;
            _deliveryChallanRepo = deliveryChallanRepo;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string searchTerm = "")
        {
            // Call repo method (already handles paging + search)
            var (challans, totalItems) = await _deliveryChallanRepo.GetChallansAsync(page, pageSize, searchTerm);

            // Compute pagination metadata
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.StartRecord = (page - 1) * pageSize + 1;
            ViewBag.EndRecord = Math.Min(page * pageSize, totalItems);
            ViewBag.SearchTerm = searchTerm;

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
                var challan = await _deliveryChallanRepo.CreateAsync(model);

                TempData["Success"] = "Challan created successfully!";
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
