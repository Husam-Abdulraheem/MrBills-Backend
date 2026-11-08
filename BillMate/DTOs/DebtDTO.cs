namespace BillMate.DTOs
{
    public class DebtDTO
    {
        public string? Description {  get; set; }
        public string? BillOwner { get; set; }
        public int BillId { get; set; }
        public float Amount { get; set; }
        public DateTime? Date {  get; set; }
        public bool IsRequired { get; set; }
        public string? Invoice {  get; set; }
        public string? Iban { get; set; }
    }
}
