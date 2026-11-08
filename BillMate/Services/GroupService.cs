using BillMate.Data;
using BillMate.DTOs;
using BillMate.Interfaces;
using BillMate.Models;
using Microsoft.EntityFrameworkCore;

namespace BillMate.Services
{
    public class GroupService : IGroupService
    {
        private readonly ApplicationDbContext _db;

        public GroupService(ApplicationDbContext db)
        {
            _db = db;
        }


        public async Task<ResponseDTO> GetGroupAsync(string userId)
        {

            try
            {
                if (userId == null)
                    return new ResponseDTO { Message = "User not found." };

                var group = await _db.Groups.Include(g => g.Members).ThenInclude(gu => gu.User).FirstOrDefaultAsync(g => g.Members.Any(u => u.UserId == userId));

                if (group == null)
                    return new ResponseDTO { Message = "This user does not have a group." };

                var sub = await _db.Subscriptions.Where(s => s.GroupId == group.Id).FirstOrDefaultAsync();

                if (sub == null)
                    return new ResponseDTO { Message = "This user does not have a group." };


                var result = new
                {
                    group.Id,
                    group.ManagerId,
                    code = group.ManagerId == userId ? group.Code : null,
                    Members = group.Members.Select(m => new
                    {
                        m.UserId,
                        m.User?.FullName,
                    }).ToList(),
                    sub.IsActive,
                };

                return new ResponseDTO { Success = true, Data = result };
            }
            catch (Exception ex)
            {

                return new ResponseDTO { Success = false, Message = $"Error occurred while Getting Group: {ex.Message}" };
            }
        }
        



        public async Task<ResponseDTO> CreateGroup(string? userId, SubscriptionPackage? package)
        {
           
            if (userId == null)
                return new ResponseDTO { Message = "Invalid user id." };

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return new ResponseDTO { Message = "User not found." };

            if (user.GroupId != null && user.GroupId != 0)
                return new ResponseDTO { Message = "This user already have a group" };

            int maxMember = package switch
            {
                SubscriptionPackage.Small_3Users => 3,
                SubscriptionPackage.Medium_5Users => 5,
                SubscriptionPackage.Large_7Users => 7,
                _ => 0
            };

            if (maxMember == 0)
                return new ResponseDTO { Message = "Invalid subscription package"};

            try
            {

                var code = GenerateGroupCode();
                var group = new Group
                {
                    Code = code,
                    ManagerId = userId,
                    MaxMembers = maxMember
                };

                await _db.Groups.AddAsync(group);
                await _db.SaveChangesAsync();

                var groupUser = new GroupUser
                {

                    UserId = userId,
                    GroupId = group.Id,
                    JoinedAt = DateTime.UtcNow,

                };
                user.GroupId = group.Id;

                await _db.GroupUsers.AddAsync(groupUser);
                await _db.SaveChangesAsync();



                return new ResponseDTO { Success = true, Message = "Created successfully", Data = group.Id };
            }
            catch (Exception ex)
            {



                return new ResponseDTO { Message = $"Error occurred while Creating Group: {ex.Message}" };
            
            }
        }




        public async Task<ResponseDTO> JoinGroup(string? userId, string code)
        {
            if (userId == null || code == null)
                return new ResponseDTO
                {
                    Message = userId == null ? "User not found." : "Code not found"
                };

            // Get data from group
            var groupData = await
                (from g in _db.Groups
                 where g.Code == code
                 select new
                 {
                     Group = g,
                     MembersCount = g.Members.Count
                 }).FirstOrDefaultAsync();

            if (groupData == null) return new ResponseDTO { Message = "Group not found", Success = false };

            if (groupData.Group.MaxMembers <= groupData.MembersCount)
                return new ResponseDTO { Message = "The group is full, you cannot join." };

            // Get user data from db
            var user = await _db.User.Where(u => u.Id == userId).Select(u => new
            {
                u.Id,
                u.GroupId,
            }).FirstOrDefaultAsync();

            if (user == null) return new ResponseDTO { Message = "User not found", Success = false };

            if (user.GroupId.HasValue) return new ResponseDTO { Message = "User already in another group", Success = false };

            // Save Changes in db
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var userEntity = await _db.Users.FindAsync(userId);
                userEntity.GroupId = groupData.Group.Id;

                _db.GroupUsers.Add(new GroupUser
                {
                    GroupId = groupData.Group.Id,
                    UserId = userId,
                    JoinedAt = DateTime.UtcNow,
                });

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                return new ResponseDTO { Message = "Error joining group" };
            }

            return new ResponseDTO { Success = true, Message = "Joined successfully" };
        }







        public async Task<ResponseDTO> KickGroupMember(string userId, string memberId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(memberId))
                return new ResponseDTO { Message = "Invalid input parameters" };

            // Get group info 
            var group = await _db.Groups.FirstOrDefaultAsync(g=>g.ManagerId == userId);

            if (group == null) return new ResponseDTO { Message = "You are not a manager of any group" };

            // Get member data form db
            var userToRemove = await _db.GroupUsers.Include(u=>u.User).FirstOrDefaultAsync(u => u.UserId == memberId && u.GroupId == group.Id);

            // Check if user in the same group
            if (userToRemove == null)
                return new ResponseDTO { Message = "User not found in your group"};

            // Check if the user you want to remove does not manager
            if (userToRemove.UserId == userId)
                return new ResponseDTO { Message = "You cannot remove yourself as the manager of the group."};

            var debts = await _db.Debts.AsNoTracking().Where(d=> d.Status && ( d.ToUserId == userToRemove.UserId || d.FromUserId == userToRemove.UserId)).ToListAsync();

            if (debts != null)
                return new ResponseDTO { Message = "You can't remove a member while they still have unpaid bills" };

            var bills =await _db.Bills.Include(d=>d.Debts).Where(b=>b.OwnerId == memberId).ToListAsync();
            // Save data in db
            using (var transaction = await _db.Database.BeginTransactionAsync())
            {
                try
                {
                    userToRemove.User.GroupId = null;
                    _db.GroupUsers.Remove(userToRemove);

                    _db.Bills.RemoveRange(bills);

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new ResponseDTO { Success = true, Message = "Member successfully removed from the group." };
                }
                catch (Exception ex)
                {

                    await transaction.RollbackAsync();
                    return new ResponseDTO { Message = $"An error occurred while removing the member. {ex.Message }" };
                }
            }
        }



        public async Task<ResponseDTO> Subscription(string userId)
        {
            if (userId == null) return new ResponseDTO { Message = "User not found." };

            var group = await _db.Groups.AsNoTracking().FirstOrDefaultAsync(u => u.ManagerId == userId);

            if (group == null)
                return new ResponseDTO { Message = "Group not Found or user not Auth" };

            var subscription = await _db.Subscriptions.AsNoTracking().FirstOrDefaultAsync(s => s.GroupId == group.Id);

            if (subscription == null)
                return new ResponseDTO { Message = "You are not Manager" };



            var timeLeft = subscription.ExpiryTime - DateTime.UtcNow;
            var daysLeft = timeLeft.HasValue ? Math.Max(timeLeft.Value.Days, 0) : 0;

            return new ResponseDTO { Success = true, Data = new {  daysLeft, subscription.Package, subscription.IsActive, subscription.PaymentMethod, subscription.ExpiryTime, subscription.Type, subscription.PriceAmount } };
        }





        private static string GenerateGroupCode()
        {
            int length = 6;
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
        .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
