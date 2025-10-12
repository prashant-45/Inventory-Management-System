using AutoMapper;
using IMS.Data;
using IMS.Models;
using IMS.Models.DTO;
using IMS.Repositories.Interfaces;
using IMS.Services;
using IMS.Services.IMS.Services;
//using IMS.Services.pdf;
using IMS.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace IMS.Repositories
{
    public class DeliveryChallanRepository : IDeliveryChallanRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IChallanPdfService _challanPdf;
        private readonly IWhatsAppService _whatsAppService;
        private readonly IMapper _mapper;


        public DeliveryChallanRepository(ApplicationDbContext context, IChallanPdfService challanPdf, IWhatsAppService whatsAppService, IMapper mapper)
        {
            _context = context;
            _challanPdf = challanPdf;
            _whatsAppService = whatsAppService;
            _mapper = mapper;
        }

        //public async Task<DeliveryChallan> CreateAsync(DeliveryChallanViewModel model)
        //{
        //    using var transaction = await _context.Database.BeginTransactionAsync();

        //    try
        //    {
        //        var challan = new DeliveryChallan
        //        {
        //            Date = model.Date,
        //            ReceiverName = model.ReceiverName,
        //            ReceiverMobile = model.ReceiverPhone,
        //            ChallanNo = model.ChallanNumber,
        //            createdBy= (int)model.createdBy,
        //            Items = model.Items.Select(i => new DeliveryChallanItem
        //            {
        //                Particular = i.Particulars,
        //                ModelNo = i.ModelNo,
        //                Quantity = i.Quantity,
        //                Remarks = i.Remarks,
        //                //Unit = i.UOM
        //            }).ToList()
        //        };

        //        _context.DeliveryChallans.Add(challan);
        //        await _context.SaveChangesAsync();



        //        var challanDto = new DeliveryChallanDto
        //        {
        //            Id = challan.Id,
        //            ChallanNo = challan.ChallanNo,
        //            ReceiverName = challan.ReceiverName,
        //            ReceiverMobile = challan.ReceiverMobile,
        //            Date = challan.Date,
        //            createdByName=model.createdByName,
        //            Items = challan.Items.Select(i => new DeliveryChallanItemDto
        //            {
        //                Particular = i.Particular,
        //                ModelNo = i.ModelNo,
        //                Quantity = i.Quantity,
        //                Remarks = i.Remarks
        //            }).ToList()
        //        };
        //        //🔹 Generate PDF here
        //        var pdfPath = await _challanPdf.GenerateChallanPdfAsync(challanDto);


        //        var queueMessage = new WhatsAppQueue
        //        {
        //            FkChallanId = challan.Id,
        //            MobileNumber = _whatsAppService.FormatPhoneNumber(challan.ReceiverMobile),
        //            Message = $"Hello {challan.ReceiverName}, your Delivery Challan {challan.ChallanNo} has been created.",
        //            Status = "PENDING",
        //            CreatedAt = DateTime.UtcNow
        //        };

        //        _context.WhatsappMessageQueue.Add(queueMessage);
        //        await _context.SaveChangesAsync();

        //        await transaction.CommitAsync();
        //        return challan;
        //    }
        //    catch
        //    {
        //        await transaction.RollbackAsync();
        //        throw;
        //    }
        //}

        public async Task<DeliveryChallan> CreateAsync(DeliveryChallanViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Map ViewModel -> Entity
                var challan = _mapper.Map<DeliveryChallan>(model);
                challan.createdBy = (int)model.createdBy;
                // Optionally, set other fields like CreatedAt
                // challan.CreatedAt = DateTime.UtcNow;

                _context.DeliveryChallans.Add(challan);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return challan;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task EnqueueWhatsAppMessageAsync(DeliveryChallan challan, string pdfUrl)
        {
            if (string.IsNullOrWhiteSpace(challan.ReceiverMobile))
                return; // skip if no mobile number

            var queueMessage = new WhatsAppQueue
            {
                FkChallanId = challan.Id,
                MobileNumber = _whatsAppService.FormatPhoneNumber(challan.ReceiverMobile),
                Message = $"Hello {challan.ReceiverName},\n\n" +
                          $"Thanks for choosing KAVA | Home Automation | Home Theatre | Security & more.\n\n" +
                          $"Please click the link below to download your delivery receipt for the delivered waybill(s):\n{pdfUrl}\n\n" +
                          $"We appreciate your trust and business!",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };

            _context.WhatsappMessageQueue.Add(queueMessage);
            await _context.SaveChangesAsync();
        }


        public async Task<bool> DeleteAsync(int id)
        {
            var challan = await _context.DeliveryChallans
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (challan == null)
                return false;

            _context.DeliveryChallanItems.RemoveRange(challan.Items);
            _context.DeliveryChallans.Remove(challan);

            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<(IEnumerable<DeliveryChallanViewModel> Challans, int TotalItems)>
     GetChallansAsync(int? page, int? pageSize, int userId, string searchTerm = "", string role = "")
        {
            var query = from c in _context.DeliveryChallans
                        join u in _context.Users
                            on c.createdBy equals u.Id into userGroup
                        from u in userGroup.DefaultIfEmpty() // Left join
                        select new { c, CreatedByName = u.FullName };

            // ✅ Filter by role
            if (!string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.c.createdBy == userId);
            }

            // 🔍 Search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(x =>
                    x.c.ChallanNo.Contains(searchTerm) ||
                    x.c.ReceiverName.Contains(searchTerm) ||
                    x.c.BranchName.Contains(searchTerm) ||
                    x.c.ReceiverMobile.Contains(searchTerm));
            }

            var totalItems = await query.CountAsync();

            var challans = await query
                .OrderByDescending(x => x.c.Date)
                .Skip((int)((page - 1) * pageSize))
                .Take((int)pageSize)
                .Select(x => new DeliveryChallanViewModel
                {
                    Id = x.c.Id,
                    ChallanNumber = x.c.ChallanNo,
                    Date = x.c.Date,
                    ReceiverName = x.c.ReceiverName,
                    ReceiverPhone = x.c.ReceiverMobile,
                    createdByName = x.CreatedByName,
                    Items = x.c.Items.Select(i => new DeliveryItemViewModel
                    {
                        ModelNo = i.ModelNo,
                        Particulars = i.Particular,
                        Quantity = i.Quantity,
                        Remarks = i.Remarks
                    }).ToList()
                })
                .ToListAsync();

            return (challans, totalItems);
        }

        public async Task<IEnumerable<DeliveryChallanViewModel>> GetChallansForDashboardAsync(int userId, string role)
        {
            var query = from c in _context.DeliveryChallans
                        join u in _context.Users on c.createdBy equals u.Id into userGroup
                        from u in userGroup.DefaultIfEmpty()
                        select new { c, CreatedByName = u.FullName };

            if (!string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
                query = query.Where(x => x.c.createdBy == userId);

            var challans = await query
                .OrderByDescending(x => x.c.Date)
                .Select(x => new DeliveryChallanViewModel
                {
                    Id = x.c.Id,
                    ChallanNumber = x.c.ChallanNo,
                    Date = x.c.Date,
                    ReceiverName = x.c.ReceiverName,
                    ReceiverPhone = x.c.ReceiverMobile,
                    createdByName = x.CreatedByName,
                    Items = x.c.Items.Select(i => new DeliveryItemViewModel
                    {
                        ModelNo = i.ModelNo,
                        Particulars = i.Particular,
                        Quantity = i.Quantity,
                        Remarks = i.Remarks
                    }).ToList()
                })
                .ToListAsync();

            return challans;
        }

        public async Task<DeliveryChallan?> GetAsync(int id)
        {
            var challan = await _context.DeliveryChallans
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (challan != null)
            {
                // Example: Ensure nullable navigation properties
                challan.Items ??= new List<DeliveryChallanItem>();
            }

            return challan;

        }

        public async Task UpdateAsync(DeliveryChallan challan, List<DeliveryItemViewModel> items)
        {
            // Attach header without touching navigation properties
            var existingChallan = await _context.DeliveryChallans.FindAsync(challan.Id);
            if (existingChallan == null) return;

            // Update header fields only
            existingChallan.ChallanNo = challan.ChallanNo;
            existingChallan.ReceiverName = challan.ReceiverName;
            existingChallan.ReceiverMobile = challan.ReceiverMobile;
            existingChallan.Date = challan.Date;
            existingChallan.UpdatedAt = DateTime.Now;
            existingChallan.updatedBy = challan.updatedBy;

            // Handle items
            var existingItems = await _context.DeliveryChallanItems
                .Where(i => i.Fk_deliveryChallanId == challan.Id)
                .ToListAsync();

            // Update or add
            foreach (var itemVm in items)
            {
                if (itemVm.Id == 0)
                {
                    // New item
                    var newItem = new DeliveryChallanItem
                    {
                        Fk_deliveryChallanId = challan.Id,
                        ModelNo = itemVm.ModelNo,
                        Particular = itemVm.Particulars,
                        Quantity = itemVm.Quantity ?? 1,
                        Remarks = itemVm.Remarks
                    };
                    _context.DeliveryChallanItems.Add(newItem);
                }
                else
                {
                    // Existing item
                    var existing = existingItems.FirstOrDefault(i => i.Id == itemVm.Id);
                    if (existing != null)
                    {
                        existing.ModelNo = itemVm.ModelNo;
                        existing.Particular = itemVm.Particulars;
                        existing.Quantity = itemVm.Quantity ?? 1;
                        existing.Remarks = itemVm.Remarks;
                    }
                }
            }

            // Remove deleted
            var itemIds = items.Select(i => i.Id).ToList();
            var toRemove = existingItems.Where(i => !itemIds.Contains(i.Id)).ToList();
            _context.DeliveryChallanItems.RemoveRange(toRemove);

            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.DeliveryChallans.AnyAsync(c => c.Id == id);
        }

        public async Task<string> GenerateChallanNumberAsync()
        {
            var year = DateTime.Now.Year;

            var lastChallan = await _context.DeliveryChallans
                .Where(c => c.Date.Year == year)
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastChallan != null)
            {
                var lastNo = lastChallan.ChallanNo.Split('-').Last();
                if (int.TryParse(lastNo, out int lastSeq))
                {
                    nextNumber = lastSeq + 1;
                }
            }

            return $"CH-{year}-{nextNumber:D6}";
        }
    }
}
