using System.ComponentModel.DataAnnotations;

namespace APIBank.Model
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }
        public string FromAccountId { get; set; } = string.Empty;
        public string ToAccountId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.Now;
    }
}
