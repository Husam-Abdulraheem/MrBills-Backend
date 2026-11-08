using BillMate.Data;
using BillMate.DTOs;
using BillMate.DTOs.PubSub;
using BillMate.Interfaces;
using BillMate.Models;
using Google.Apis.AndroidPublisher.v3;
using Google.Apis.AndroidPublisher.v3.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace BillMate.Services
{
    public class GooglePlayBillingService : IGooglePlayBillingService
    {
        private readonly AndroidPublisherService _service;
        private readonly string _packageName;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<GooglePlayBillingService> _logger;



        public GooglePlayBillingService(string serviceAccountJsonPath, string packageName, ApplicationDbContext db, ILogger<GooglePlayBillingService> logger)
        {
            _logger = logger;
            _db = db;
            _packageName = packageName;
            if (File.Exists(serviceAccountJsonPath))
            {
                try
                {
                    var credential = GoogleCredential.FromFile(serviceAccountJsonPath).CreateScoped(AndroidPublisherService.Scope.Androidpublisher);
                    _service = new AndroidPublisherService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = "Mr Bills",
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Google Play credentials could not be loaded. Google Play billing verification will be disabled.");
                }
            }
            else
            {
                _logger.LogWarning("Google Play Service Account file not found at: {Path}. Service is running in standalone mode.", serviceAccountJsonPath);
            }
        }




        public async Task<SubscriptionPurchase> VerifySubscriptionAsync(string subscriptionId, string purchaseToken)
        {
            var request = _service.Purchases.Subscriptions.Get(_packageName, subscriptionId, purchaseToken);

            try
            {
                var subscriptionPurchase = await request.ExecuteAsync();
                return subscriptionPurchase;
            }
            catch (Google.GoogleApiException apiEx)
            {
                // Handle errors returned by the Google API (e.g., invalid token, subscription not found)
                Console.Error.WriteLine($"Google API Error: {apiEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                // Handle any other errors.
                Console.Error.WriteLine($"General Error: {ex.Message}");
                throw;
            }
        }







        public async Task<ResponseDTO> ProcessGooglePlayPurchaseAsync(int groupId, SubscriptionDTO dto, string userId)
        {

            try
            {
                var subscriptionGoogleService = await VerifySubscriptionAsync(dto.SubscriptionId, dto.PurchaseToken);

                if (subscriptionGoogleService == null || subscriptionGoogleService.ExpiryTimeMillis == null)
                    return new ResponseDTO { Message = "Invalid or expired subscription.", Success = false };




                SubscriptionPackage package = 0;
                switch (dto.SubscriptionId)
                {
                    case "small_plan":
                        package = SubscriptionPackage.Small_3Users;
                        break;
                    case "medium_plan":
                        package = SubscriptionPackage.Medium_5Users;
                        break;
                    case "large_plan":
                        package = SubscriptionPackage.Large_7Users;
                        break;
                    default:
                        return new ResponseDTO { Message = "Invalid subscription ID.", Success = false };
                }


                var subscription = new Models.Subscription()
                {
                    IsTrial = subscriptionGoogleService.PaymentState == 2 && subscriptionGoogleService.PriceAmountMicros == 0,
                    IsActive = subscriptionGoogleService.PaymentState == 1 || subscriptionGoogleService.PaymentState == 2,
                    GroupId = groupId,
                    Package = package,
                    Type =  SubscriptionType.Paid,
                    PurchaseToken = dto.PurchaseToken,
                    SubscriptionId = dto.SubscriptionId,
                    PaymentMethod = PaymentMethod.GooglePlay,
                    OrderId = subscriptionGoogleService.OrderId,
                    CountryCode = subscriptionGoogleService.CountryCode,
                    PaymentState = subscriptionGoogleService.PaymentState,
                    CancelReason = subscriptionGoogleService.CancelReason,
                    AutoRenewing = subscriptionGoogleService.AutoRenewing,
                    PriceCurrencyCode = subscriptionGoogleService.PriceCurrencyCode,
                    PriceAmount = (decimal)subscriptionGoogleService.PriceAmountMicros.Value / 1000000,
                    StartTime = DateTimeOffset.FromUnixTimeMilliseconds((long)subscriptionGoogleService.StartTimeMillis).UtcDateTime,
                    ExpiryTime = DateTimeOffset.FromUnixTimeMilliseconds((long)subscriptionGoogleService.ExpiryTimeMillis).UtcDateTime,
                };


                await _db.Subscriptions.AddAsync(subscription);
                await _db.SaveChangesAsync();


                return new ResponseDTO { Message = "Process GooglePlay successfully", Success = true };

            }
            catch (Exception ex)
            {
                return new ResponseDTO { Message = $"Error: {ex.Message}" };
            }
        }









        public async Task<ResponseDTO> RenewGooglePlaySubscriptionAsync(string userId, string subscriptionId, string purchaseToken)
        {
            try
            {
                var subscription = await _db.Subscriptions
                                   .Include(s => s.Group)
                                   .FirstOrDefaultAsync(s => s.Group.ManagerId == userId && s.PaymentMethod == PaymentMethod.GooglePlay);

                var googleResponse = await VerifySubscriptionAsync(subscriptionId, purchaseToken);

                if (googleResponse == null || googleResponse.ExpiryTimeMillis == null)
                    return new ResponseDTO { Message = "Invalid or expired subscription.", Success = false };

                if (subscription == null)
                {
                    return new ResponseDTO
                    {
                        Success = false,
                        Message = "No existing subscription found for this user."
                    };
                }


                subscription.AutoRenewing = googleResponse.AutoRenewing;
                subscription.CancelReason = googleResponse.CancelReason;
                subscription.ExpiryTime = DateTimeOffset.FromUnixTimeMilliseconds((long)googleResponse.ExpiryTimeMillis).UtcDateTime;
                subscription.StartTime = DateTimeOffset.FromUnixTimeMilliseconds((long)googleResponse.StartTimeMillis).UtcDateTime;
                subscription.OrderId = googleResponse.OrderId;
                subscription.CountryCode = googleResponse.CountryCode;
                subscription.PaymentState = googleResponse.PaymentState;
                subscription.PriceCurrencyCode = googleResponse.PriceCurrencyCode;
                subscription.PriceAmount = (decimal)(googleResponse.PriceAmountMicros ?? 0) / 1_000_000;
                subscription.PurchaseToken = purchaseToken;
                subscription.SubscriptionId = subscriptionId;
                subscription.IsActive = subscription.ExpiryTime > DateTime.UtcNow && googleResponse.PaymentState == 1;

                await _db.SaveChangesAsync();

                return new ResponseDTO
                {
                    Success = true,
                    Message = "Google Play subscription renewed successfully."
 
                };



            }
            catch (Exception ex)
            {

                return new ResponseDTO { Success = false, Message = ex.Message };
            }
        }








        public async Task HandleNotificationAsync(GooglePlayNotification notification)
        {

            var subNotif = notification.SubscriptionNotification;

            if (subNotif == null)
            {
                // إشعار تجريبي أو غير مرتبط باشتراك فعلي
                _logger.LogInformation($"Received test or unknown notification: {subNotif}",
                    JsonSerializer.Serialize(notification));
                return; // لا شيء للمعالجة
            }

            var userSubscription = await _db.Subscriptions
                .FirstOrDefaultAsync(s => s.PurchaseToken == subNotif.PurchaseToken);

            if (userSubscription == null)
            {
                _logger.LogWarning("No local subscription found for token {PurchaseToken}", subNotif.PurchaseToken);
                return;
            }

            switch (subNotif.NotificationType)
            {
                case 1: // SUBSCRIPTION_RECOVERED
                    userSubscription.IsActive = true;
                    userSubscription.IsTrial = false;
                    _logger.LogInformation("Subscription {PurchaseToken} recovered.", subNotif.PurchaseToken);
                    break;

                case 2: // SUBSCRIPTION_RENEWED
                    var purchase = await VerifySubscriptionAsync(userSubscription.SubscriptionId, subNotif.PurchaseToken);

                    if (purchase == null)
                    {
                        _logger.LogWarning("Subscription purchase verification failed for token {PurchaseToken}", subNotif.PurchaseToken);
                        return;
                    }

                    var expiryTime = DateTimeOffset.FromUnixTimeMilliseconds(purchase.ExpiryTimeMillis.Value).UtcDateTime;

                    int? paymentState = purchase.PaymentState;
                    bool isPaid = paymentState == 1;

                    userSubscription.ExpiryTime = expiryTime;
                    userSubscription.IsActive = isPaid;
                    userSubscription.IsTrial = false;

                    _logger.LogInformation("Subscription {PurchaseToken} renewed. New expiry: {ExpiryTime}", subNotif.PurchaseToken, expiryTime);
                    break;

                case 3: // SUBSCRIPTION_CANCELED
                    userSubscription.IsActive = false;
                    _logger.LogInformation("Subscription {PurchaseToken} canceled.", subNotif.PurchaseToken);
                    break;

                case 4: // SUBSCRIPTION_PURCHASED
                    var newPurchase = await VerifySubscriptionAsync(userSubscription.SubscriptionId, subNotif.PurchaseToken);

                    if (newPurchase != null)
                    {
                        var newExpiry = DateTimeOffset.FromUnixTimeMilliseconds(newPurchase.ExpiryTimeMillis.Value).UtcDateTime;


                        if (!userSubscription.IsActive && userSubscription.PurchaseToken != subNotif.PurchaseToken)
                        {
                            userSubscription.PurchaseToken = subNotif.PurchaseToken;
                            userSubscription.ExpiryTime = newExpiry;
                            userSubscription.IsActive = true;
                            userSubscription.IsTrial = newPurchase.PaymentState == 0 && newPurchase.PriceAmountMicros == 0;

                            _logger.LogInformation($"Subscription {userSubscription.PurchaseToken} replaced old token. New expiry: {userSubscription.ExpiryTime}",
                                subNotif.PurchaseToken, newExpiry);
                        }

                    }
                    else
                    {
                        _logger.LogWarning("Failed to verify new purchase for token {PurchaseToken}", subNotif.PurchaseToken);
                    }
                    break;

                case 5: // SUBSCRIPTION_ON_HOLD
                    userSubscription.IsActive = false;
                    _logger.LogInformation("Subscription {PurchaseToken} put on hold.", subNotif.PurchaseToken);
                    break;

                default:
                    _logger.LogWarning("Unhandled subscription notification type: {Type}", subNotif.NotificationType);
                    break;
            }

            await _db.SaveChangesAsync();
        }



    }
}
