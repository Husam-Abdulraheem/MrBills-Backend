using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillMate.Models
{
    public class ActivityLog
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int BillId { get; set; }
        public string? Name { get; set; }
        public float Amount { get; set; }
        public DateTime PaymentTime { get; set; }
        public string? PaymentType { get; set; }
    }
}
