using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BillMate.Models
{
    public class User : IdentityUser
    {
        [MaxLength(100)]
        public string? FullName { get; set; } = "Unknown";
        [MinLength(26)]
        [MaxLength(34)] 
        public string? Iban { get; set; }
        public bool IsAdmin { get; set; } = false;
        public int? GroupId { get; set; }
        //public string? RefreshToken { get; set; }
        //public DateTime? RefreshTokenExpiry { get; set; }
        //public Group? Group { get; set; }
    }
}
