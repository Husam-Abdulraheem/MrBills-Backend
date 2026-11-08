using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillMate.Models
{
    public class Group
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string? Code { get; set; }
        public string? ManagerId { get; set; }
        public int MaxMembers {  get; set; }
        public ICollection<GroupUser>? Members { get; set; } = new List<GroupUser>();

    }
}
