public interface ITransactionService
{
    void SendMoney(Transaction transaction);
    void ReceiveMoney(Transaction transaction);
    void TransactionResponse(Transaction transaction);
}