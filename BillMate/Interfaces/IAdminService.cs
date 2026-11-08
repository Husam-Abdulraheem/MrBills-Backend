using BillMate.DTOs;

namespace BillMate.Interfaces
{
    public interface IAdminService
    {
        public Task<ResponseDTO> ViewGroups();
        public Task<ResponseDTO> Subscriptions();
        public Task<ResponseDTO> SendEmailForInActiveSubscriptions();
    }
}
