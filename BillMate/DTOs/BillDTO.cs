using BillMate.Models;

namespace BillMate.DTOs
{
    public class BillDTO
    {
        public int BillId { get; set; }
        public float Total { get; set; }
        public string? Description {  get; set; }
        public DateTime CreateDate { get; set; }
        public string? OwnerName { get; set; }
        public List<ActivityLog>? ActivityLogs { get; set; }
        public List<ClaimantDTO>? Claimants { get; set; }
    }
}
