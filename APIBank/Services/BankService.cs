using APIBank.Model;
using Microsoft.EntityFrameworkCore;

public class BankService : IBankService
{
    private readonly AppDbContext _context;

    public BankService(AppDbContext context)
    {
        _context = context;
    }

    // ── Client ────────────────────────────────────────────────────────────────

    public bool AccountIdExists(string accountId) =>
        _context.Clients.Any(c => c.AccountId == accountId);

    public Client CreateClient(string firstName, string lastName)
    {
        Client client = Client.Create(firstName, lastName, AccountIdExists);
        _context.Clients.Add(client);
        _context.SaveChanges();
        return client;
    }

    public Client? GetClient(string accountId) =>
        _context.Clients
            .Include(c => c.Cards)
            .Include(c => c.PrimaryCard)
            .FirstOrDefault(c => c.AccountId == accountId);

    public List<Client> GetAllClients() =>
        _context.Clients
            .Include(c => c.Cards)
            .Include(c => c.PrimaryCard)
            .ToList();

    public void UpdateClient(string accountId, string firstName, string lastName)
    {
        var client = _context.Clients.Find(accountId)
            ?? throw new Exception($"Client {accountId} not found");

        client.FirstName = firstName;
        client.LastName = lastName;
        _context.SaveChanges();
    }

    public void DeleteClient(string accountId)
    {
        var client = _context.Clients.Find(accountId)
            ?? throw new Exception($"Client {accountId} not found");

        _context.Clients.Remove(client);
        _context.SaveChanges();
    }

    // ── Card ──────────────────────────────────────────────────────────────────

    public bool CardNumberExists(string cardNumber) =>
        _context.Cards.Any(c => c.CardNumber == cardNumber);

    public Card CreateCard(Client owner)
    {
        Card card = Card.Create(owner.AccountId, CardNumberExists);
        card.Owner = owner;
        owner.AddCard(card);
        _context.Cards.Add(card);
        _context.SaveChanges();
        return card;
    }

    public Card? GetCard(string cardNumber) =>
        _context.Cards
            .Include(c => c.Owner)
            .FirstOrDefault(c => c.CardNumber == cardNumber);

    public List<Card> GetClientCards(string accountId) =>
        _context.Cards
            .Where(c => c.AccountId == accountId)
            .ToList();

    public void SetPrimaryCard(string accountId, string cardNumber)
    {
        var client = _context.Clients
            .Include(c => c.Cards)
            .FirstOrDefault(c => c.AccountId == accountId)
            ?? throw new Exception($"Client {accountId} not found");

        var card = client.Cards.FirstOrDefault(c => c.CardNumber == cardNumber)
            ?? throw new Exception($"Card {cardNumber} does not belong to client {accountId}");

        client.PrimaryCardNumber = cardNumber;
        client.PrimaryCard = card;
        _context.SaveChanges();
    }

    public void DeleteCard(string cardNumber)
    {
        var card = _context.Cards
            .Include(c => c.Owner)
            .FirstOrDefault(c => c.CardNumber == cardNumber)
            ?? throw new Exception($"Card {cardNumber} not found");

        if (card.Owner?.PrimaryCardNumber == cardNumber)
        {
            card.Owner.PrimaryCardNumber = null;
            card.Owner.PrimaryCard = null;
        }

        _context.Cards.Remove(card);
        _context.SaveChanges();
    }

    // ── Balance ───────────────────────────────────────────────────────────────

    public decimal GetCardBalance(string cardNumber)
    {
        var card = _context.Cards.Find(cardNumber)
            ?? throw new Exception($"Card {cardNumber} not found");

        return card.Balance;
    }

    public decimal GetTotalBalance(string accountId) =>
        _context.Cards
            .Where(c => c.AccountId == accountId)
            .Sum(c => c.Balance);

    // ── Transaction ───────────────────────────────────────────────────────────

    public void AddTransaction(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        _context.SaveChanges();
    }

    public Transaction? GetTransaction(int transactionId) =>
        _context.Transactions.Find(transactionId);

    public List<Transaction> GetTransactions(string accountId) =>
        _context.Transactions
            .Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId)
            .OrderByDescending(t => t.TransactionDate)
            .ToList();
}
