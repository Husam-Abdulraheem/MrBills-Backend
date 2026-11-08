using BillMate.Data;
using BillMate.DTOs;
using BillMate.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BillMate.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _db;
        public AdminService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ResponseDTO> ViewGroups()
        {
            var query = await _db.Subscriptions
         .Select(s => new
         {
             s.Id, // SubscriptionId
             s.IsActive,
             s.ExpiryTime,
             s.ReceiptUrl,
             s.Type,
             Group = new
             {
                 s.Group.Id,
                 s.Group.Code,
                 s.Group.MaxMembers,
                 FullNames = string.Join(", ", s.Group.Members.Select(gu => gu.User.FullName))
             }
         })
         .ToListAsync();


            return new ResponseDTO { Data = query, Success = true };


        }

        public async Task<ResponseDTO> Subscriptions()
        {
            // Get the subscriptions for users and groups
            var subscriptions = await _db.Subscriptions
                .Include(s => s.Group)
                    .ThenInclude(g => g.Members)
                        .ThenInclude(gu => gu.User)
                .ToListAsync();

            // Get all Ids for managers
            var managerIds = subscriptions
                .Select(s => s.Group?.ManagerId)
                .Where(id => id != null)
                .Distinct()
                .ToList();

            // Get all managers data in one request
            var managers = await _db.User
                .Where(u => managerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u);

            // Get all group IDs
            var groupIds = subscriptions.Select(s => s.GroupId).Distinct().ToList();

            // Get bill counts for each group
            var billCounts = await _db.Bills
                .Where(b => groupIds.Contains(b.GroupId))
                .GroupBy(b => b.GroupId)
                .Select(g => new { GroupId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.GroupId, g => g.Count);

            // the results
            var result = subscriptions.Select(s => new AdminSubscriptionDTO
            {
                SubscriptionId = s.Id,
                GroupId = s.GroupId,
                Package = s.Package?.ToString(),
                Type = s.Type.ToString(),
                StartDate = s.StartTime,
                EndDate = s.ExpiryTime,
                IsActive = s.IsActive,
                DaysLeft = s.ExpiryTime.HasValue
                    ? Math.Max(0, (int)(s.ExpiryTime.Value - DateTime.UtcNow).TotalDays)
                    : 0,
                Users = s.Group?.Members?.Select(gu => new UserInfoDTO
                {
                    FullName = gu.User?.FullName,
                    Email = gu.User?.Email
                }).ToList() ?? new List<UserInfoDTO>(),
                Creator = s.Group != null && managers.ContainsKey(s.Group.ManagerId) ? new CreatorInfoDTO
                {
                    FullName = managers[s.Group.ManagerId].FullName,
                    Email = managers[s.Group.ManagerId].Email
                } : null,
                NumberOfBills = (s.GroupId.HasValue && billCounts.ContainsKey(s.GroupId.Value)) ? billCounts[s.GroupId.Value] : 0
            }).ToList();

            return new ResponseDTO { Data = result, Success = true };
        }





        public async Task<ResponseDTO> SendEmailForInActiveSubscriptions()
        {
            var sevenDaysAgo = DateTime.UtcNow.Date.AddDays(-7);

            var users = await (from s in _db.Subscriptions.AsNoTracking()
                               join g in _db.Groups.AsNoTracking() on s.GroupId equals g.Id
                               join u in _db.Users.AsNoTracking() on s.GroupId equals u.GroupId
                               where s.IsActive == false
                                     && s.ExpiryTime < sevenDaysAgo
                                     && u.Id == g.ManagerId
                               select new
                               {
                                   u.Email,
                                   u.FullName
                               }).ToListAsync();

            return new ResponseDTO { Data = users.Select(u => u.Email).ToList(), Success = true };
        }




    }
}
