
namespace BillMate.DTOs
{
    public class CreateBillDTO
    {
        public float Total { get; set; }
        public string? Description { get; set; }
        public bool RequiredReceipt { get; set; }
        public IFormFile? Invoice { get; set; }
        public List<string>? BillUsersId { get; set; }
    }
}
