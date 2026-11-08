using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillMate.Models
{
    public class Debt
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int BillId { get; set; }
        public Bill? Bill { get; set; }
        public string? ToUserId { get; set; }
        public User? ToUser { get; set; }
        public string? FromUserId { get; set; }
        public User? FromUser { get; set; }
        public float Amount { get; set; }
        public bool Status { get; set; }
        public string? ReceiptUrl { get; set; }
    }
}
