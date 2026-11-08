namespace BillMate.DTOs
{
    public class EditBillDTO
    {
        public int BillId {  get; set; }
        public string? Description { get; set; }
        public float Total { get; set; }
        public List<ClaimantDTO>? Claimants { get; set; }

    }
}
