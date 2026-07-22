namespace APIBank.Model.Transfers
{
    public class TransferRequestDTO
    {
        public string FromAccountId { get; set; } = string.Empty;
        public string ToAccountId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int TransactionId { get; set; }
    }
}
