using BillMate.Data;
using BillMate.DTOs;
using BillMate.Interfaces;
using BillMate.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace BillMate.Services
{
    public class ProfileService : IProfileService
    {
        private readonly ApplicationDbContext _db;
        public ProfileService(ApplicationDbContext db)
        {
            _db = db;
        }


        public async Task<ResponseDTO> GetProfileAsync(string userId)
        {
            if (userId == null) return new ResponseDTO { Message = "User not found" };

            var user = await _db.User.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return new ResponseDTO { Message = "Not in Auth" };

            return new ResponseDTO
            {
                Success = true,
                Data = new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.Iban
                }
            };

        }


        public async Task<ResponseDTO> UpdateProfile(string userId, JsonPatchDocument<UpdateUserDTO> dto)
        {
            try
            {
                if(dto == null || dto.Operations.Count == 0)
                    return new ResponseDTO { Success = false, Message = "Invalid patch document" };


                var user = await _db.User.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return new ResponseDTO { Message = "Can't find this user" };

               
          
                // First applay data in DTO before applay Patch
                var data = new UpdateUserDTO
                {
                  FullName = user.FullName,
                  Iban = user.Iban,
                };

                //Applay Patch to DTO
                dto.ApplyTo(data);


                _db.Entry(user).CurrentValues.SetValues(data);

                // Save Changes
                await _db.SaveChangesAsync();
                return new ResponseDTO { Success = true, Message = "Update user successful" };
            }
            catch (Exception ex)
            {
                return new ResponseDTO { Success = false, Message = $"Error occurred while updating profile: {ex.Message}" };
            }
        }


        public async Task<ResponseDTO> ExitGroup(string userId)
        {
            try
            {
                var groupUser = await _db.GroupUsers.Include(u => u.User).FirstOrDefaultAsync(u => u.UserId == userId);

                if (groupUser == null)
                    return new ResponseDTO { Message = "User is not in any group.", Success = true };

                var currentGroup = await _db.Groups.FirstOrDefaultAsync(g=>g.Id == groupUser.User.GroupId);

                if (currentGroup == null)
                    return new ResponseDTO { Message = "The group does not exist." };

                if(currentGroup.ManagerId == userId)
                    return new ResponseDTO { Message = "You cannot leave as a group manager." };

                var debts = await _db.Debts.AsNoTracking().Where(d => d.Status && (d.ToUserId == userId || d.FromUserId == userId)).ToListAsync();

                if (debts.Count != 0)
                    return new ResponseDTO { Message = "You can't exitfrom this group while you still have unpaid bills" };

                _db.GroupUsers.Remove(groupUser);
                groupUser.User.GroupId = null;

                await _db.SaveChangesAsync();
                return new ResponseDTO { Success = true, Message = "You have successfully exited the group." };

            }
            catch (Exception ex)
            {

                return new ResponseDTO { Success = false, Message = $"Error occurred while Exit Group: {ex.Message}" };
            }
        }


    }
}
