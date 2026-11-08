namespace BillMate.DTOs
{
    public class LoginResponse
    {
        public bool Success { get; set; } = false;
        public string? Token { get; set; }
        //public string? RefreshToken { get; internal set; }
        public string? Message { get; set; }
        public bool Group { get; set; } = false ;
    }
}
