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
        Task<IEnumerable<DeliveryChallanViewModel>> GetAllChallansAsync();
        Task<bool> ExistsAsync(int id);
        
    }
}
