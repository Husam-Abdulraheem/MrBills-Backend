using BillMate.DTOs;
using BillMate.Models;

namespace BillMate.Interfaces
{
    public interface IGroupService
    {
        Task<ResponseDTO> GetGroupAsync(string userId);
        Task<ResponseDTO> CreateGroup(string userId, SubscriptionPackage? package);
        Task<ResponseDTO> JoinGroup(string userId, string code);
        Task<ResponseDTO> KickGroupMember(string userId, string kickedMember);
        public Task<ResponseDTO> Subscription(string userId);

    }
}
