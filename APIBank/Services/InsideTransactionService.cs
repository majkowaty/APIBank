using APIBank.Model;
using Microsoft.EntityFrameworkCore;

    public class InsideTransactionService : ITransactionService
{
    private readonly AppDbContext _context;

    public InsideTransactionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task SendMoney(Transaction transaction)
    {
        var fromCard = await _context.Cards
            .FirstOrDefaultAsync(c => c.AccountId == transaction.FromAccountId);

        if (fromCard == null)
            throw new Exception("Account not found");

        if (fromCard.Balance < transaction.Amount)
            throw new Exception("Insufficient balance");

        fromCard.Balance -= transaction.Amount;
        await ReceiveMoney(transaction);
        TransactionResponse(transaction);
    }

    public async Task ReceiveMoney(Transaction transaction)
    {
        var toCard = await _context.Cards
            .FirstOrDefaultAsync(c => c.AccountId == transaction.ToAccountId);

        if (toCard == null)
            throw new Exception("Account not found");

        toCard.Balance += transaction.Amount;
        await _context.SaveChangesAsync();
    }

    public void TransactionResponse(Transaction transaction)
    {
        Console.WriteLine($"Transakcja {transaction.TransactionId}: {transaction.Amount} " +
            $"z konta {transaction.FromAccountId} na konto {transaction.ToAccountId} ({transaction.TransactionDate}).");
    }
}
