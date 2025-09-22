using IMS.Models;
using IMS.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IMS.Repositories.Interfaces
{
    public interface IDeliveryChallanRepository
    {
        Task<DeliveryChallan> CreateAsync(DeliveryChallanViewModel model);
        Task UpdateAsync(DeliveryChallan challan, List<DeliveryItemViewModel> items);
        Task<bool> DeleteAsync(int id);
        Task<DeliveryChallan> GetAsync(int id);
        Task<(IEnumerable<DeliveryChallanViewModel> Challans, int TotalItems)>
     GetChallansAsync(int page, int pageSize, int userId, string searchTerm = "",string role="");
        Task<bool> ExistsAsync(int id);
        Task<string> GenerateChallanNumberAsync();

    }
}
