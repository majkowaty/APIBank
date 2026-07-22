using APIBank.Model;

public interface ITransactionService
{
    Task SendMoney(Transaction transaction);
    Task ReceiveMoney(Transaction transaction);
    void TransactionResponse(Transaction transaction);
}
