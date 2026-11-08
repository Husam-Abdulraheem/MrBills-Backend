using BillMate.DTOs;

namespace BillMate.Interfaces
{
    public interface INotificationService
    {
        Task SendNotificationAsync(string userId, string title, string message, string? link = null);
        Task<ResponseDTO> CreateBillNotification(string userId, CreateBillDTO billDTO, int billId);
        Task<ResponseDTO> GetMyNotifications(string userId);
        Task<ResponseDTO> BroadcastNotification(BroadcastNotificationDTO broadcast);
    }
}
