using APIBank.Model;

public interface IBankService
{
    // Client
    Client CreateClient(string firstName, string lastName);
    Client? GetClient(string accountId);
    List<Client> GetAllClients();
    void UpdateClient(string accountId, string firstName, string lastName);
    void DeleteClient(string accountId);
    bool AccountIdExists(string accountId);

    // Card
    Card CreateCard(Client owner);
    Card? GetCard(string cardNumber);
    List<Card> GetClientCards(string accountId);
    void SetPrimaryCard(string accountId, string cardNumber);
    void DeleteCard(string cardNumber);
    bool CardNumberExists(string cardNumber);

    // Balance
    decimal GetCardBalance(string cardNumber);
    decimal GetTotalBalance(string accountId);

    // Transaction
    void AddTransaction(Transaction transaction);
    Transaction? GetTransaction(int transactionId);
    List<Transaction> GetTransactions(string accountId);
}
