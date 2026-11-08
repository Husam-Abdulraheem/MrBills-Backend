namespace BillMate.DTOs
{
    public class ShopierCallbackDTO
    {
        public string Status { get; set; }
        public string? PlatformOrderId { get; set; }
        public string? PaymentId { get; set; }
        public string Signature { get; set; }
        public string? RandomNr { get; set; }
        public string? TotalOrderValue { get; set; }
        public string? Currency { get; set; }
        public string? email { get; set; }
    }
}
