using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillMate.Models
{
    public class Bill
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string? OwnerId { get; set; }
        public User? Owner { get; set; }
        public int GroupId { get; set; }
        public float Total { get; set; }
        public string? Description { get; set; }
        public DateTime CreateDate { get; set; }
        public bool RequiredReceipt { get; set; }
        public string? Invoice { get; set; }
        public bool IsArchived { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public ICollection<Debt>? Debts { get; set; }

    }
}
