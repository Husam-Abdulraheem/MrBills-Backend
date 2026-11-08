using BillMate.DTOs;

namespace BillMate.Interfaces
{
    public interface ISubscriptionService
    {

        public Task<ResponseDTO> CreateSubscription(SubscriptionDTO dto, string userId);
        public Task<ResponseDTO> RenewSubscription(string userId, SubscriptionDTO dto);
        public Task CheckAndUpdateSubscriptionsAsync();

        public Task<ResponseDTO> CheckSubscriptionForManager(string userId);



    }
}
