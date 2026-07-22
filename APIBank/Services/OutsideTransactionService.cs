public class OutsideTransactionService : ITransactionService{
private readonly HttpClient _httpClient;
public OutsideTransactionService(HttpClient httpClient)
{
    _httpClient = httpClient;
}

    public async Task SendMoney(Transaction transaction){
        var fromAccount = _context.Accounts.FirstOrDefault(a => a.AccountId == transaction.FromAccountId);
        
        if (fromAccount == null){
            throw new Exception("Account not found");
        }
        
        if (fromAccount.Balance < transaction.Amount){
            throw new Exception("Insufficient balance");
        }
        
        var transferRequest = new TransferRequestDTO{
            FromAccountId = transaction.FromAccountId,
            ToAccountId = transaction.ToAccountId,
            Amount = transaction.Amount,
            TransactionId = transaction.TransactionId
        };


        TransferResponse? result;
        try{
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/api/transfers", transferRequest);

            if (!response.IsSuccessStatusCode){
                throw new Exception($"Failed to send money: {response.StatusCode}");
            }

            result = await response.Content.ReadFromJsonAsync<TransferResponseDTO>();
            if (result == null){
                throw new Exception("Failed to send money");
            }

            if (!result.Accepted || result.Reason != null){
                throw new Exception(result.Reason);
            }
        catch (HttpRequestException){
            throw new Exception("Failed to connect to the outside bank");
            }
        catch (TaskCanceledException){
            throw new Exception("Request timed out");
            }

            if (result != null && result.Accepted)
            {
                fromAccount.Balance -= transaction.Amount;
                _context.SaveChanges();
                TransactionResponse(transaction);
            }
            else{
                throw new Exception(result?.Reason);
            }
    }

    public void ReceiveMoney(Transaction transaction){
        var toAccount = _context.Accounts.FirstOrDefault(a => a.AccountId == transaction.ToAccountId);

        if (toAccount == null)
        {
            throw new Exception("Account not found");
        }
        toAccount.Balance += transaction.Amount;
        _context.SaveChanges();
    }
    public void TransactionResponse(Transaction transaction){
        Console.WriteLine($"[OUTSIDE] Transakcja {transaction.TransactionId}: " +
        $"{transaction.Amount} z {transaction.FromAccountId} do {transaction.ToAccountId}.");
    }
}
}