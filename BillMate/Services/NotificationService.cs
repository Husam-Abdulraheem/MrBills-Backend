using BillMate.Data;
using BillMate.DTOs;
using BillMate.Hubs;
using BillMate.Interfaces;
using BillMate.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BillMate.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHubContext<NotificationHub> _hub;
        public NotificationService(ApplicationDbContext db, IHubContext<NotificationHub> hub)
        {
            _db = db;
            _hub = hub;
        }
        public async Task SendNotificationAsync(string userId, string title, string message, string? link = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Link = link,
                CreatedAt = DateTime.Now,
                IsRead = false,
                Type = NotificationType.Bill
            };

            _db.Notification.Add(notification);
            await _db.SaveChangesAsync();

            await _hub.Clients.User(userId).SendAsync(" ", new
            {
                title,
                message,
                link,
                createdAt = notification.CreatedAt
            });

        }


        public async Task<ResponseDTO> CreateBillNotification(string userId, CreateBillDTO billDTO, int billId)
        {
            foreach(var user in billDTO.BillUsersId)
            {
                if(user != userId)
                {
                await SendNotificationAsync(
                    user,
                    "📄 فاتورة جديدة",
                    $"تمت إضافتك إلى فاتورة '{billDTO.Description}'",
                    $"/bills/{billId}"


                    );
                }
            }
            return new ResponseDTO { Success = true };
        }



        public async Task<ResponseDTO> GetMyNotifications(string userId)
        {
            try
            {
                if(userId == null)
                    return new ResponseDTO { Success = false };

                var notifications = await _db.Notification.Where(u=> u.UserId == userId || u.UserId == null).OrderByDescending(u=>u.CreatedAt).ToListAsync();

                return new ResponseDTO { Success = true , Data= notifications};
            }
            catch (Exception ex)
            {

                throw new Exception("Error while get my notifications" + ex);
            }
        }



        public async Task<ResponseDTO> BroadcastNotification(BroadcastNotificationDTO broadcast)
        {
            try
            {
                if (broadcast.Title == null && broadcast.Message == null)
                    return new ResponseDTO { Success = false, Message= "There is missing values" };

                var allUsersId = await _db.User.Select(u=> u.Id).ToListAsync();

                var notification = allUsersId.Select(userId => new Notification
                {
                    UserId = userId,
                    CreatedAt = DateTime.Now,
                    Title = broadcast.Title,
                    Message= broadcast.Message,
                    Link = broadcast.Link,
                    IsRead = false,
                    Type = NotificationType.General
                });

                _db.Notification.AddRange(notification);
                await _db.SaveChangesAsync();
                
                return new ResponseDTO { Success = true };
            }
            catch (Exception ex)
            {

                throw new Exception("Error while Broadcast notification" + ex);

            }
        }
    }
}
