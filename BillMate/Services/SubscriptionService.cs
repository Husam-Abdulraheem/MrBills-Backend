using BillMate.Data;
using BillMate.DTOs;
using BillMate.Interfaces;
using BillMate.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace BillMate.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IGroupService _groupService;
        private readonly ApplicationDbContext _db;
        private readonly IGooglePlayBillingService _googlePlayBillingService;
        private readonly IShopierService _shopierService;

        public SubscriptionService(IGroupService groupService, ApplicationDbContext db, IGooglePlayBillingService googlePlayBillingService, IShopierService shopierService)
        {
            _groupService = groupService;
            _db = db;   
            _googlePlayBillingService = googlePlayBillingService;
            _shopierService = shopierService;
        }



        public async Task<ResponseDTO> CreateSubscription(SubscriptionDTO dto, string userId)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {

                var group = await _groupService.CreateGroup(userId, dto.Package);

                if (!group.Success)
                {
                    await transaction.RollbackAsync();
                    return new ResponseDTO { Message = "Cannot create new group." };
                }
                int groupId = (int)group.Data;


                ResponseDTO result;

                switch (dto.Type)
                {
                    case SubscriptionType.Free:
                        result = await ProcessFreePackage(groupId, dto);
                        break;

                    case SubscriptionType.Paid:
                        result = await ProcessPaidPackage(groupId, dto, userId);
                        break;

                    default:
                        return new ResponseDTO { Success = false, Message = "Invalid subscription type." };
                }

                if (!result.Success)
                {
                    await transaction.RollbackAsync();
                    return new ResponseDTO { Success = false, Message = $"Failed to process subscription: {result.Message}" };
                }


                await transaction.CommitAsync();

                return new ResponseDTO { Message = result.Message, Success = true, HtmlContent = result.HtmlContent };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResponseDTO { Message = $"Error: {ex.Message}" };
            }
        }






        private async Task<ResponseDTO> ProcessFreePackage(int groupId, SubscriptionDTO dto)
        {
            try
            {
                var subscription = new Subscription()
                {
                    GroupId = groupId,
                    StartTime = DateTime.UtcNow,
                    Type = SubscriptionType.Free,
                    Package = SubscriptionPackage.Small_3Users,
                    Period = null,
                    PaymentMethod = null,
                    ExpiryTime= null,
                    IsActive= true,
                    IsTrial= false,
                    
                };

                await _db.Subscriptions.AddAsync(subscription);
                await _db.SaveChangesAsync();

                return new ResponseDTO { Success = true };
            }
            catch (Exception ex)
            {
                return new ResponseDTO { Success = false, Message = ex.Message };
            }
        }




        private async Task<ResponseDTO> ProcessPaidPackage(int groupId, SubscriptionDTO dto, string userId)
        {
            try
            {
                
                switch (dto.PaymentMethod)
                {
                    case PaymentMethod.GooglePlay:
                        var result = await _googlePlayBillingService.ProcessGooglePlayPurchaseAsync(groupId, dto, userId);
                        if (!result.Success)
                            return new ResponseDTO { Message = "Error while processed with Google Play" };
                        return new ResponseDTO { Success = true, Message = "Processed with Google Play" };

                    case PaymentMethod.Shopier:
                        var formModel = _shopierService.GenerateShopierForm(groupId, dto, userId);
                        if (formModel == null)
                            return new ResponseDTO { Message = "Error while processed with Shopier" };
                        return new ResponseDTO
                        {
                            Success = true,
                            Message = "Shopier form generated",
                            HtmlContent = formModel
                        };

                    default:
                        return new ResponseDTO { Success = false, Message = "Unsupported payment method." };

                }

            }
            catch (Exception ex)
            {
                return new ResponseDTO { Success = false, Message = ex.Message };
            }
        }











        public async Task<ResponseDTO> RenewSubscription(string userId, SubscriptionDTO dto)
        {


            var subscription = await _db.Subscriptions.Include(g => g.Group).Where(s => s.Group.ManagerId == userId).FirstOrDefaultAsync();

            if (subscription == null) return new ResponseDTO { Message = "You don't have a subscription." };

            if (subscription.IsActive && subscription.ExpiryTime > DateTime.UtcNow)
                return new ResponseDTO { Message = "Your subscription is still active." };

            if (!subscription.IsActive)
            {
                if (subscription.PaymentMethod == PaymentMethod.Shopier)
                {


                    dto.Package = subscription.Package;
                    dto.Period = subscription.Period;

                    var formModel = _shopierService.GenerateShopierForm((int)subscription.GroupId, dto, userId);

                    _db.Subscriptions.UpdateRange(subscription);
                    await _db.SaveChangesAsync();
                    return new ResponseDTO
                    {
                        Success = true,
                        Message = "Shopier form generated",
                        HtmlContent = formModel
                    };

                }
                else if (subscription.PaymentMethod == PaymentMethod.GooglePlay)
                {
                    if (dto.PurchaseToken == null || dto.SubscriptionId == null)
                        return new ResponseDTO { Message = "Invalid Data" };

                    var isValid = await _googlePlayBillingService.RenewGooglePlaySubscriptionAsync(userId, dto.SubscriptionId, dto.PurchaseToken);

                    if (!isValid.Success)
                    {
                        return new ResponseDTO { Success = false, Message = "Failed to renew via Google Play." };
                    }

                    return new ResponseDTO
                    {
                        Success = true,
                        Message = "Subscription renewed via Google successfully."
                    };

                }
            }

            return new ResponseDTO { Success = false, Message = "Cannot renew subscription." };
        }





        public async Task CheckAndUpdateSubscriptionsAsync()
        {
            var expiredSubscriptions = await _db.Subscriptions.Where(s => s.ExpiryTime < DateTime.UtcNow && s.IsActive).ToListAsync();

            foreach (var sub in expiredSubscriptions)
            {
                sub.IsActive = false;
            }
            await _db.SaveChangesAsync();


        }




        public async Task<ResponseDTO> CheckSubscriptionForManager(string userId)
        {
            try
            {
                var sub = await _db.Subscriptions.Include(g => g.Group).Where(s => s.Group.ManagerId == userId).FirstOrDefaultAsync();

                if (sub == null) return new ResponseDTO { Message = "no subscription", Success = true };

                if (!sub.IsActive || sub.ExpiryTime < DateTime.UtcNow) return new ResponseDTO { Message = "Subscription not active", Success = true};

                return new ResponseDTO { Message = "You have active subscription", Success = true };

            }
            catch (Exception)
            {

                throw;
            }
        }



    }
}
