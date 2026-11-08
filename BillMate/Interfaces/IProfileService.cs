using BillMate.DTOs;
using Microsoft.AspNetCore.JsonPatch;

namespace BillMate.Interfaces
{
    public interface IProfileService
    {

        public Task<ResponseDTO> GetProfileAsync(string userId);
        public Task<ResponseDTO> UpdateProfile(string userId, JsonPatchDocument<UpdateUserDTO> dto);
        public Task<ResponseDTO> ExitGroup(string userId);

    }
}
