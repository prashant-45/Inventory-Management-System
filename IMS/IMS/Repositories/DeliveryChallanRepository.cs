using IMS.Data;
using IMS.Models;
using IMS.Repositories.Interfaces;
using IMS.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IMS.Repositories
{
    public class DeliveryChallanRepository : IDeliveryChallanRepository
    {
        private readonly ApplicationDbContext _context;

        public DeliveryChallanRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DeliveryChallan> CreateAsync(DeliveryChallanViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var challan = new DeliveryChallan
                {
                    Date = model.Date,
                    ReceiverName = model.ReceiverName,
                    ReceiverMobile = model.ReceiverPhone,
                    ChallanNo = model.ChallanNumber,
                    Items = model.Items.Select(i => new DeliveryChallanItem
                    {
                        Particular = i.Particulars,
                        Quantity = i.Quantity,
                        Remarks = i.Remarks,    
                        //Unit = i.UOM
                    }).ToList()
                };

                _context.DeliveryChallans.Add(challan);
                await _context.SaveChangesAsync();

                //var queueMessage = new WhatsAppQueue
                //{
                //    MobileNumber = challan.ReceiverMobile,
                //    Message = $"Delivery Challan {challan.ChallanNo} created with {challan.Items.Count} items.",
                //    Status = "PENDING",
                //    CreatedAt = DateTime.UtcNow
                //};

                //_context.WhatsAppQueue.Add(queueMessage);
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


        public async Task<IEnumerable<DeliveryChallanViewModel>> GetAllChallansAsync()
        {
            return await _context.DeliveryChallans
                .Include(c => c.Items)
                .AsNoTracking()
                .Select(c => new DeliveryChallanViewModel
                {
                    Id = c.Id,
                    ChallanNumber = c.ChallanNo,
                    Date = c.Date,
                    ReceiverName = c.ReceiverName,
                    ReceiverPhone = c.ReceiverMobile,
                    Items = c.Items.Select(i => new DeliveryItemViewModel
                    {
                        ModelNo = i.ModelNo,
                        Particulars = i.Particular,
                        Quantity = i.Quantity,
                        //UOM = i.Unit,
                        Remarks = i.Remarks
                    }).ToList()
                })
                .ToListAsync();
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
            //foreach (var itemVm in items)
            //{
            //    if (itemVm.Id == 0)
            //    {
            //        // New item
            //        var newItem = new DeliveryChallanItem
            //        {
            //            Fk_deliveryChallanId = challan.Id,
            //            ModelNo = itemVm.ModelNo,
            //            Particular = itemVm.Particulars,
            //            Quantity = itemVm.Quantity ?? 1,
            //            Remarks = itemVm.Remarks
            //        };
            //        _context.DeliveryChallanItems.Add(newItem);
            //    }
            //    else
            //    {
            //        // Existing item
            //        var existing = existingItems.FirstOrDefault(i => i.Id == itemVm.Id);
            //        if (existing != null)
            //        {
            //            existing.ModelNo = itemVm.ModelNo;
            //            existing.Particular = itemVm.Particulars;
            //            existing.Quantity = itemVm.Quantity ?? 1;
            //            existing.Remarks = itemVm.Remarks;
            //        }
            //    }
            //}

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
    }
}
