using BillMate.Data;
using BillMate.DTOs;
using BillMate.Interfaces;
using BillMate.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace BillMate.Services
{
    public class BillService : IBillService
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileService _fileService;
        private readonly INotificationService _notificationService;
        private readonly ILogger _logger;
        private readonly IWebHostEnvironment _env;

        public BillService(ApplicationDbContext db, IFileService fileService, INotificationService notificationService, ILogger<BillService> logger, IWebHostEnvironment env)
        {
            _db = db;
            _fileService = fileService;
            _notificationService = notificationService;
            _logger = logger;
            _env = env;
        }

        public async Task<ResponseDTO> CreateBill(string userId, CreateBillDTO billDTO)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Check data not null
                if (billDTO == null || billDTO.BillUsersId == null || !billDTO.BillUsersId.Any() || billDTO.Total <= 0)
                    return new ResponseDTO { Message = "Invalid input. Please provide valid data." };
                
      
                var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u=>u.Id == userId);

                if (user == null)
                    return new ResponseDTO { Message = "User not found", Success = false };

                if (!user.GroupId.HasValue)
                    return new ResponseDTO { Message = "User doesn't belong to a group", Success = false };

                var invalidMembers = billDTO.BillUsersId.Where(m=> !_db.User.AsNoTracking().Any(u=>u.Id == m)).ToList();

                if (invalidMembers.Any())
                    return new ResponseDTO{ Message = $"Invalid member IDs: {string.Join(", ", invalidMembers)}"};


                var subscription = await _db.Subscriptions
                    .Where(s => s.GroupId == user.GroupId && s.IsActive)
                    .OrderByDescending(s => s.StartTime)
                    .FirstOrDefaultAsync();

                if (subscription == null)
                    return new ResponseDTO { Success = false, Message = "No active subscription found." };

                if (subscription.Type == SubscriptionType.Free)
                {
                    var weekAgo = DateTime.UtcNow.AddDays(-7);

                    var invoicesLast7Days = await _db.Bills
                        .Where(i => i.GroupId == user.GroupId && i.CreateDate >= weekAgo)
                        .CountAsync();

                    if (invoicesLast7Days >= 5)
                    {
                        return new ResponseDTO
                        {
                            Success = false,
                            Message = "You can only create 5 invoices per week on the free plan."
                        };
                    }
                }

                string? invoicePath = null;
                if (billDTO.Invoice != null && billDTO.Invoice.Length > 0)
                {


                    var savedResult = await _fileService.ProcessReceipt(billDTO.Invoice, "invoices");

                    if (!savedResult.Success)
                        return new ResponseDTO { Message = savedResult.Message, Error = savedResult.Error };
                    invoicePath = savedResult.Data.ToString();
                }

                

                // create new Bill 
                var createBill = new Bill()
                {
                    OwnerId = userId,
                    GroupId = (int)user.GroupId,
                    CreateDate = DateTime.UtcNow,
                    Total = MathF.Round(billDTO.Total, 1),
                    Description = billDTO.Description,
                    RequiredReceipt = billDTO.RequiredReceipt,
                    Invoice = invoicePath
                };

                if (billDTO.BillUsersId.Count == 1 && billDTO.BillUsersId.Contains(userId))
                {
                    createBill.ArchivedAt = DateTime.UtcNow;
                    createBill.IsArchived = true;
                }

                // Save Bill in Database
                await _db.Bills.AddAsync(createBill);
                await _db.SaveChangesAsync();

                // What is share foreach member
                float share = billDTO.Total / billDTO.BillUsersId.Count;

                // Add new Debt foreach one
                var NewDebt = new List<Debt>();
                foreach (var debt in billDTO.BillUsersId)
                {
                    NewDebt.Add(new Debt
                    {
                        FromUserId = userId,
                        ToUserId = debt,
                        BillId = createBill.Id,
                        Status = debt == userId,
                        Amount = share,
                    });
                }
                // Save Debts in Database
                await _db.Debts.AddRangeAsync(NewDebt);
                await _db.SaveChangesAsync();


                await _notificationService.CreateBillNotification(userId, billDTO, createBill.Id);

                await transaction.CommitAsync();

                return new ResponseDTO { Message = "Bill created successfully", Success = true };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResponseDTO
                {
                    Message = "Error processing request",
                    Error = ex.Message,
                    Success = false
                };
            }
        }



        //ViewBillsICreated
        public async Task<ResponseDTO> MyBills(string userId)
        {
            try
            {
                var bills = await _db.Bills.Where(b => b.OwnerId == userId && _db.Debts.Any(d=>d.BillId == b.Id && !d.Status))
                    .Select(bill => new BillDTO
                    {
                        BillId = bill.Id,
                        OwnerName = bill.Owner.FullName,
                        Description = bill.Description,
                        CreateDate = TimeZoneInfo.ConvertTimeFromUtc(bill.CreateDate, TimeZoneInfo.Local),
                        Total = MathF.Round(bill.Total, 1),
                        ActivityLogs = _db.ActivityLogs.Where(a=>a.BillId == bill.Id).ToList(),
                        Claimants = _db.Debts
                        .Where(d=>d.BillId == bill.Id)
                        .Select(de => new ClaimantDTO
                        {
                            ClaimantId = de.ToUserId,
                            ClaimantName = de.ToUser.FullName,
                            Amount = MathF.Round(de.Amount),
                            IsPaid= de.Status,
                            Receipt = de.ReceiptUrl != null ? $"http://api.mister-bills.com/uploads/receipts/{de.ReceiptUrl}" : null
                        }).ToList()
                    }).ToListAsync();

               

                if (!bills.Any())
                {
                    return new ResponseDTO { Message = "You don't have any bills", Success = true };
                }
                return new ResponseDTO { Data= bills, Success = true};
            }
            catch (Exception ex)
            {

                throw new Exception("Error when get bills for user:" + userId + " => " + ex);
            }
        }



        public async Task<ResponseDTO> Bills(string userId)
        {
            try
            {
                var query =  from bill in _db.Bills
                            join debt in _db.Debts on bill.Id equals debt.BillId
                            where debt.ToUserId == userId && !debt.Status
                            select new DebtDTO
                            {
                                BillId = bill.Id,
                                Description = bill.Description,
                                BillOwner = bill.Owner.FullName,
                                Amount = debt.Amount,
                                Date = TimeZoneInfo.ConvertTimeFromUtc(bill.CreateDate, TimeZoneInfo.Local),
                                Iban = debt.FromUser.Iban,
                                IsRequired = bill.RequiredReceipt,
                                Invoice = bill.Invoice != null ? $"http://api.mister-bills.com/uploads/invoices/{bill.Invoice}" : null,
                            };

                var bills = await query.ToListAsync();
                var totalDebtsAmount = await query.SumAsync(b => b.Amount);
                    


                // Amount due
                var receivables = await _db.Debts.Where(d => d.FromUserId == userId && !d.Status).SumAsync(d => d.Amount);
                //receivables = MathF.Round(receivables, 1);

                if (!bills.Any())
                    return new ResponseDTO { Message = "No Bills", Success = true, Data = new { totalDebtsAmount = 0, receivables } };

                return new ResponseDTO { Data= new { bills, totalDebtsAmount, receivables } ,Success = true};
            }
            catch (Exception e)
            {

                throw new Exception("Error in get bills" + e);
            }
        }



        public async Task<ResponseDTO> EditBill(string userId, EditBillDTO editBillDTO)
        {
            using (var transaction = await _db.Database.BeginTransactionAsync())
            {
                try
                {

                    var currBill = await _db.Bills.Where(b => b.Id == editBillDTO.BillId && userId == b.OwnerId).FirstOrDefaultAsync();


                    if (currBill == null)
                        return new ResponseDTO { Message = "Can't find this bill" };

                    if (editBillDTO.Total <= 0)
                        return new ResponseDTO { Message = "Total amount must be greater than zero." };

                    bool hasPayments = await _db.ActivityLogs.AnyAsync(a => a.BillId == currBill.Id);
                    if (hasPayments)
                        return new ResponseDTO { Message = "Cannot modify the bill amount after payments have been made." };


                    // Debts in current Bill
                    var currDebt = await _db.Debts
                        .Where(d => d.BillId == currBill.Id)
                        .Include(d => d.ToUser)
                        .ToListAsync();

                    // Users in same group for current bill
                    var groupUsers = await _db.Users
                        .Where(u => u.GroupId == currBill.GroupId)
                        .ToListAsync();

                    // Update current bill data 
                    currBill.Description = editBillDTO.Description ?? currBill.Description;
                    currBill.Total = MathF.Round(editBillDTO.Total, 1);


                    // if there Claimants in Bill
                    if (editBillDTO.Claimants != null && editBillDTO.Claimants.Count > 0)
                    {
                        // 1) the Amount must be equal in all Claimants
                        int numberOfClaimants = editBillDTO.Claimants.Count;
                        float portion = numberOfClaimants > 0 ? editBillDTO.Total / numberOfClaimants : 0;


                        // Give all claimant the same amount
                        foreach (var claimant in editBillDTO.Claimants)
                        {
                            claimant.Amount = portion;
                        }

                        // 2) Edit Claimants in Debts
                        foreach (var claimant in editBillDTO.Claimants)
                        {
                            var existingDebt = currDebt
                                .FirstOrDefault(d => d.ToUser?.FullName == claimant.ClaimantName);

                            if (existingDebt == null)
                            {
                                // Chack if all Claimants in same group
                                var user = groupUsers
                                    .FirstOrDefault(u => u.FullName == claimant.ClaimantName);

                                if (user == null)
                                {
                                    return new ResponseDTO
                                    {
                                        Message = $"User: {claimant.ClaimantName} is not exists in this group.",
                                    };
                                }

                                // Add new Debt
                                var newDebt = new Debt
                                {
                                    BillId = currBill.Id,
                                    ToUserId = user.Id,
                                    ToUser = user,
                                    Amount = claimant.Amount,
                                    Status = user.Id == userId ? true : claimant.IsPaid,
                                };
                                await _db.Debts.AddAsync(newDebt);
                            }
                            else
                            {
                                    // Update Amount and status
                                existingDebt.Amount = claimant.Amount;
                                existingDebt.Status = existingDebt.ToUserId == currBill.OwnerId ? true : claimant.IsPaid;
                                
                            }
                        }

                        // 3) Remove non-existent debts
                        var removeDebts = currDebt
                            .Where(ed => !editBillDTO.Claimants
                                .Any(u => u.ClaimantName == ed.ToUser?.FullName))
                            .ToList();

                        _db.Debts.RemoveRange(removeDebts);
                    }
                    else
                    {
                        // If there are no more claims -> delete all debts
                        _db.Debts.RemoveRange(currDebt);
                        _db.Bills.Remove(currBill);
                    }

                    // Save Changes
                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return new ResponseDTO { Success = true, Message = "Edit bill successful" };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return new ResponseDTO { Message = $"Error occurred while editing bill: { ex.Message}"
                };
                }
            }
        }



        public async Task<ResponseDTO> PayBill(string userId, PayBillDTO dto)
        {
            try
            {
                if (userId == null || dto.BillId <= 0)
                    return new ResponseDTO { Message = "User or bill data is incorrect." };


                var debt = await _db.Debts.Include(u=>u.ToUser).FirstOrDefaultAsync(d => d.BillId == dto.BillId && d.ToUserId == userId);



                if (debt == null) return new ResponseDTO { Message = "Can not found debt with this user for this bill" };



                var bill = await _db.Bills.FirstOrDefaultAsync(b=>b.Id == dto.BillId);



                if (bill == null)
                    return new ResponseDTO { Message = "Bill not found" };



                if (debt.Status) return new ResponseDTO { Message = "This Debt has paid" };



                // Check Receipt Requir
                if (bill.RequiredReceipt && (dto.ReceiptFile == null || dto.ReceiptFile.Length == 0))
                    return new ResponseDTO { Message = "Receipt is required" };

                if (bill.RequiredReceipt)
                {
                    

                    var savedResult = await _fileService.ProcessReceipt(dto.ReceiptFile, "receipts");

                    if (!savedResult.Success)
                        return new ResponseDTO { Message = savedResult.Message, Error = savedResult.Error };

                    debt.Status = true;
                    debt.ReceiptUrl = savedResult.Data.ToString();
                }

                debt.Status = true;
                    
                


                //Add a record to the activity table
                var activity = new ActivityLog
                {
                    BillId = dto.BillId,
                    Name = debt.ToUser.FullName,
                    Amount = debt.Amount,
                    PaymentTime = DateTime.UtcNow,
                    PaymentType = "Paid"
                };

                await _db.ActivityLogs.AddAsync(activity);
                await _db.SaveChangesAsync();

                //Check if all debts have been paid
                bool allDebtsPaid = await _db.Debts
                    .Where(d => d.BillId == dto.BillId)
                    .AllAsync(d => d.Status);

                // Archive paid bills
                if (allDebtsPaid)
                {
                    bill.IsArchived = true;
                    bill.ArchivedAt = DateTime.UtcNow;
                }

                _db.Bills.Update(bill);
                await _db.SaveChangesAsync();
                return new ResponseDTO { Success = true };
            }
            catch (Exception ex)
            {
                return new ResponseDTO { Message = "Error when processing payment", Success = false, Error = ex.Message };
            }
        }

       


        public async Task<ResponseDTO> SettlementBill(string userId, SettlementDTO dto)
        {
            try
            {
                using var transaction = await _db.Database.BeginTransactionAsync();

                var debts = await _db.Debts
           .Where(d => (d.BillId == dto.OtherBillId && d.ToUserId == userId && !d.Status) ||
                       (d.BillId == dto.MyBillId && !d.Status))
           .ToListAsync();

                var myDebt = debts.FirstOrDefault(d => d.BillId == dto.OtherBillId && d.ToUserId == userId);
                if (myDebt == null)
                    return new ResponseDTO { Message = "There is no bill" };

                var otherDebt = debts.FirstOrDefault(d => d.BillId == dto.MyBillId && d.ToUserId == myDebt.FromUserId && d.FromUserId == myDebt.ToUserId);
                if (otherDebt == null)
                    return new ResponseDTO { Message = "There is no bill" };


                if (myDebt.FromUserId != otherDebt.ToUserId || myDebt.ToUserId != otherDebt.FromUserId)
                    return new ResponseDTO { Message = "Invalid debts for settlement" };

                if (otherDebt.Status || myDebt.Status)
                    return new ResponseDTO { Message = "One of these bills has already been paid" };

                float deductedAmount = MathF.Min(myDebt.Amount, otherDebt.Amount);

                myDebt.Amount -= deductedAmount;
                otherDebt.Amount -= deductedAmount;

                if (myDebt.Amount == 0) myDebt.Status = true;
                if (otherDebt.Amount == 0) otherDebt.Status = true;

                var user = await _db.User.FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return new ResponseDTO { Message = "User not found" };

                var activities = new List<ActivityLog>
                {
                    new ActivityLog
                    {
                        BillId = myDebt.BillId,
                        Name = user.FullName,
                        Amount = deductedAmount,
                        PaymentTime = DateTime.UtcNow,
                        PaymentType = "Settled"
                    },
                    new ActivityLog
                    {
                        BillId = otherDebt.BillId,
                        Name = user.FullName,
                        Amount = deductedAmount,
                        PaymentTime = DateTime.UtcNow,
                        PaymentType = "Settled"
                    },

                };

                await _db.ActivityLogs.AddRangeAsync(activities);
                _db.Debts.Update(myDebt);
                _db.Debts.Update(otherDebt);
                await _db.SaveChangesAsync();

                var bills = await _db.Bills.Where(b=>b.Id == dto.MyBillId || b.Id == dto.OtherBillId).ToListAsync();

                var myBill = bills.FirstOrDefault(b=>b.Id == dto.MyBillId);
                var otherBill = bills.FirstOrDefault(b=>b.Id == dto.OtherBillId);


                //Check if all debts have been paid
                bool allMyBillDebtsPaid = debts.Where(d => d.BillId == dto.MyBillId).All(d => d.Status);
                bool allOtherBillDebtsPaid = debts.Where(d => d.BillId == dto.OtherBillId).All(d => d.Status);



                if (allMyBillDebtsPaid && myBill != null && !myBill.IsArchived)
                {
                    myBill.IsArchived = true;
                    myBill.ArchivedAt = DateTime.UtcNow;
                }

                if (allOtherBillDebtsPaid && otherBill != null && !otherBill.IsArchived)
                {
                    otherBill.IsArchived = true;
                    otherBill.ArchivedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ResponseDTO { Success = true };
            }
            catch (Exception ex)
            {

                return new ResponseDTO { Success = false, Message = "An error occurred while processing the settlement. Please try again later." };
            }
        }
        



        public async Task<ResponseDTO> DeleteBill(int billId, string userId)
        {
            try
            {
                var deleteBill = await _db.Bills.Where(b=>b.Id == billId && b.OwnerId == userId).FirstOrDefaultAsync();
                if (deleteBill == null)
                    throw new Exception("Not find this Bill");

                var debts = await _db.Debts.Where(d => d.BillId == billId).ToListAsync();
                _db.Debts.RemoveRange(debts);
                 _db.Bills.Remove(deleteBill);
                await _db.SaveChangesAsync();
                return new ResponseDTO { Message = "successfully deleted", Success= true } ;

            }
            catch (Exception ex)
            {

                throw new Exception("Error when delete this bill:" + " => " + ex.Message);
            }
        }





        public async Task<ResponseDTO> ArchivedBills(string userId)
        {
            var archivedBills = await _db.Bills
                .Where(b => b.OwnerId == userId && b.IsArchived)
                .Select(b => new BillDTO
                {
                    BillId = b.Id,
                    Description = b.Description,
                    CreateDate = TimeZoneInfo.ConvertTimeFromUtc(b.CreateDate, TimeZoneInfo.Local),
                    Total = MathF.Round(b.Total),
                    ActivityLogs = _db.ActivityLogs.Where(a => a.BillId == b.Id).ToList(),
                    Claimants = b.Debts.Select(d => new ClaimantDTO
                    {
                        ClaimantId = d.ToUserId,
                        ClaimantName = d.ToUser.FullName,
                        Amount = MathF.Round(d.Amount),
                        IsPaid = d.Status,
                        Receipt = d.ReceiptUrl != null ?$"http://api.mister-bills.com/uploads/receipts/{d.ReceiptUrl}": null
                    }).ToList()
                })
                .ToListAsync();

            if (!archivedBills.Any())
            {
                return new ResponseDTO { Message = "No archived bills found", Success = true };
            }

            return new ResponseDTO { Data = archivedBills, Success = true };
        }



        public async Task CleanupArchivedBillsAsync()
        {
            var expirationDate = DateTime.UtcNow.AddDays(-30);

            var expiredBills = await _db.Bills
                .Where(b => b.IsArchived && b.ArchivedAt < expirationDate).Include(b=>b.Debts)
                .ToListAsync();

            if (!expiredBills.Any()) return;

            var expiredDebts = expiredBills.SelectMany(b => b.Debts).ToList();

            if (!expiredDebts.Any()) return;


            string baseUrl = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "receipts");
            foreach (var bill in expiredBills)
            {
                var receiptUrls = bill.Debts.Select(d=>d.ReceiptUrl).ToList();
                foreach (var url in receiptUrls)
                {

                    if (string.IsNullOrEmpty(url)) continue;

                    string fullUrl = Path.Combine(baseUrl, url);

                    if (File.Exists(fullUrl))
                    {
                        try
                        {
                            File.Delete(fullUrl);
                            _logger.LogInformation($"Deleted receipt: {fullUrl}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error deleting bill receipt {bill.Id}: {ex.Message}");
                        }
                    }
                }
            }

            _db.Debts.RemoveRange(expiredDebts); // Remove all debts at first
            _db.Bills.RemoveRange(expiredBills);
            await _db.SaveChangesAsync();
        }




    }
}
