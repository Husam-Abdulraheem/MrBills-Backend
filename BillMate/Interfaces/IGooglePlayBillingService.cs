using BillMate.DTOs;
using BillMate.DTOs.PubSub;
using Google.Apis.AndroidPublisher.v3.Data;

namespace BillMate.Interfaces
{
    public interface IGooglePlayBillingService
    {
        Task<SubscriptionPurchase> VerifySubscriptionAsync
            (string subscriptionId, string purchaseToken);

        Task HandleNotificationAsync(GooglePlayNotification notification);
        Task<ResponseDTO> ProcessGooglePlayPurchaseAsync(int groupId, SubscriptionDTO dto, string userId);
        Task<ResponseDTO> RenewGooglePlaySubscriptionAsync(string userId, string subscriptionId, string purchaseToken);



    }
}
