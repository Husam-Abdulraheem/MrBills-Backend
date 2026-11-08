using System;
using System.Collections.Generic;

namespace BillMate.DTOs
{
  public class AdminSubscriptionDTO
  {
    public int SubscriptionId { get; set; }
    public int? GroupId { get; set; }
    public string? Package { get; set; }
    public string Type { get; set; } // Free / Paid
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public int DaysLeft { get; set; }
    public int NumberOfBills { get; set; }
    public List<UserInfoDTO> Users { get; set; } = new();
    public CreatorInfoDTO Creator { get; set; }
  }

  public class UserInfoDTO
  {
    public string? FullName { get; set; }
    public string? Email { get; set; }
  }

  public class CreatorInfoDTO
  {
    public string? FullName { get; set; }
    public string? Email { get; set; }
  }
}