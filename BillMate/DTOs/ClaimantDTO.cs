namespace BillMate.DTOs
{
    public class ClaimantDTO
    {
        public string? ClaimantId { get; set; }
        public string? ClaimantName { get; set; }
        public float Amount { get; set; }
        public bool IsPaid { get; set; }
        public string? Receipt { get; set; }
    }
}