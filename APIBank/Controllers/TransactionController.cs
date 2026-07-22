using APIBank.Model;
using Microsoft.AspNetCore.Mvc;

namespace APIBank.Controllers
{
    public record MoneyTransferRequest(string FromAccountId, string ToAccountId, decimal Amount);

    [ApiController]
    [Route("[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly InsideTransactionService _insideService;
        private readonly OutsideTransactionService _outsideService;
        private readonly IBankService _bankService;

        public TransactionController(
            InsideTransactionService insideService,
            OutsideTransactionService outsideService,
            IBankService bankService)
        {
            _insideService = insideService;
            _outsideService = outsideService;
            _bankService = bankService;
        }

        private ITransactionService ResolveService(string toAccountId) =>
            toAccountId.StartsWith(Client.AccountPrefix)
                ? _insideService
                : _outsideService;

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
                var service = ResolveService(request.ToAccountId);
                await service.SendMoney(transaction);
                _bankService.AddTransaction(transaction);
                service.TransactionResponse(transaction);
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
                var service = ResolveService(request.ToAccountId);
                await service.ReceiveMoney(transaction);
                _bankService.AddTransaction(transaction);
                service.TransactionResponse(transaction);
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
