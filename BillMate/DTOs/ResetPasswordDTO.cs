namespace BillMate.DTOs
{
    public class ResetPasswordDTO
    {
        public string? UserId { get; set; }
        public string? Code { get; set; }
        public string? NewPassword { get; set; }
    }
}
