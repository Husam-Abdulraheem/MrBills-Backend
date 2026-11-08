namespace BillMate.DTOs
{
    public class PayBillDTO
    {
        public int BillId {  get; set; }
        public IFormFile? ReceiptFile { get; set; }
    }
}
