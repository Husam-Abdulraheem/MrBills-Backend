using BillMate.DTOs;
using BillMate.Models;

namespace BillMate.Interfaces
{
    public interface IShopierService
    {
        public string GenerateShopierForm(int groupId, SubscriptionDTO data, string userId);
        Task<bool> ProcessCallbackAsync(ShopierCallbackDTO dto);
        Task<bool> ProcessOsbCallbackAsync(ShopierOsbData data);
    }
}
