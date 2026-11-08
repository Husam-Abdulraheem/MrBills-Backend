namespace BillMate.DTOs
{
    public class ResponseDTO
    {
        public bool Success { get; set; } = false;
        public string? Message { get; set; }
        public object? Data { get; set; } = new List<object>();
        public string? HtmlContent { get; set; }
        public string Error { get; internal set; }
    }
}
