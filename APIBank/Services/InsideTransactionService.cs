public class InsideTransactionService : ITransactionService{

    public void SendMoney(Transaction transaction){
        var fromAccount = _context.Accounts.FirstOrDefault(a => a.AccountId == transaction.FromAccountId);
        var toAccount = _context.Accounts.FirstOrDefault(a => a.AccountId == transaction.ToAccountId);
        if(fromAccount == null || toAccount == null){
            throw new Exception("Account not found");
        }
        if(fromAccount.Balance < transaction.Amount){
            throw new Exception("Insufficient balance");
        }
        ReceiveMoney(transaction);
        TransactionResponse(transaction);
    }
    public void ReceiveMoney(Transaction transaction){
        var fromAccount = _context.Accounts.FirstOrDefault(a => a.AccountId == transaction.FromAccountId);
        var toAccount = _context.Accounts.FirstOrDefault(a => a.AccountId == transaction.ToAccountId);
        if(fromAccount == null || toAccount == null){
            throw new Exception("Account not found");
        }
        fromAccount.Balance -= transaction.Amount;
        toAccount.Balance += transaction.Amount;
        _context.SaveChanges();
    }
    public void TransactionResponse(Transaction transaction){
        Console.WriteLine($"Transakcja {transaction.TransactionId}: {transaction.Amount} z konta {transaction.FromAccountId} na konto {transaction.ToAccountId} ({transaction.TransactionDate}).");
    }
}