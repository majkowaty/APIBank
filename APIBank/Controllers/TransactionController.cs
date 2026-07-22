using APIBank.Model;
using Microsoft.AspNetCore.Mvc;

namespace APIBank.Controllers
{
    public record MoneyTransferRequest(string FromAccountId, string ToAccountId, decimal Amount);

    [ApiController]
    [Route("[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IBankService _bankService;

        public TransactionController(ITransactionService transactionService, IBankService bankService)
        {
            _transactionService = transactionService;
            _bankService = bankService;
        }

        // ── Send / Receive ────────────────────────────────────────────────────

        [HttpPost("send")]
        public async Task<IActionResult> SendMoney([FromBody] MoneyTransferRequest request)
        {
            var transaction = new Transaction
            {
                FromAccountId = request.FromAccountId,
                ToAccountId = request.ToAccountId,
                Amount = request.Amount,
                TransactionDate = DateTime.Now
            };

            try
            {
                await _transactionService.SendMoney(transaction);
                _bankService.AddTransaction(transaction);
                _transactionService.TransactionResponse(transaction);
                return Ok(new { message = "Transfer completed", transaction });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("receive")]
        public async Task<IActionResult> ReceiveMoney([FromBody] MoneyTransferRequest request)
        {
            var transaction = new Transaction
            {
                FromAccountId = request.FromAccountId,
                ToAccountId = request.ToAccountId,
                Amount = request.Amount,
                TransactionDate = DateTime.Now
            };

            try
            {
                await _transactionService.ReceiveMoney(transaction);
                _bankService.AddTransaction(transaction);
                _transactionService.TransactionResponse(transaction);
                return Ok(new { message = "Funds received", transaction });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ── History ───────────────────────────────────────────────────────────

        [HttpGet("{transactionId:int}")]
        public IActionResult GetTransaction(int transactionId)
        {
            var transaction = _bankService.GetTransaction(transactionId);
            if (transaction == null) return NotFound($"Transaction {transactionId} not found");
            return Ok(transaction);
        }

        [HttpGet("account/{accountId}")]
        public IActionResult GetTransactions(string accountId)
        {
            var transactions = _bankService.GetTransactions(accountId);
            return Ok(transactions);
        }
    }
}
