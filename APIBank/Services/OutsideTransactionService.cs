using APIBank.Model;
using APIBank.Model.Transfers;
using Microsoft.EntityFrameworkCore;

public class OutsideTransactionService : ITransactionService
{
    private readonly AppDbContext _context;
    private readonly HttpClient _httpClient;

    public OutsideTransactionService(AppDbContext context, HttpClient httpClient)
    {
        _context = context;
        _httpClient = httpClient;
    }

    public async Task SendMoney(Transaction transaction)
    {
        var fromCard = await _context.Cards
            .FirstOrDefaultAsync(c => c.AccountId == transaction.FromAccountId);

        if (fromCard == null)
            throw new Exception("Account not found");

        if (fromCard.Balance < transaction.Amount)
            throw new Exception("Insufficient balance");

        var transferRequest = new TransferRequestDTO
        {
            FromAccountId = transaction.FromAccountId,
            ToAccountId = transaction.ToAccountId,
            Amount = transaction.Amount,
            TransactionId = transaction.TransactionId
        };

        TransferResponseDTO? result = null;
        try
        {
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/api/transfers", transferRequest);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to send money: {response.StatusCode}");

            var dto = await response.Content.ReadFromJsonAsync<TransferResponseDTO?>();  
            if (dto == null)
                throw new Exception("Failed to send money: empty response");

            result = dto;
        } catch (JsonException) {
            throw new Exception("Failed to send money: invalid response");
        }
        catch (HttpRequestException)
        {
            throw new Exception("Failed to connect to the outside bank");
        }
        catch (TaskCanceledException)
        {
            throw new Exception("Request timed out");
        }

        if (!result.Accepted)
            throw new Exception(result.Reason ?? "Transfer rejected");

        fromCard.Balance -= transaction.Amount;
        await _context.SaveChangesAsync();
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
        Console.WriteLine($"[OUTSIDE] Transakcja {transaction.TransactionId}: " +
            $"{transaction.Amount} z {transaction.FromAccountId} do {transaction.ToAccountId}.");
    }
}
