using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIBank.Model
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public string? ClientAccountId { get; set; }

        [ForeignKey(nameof(ClientAccountId))]
        public Client? Client { get; set; }
    }
}
