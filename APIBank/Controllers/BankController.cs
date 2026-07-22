using APIBank.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIBank.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class BankController : ControllerBase
    {
        private readonly IBankService _bankService;

        public BankController(IBankService bankService)
        {
            _bankService = bankService;
        }

        // ── Clients ───────────────────────────────────────────────────────────

        [HttpGet("clients")]
        public IActionResult GetAllClients()
        {
            var clients = _bankService.GetAllClients();
            return Ok(clients);
        }

        [HttpGet("clients/{accountId}")]
        public IActionResult GetClient(string accountId)
        {
            var client = _bankService.GetClient(accountId);
            if (client == null) return NotFound($"Client {accountId} not found");
            return Ok(client);
        }

        [HttpPost("clients")]
        public IActionResult CreateClient([FromQuery] string firstName, [FromQuery] string lastName)
        {
            var client = _bankService.CreateClient(firstName, lastName);
            return CreatedAtAction(nameof(GetClient), new { accountId = client.AccountId }, client);
        }

        public record UpdateClientRequest(string FirstName, string LastName);

        [HttpPut("clients/{accountId}")]
        public IActionResult UpdateClient(string accountId, [FromBody] UpdateClientRequest request)
        {
            try
            {
                _bankService.UpdateClient(accountId, request.FirstName, request.LastName);
                return NoContent();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("clients/{accountId}")]
        public IActionResult DeleteClient(string accountId)
        {
            try
            {
                _bankService.DeleteClient(accountId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        // ── Cards ─────────────────────────────────────────────────────────────

        [HttpGet("cards/{cardNumber}")]
        public IActionResult GetCard(string cardNumber)
        {
            var card = _bankService.GetCard(cardNumber);
            if (card == null) return NotFound($"Card {cardNumber} not found");
            return Ok(card);
        }

        [HttpGet("clients/{accountId}/cards")]
        public IActionResult GetClientCards(string accountId)
        {
            var cards = _bankService.GetClientCards(accountId);
            return Ok(cards);
        }

        [HttpPost("clients/{accountId}/cards")]
        public IActionResult CreateCard(string accountId)
        {
            var owner = _bankService.GetClient(accountId);
            if (owner == null) return NotFound($"Client {accountId} not found");

            var card = _bankService.CreateCard(owner);
            return CreatedAtAction(nameof(GetCard), new { cardNumber = card.CardNumber }, card);
        }

        [HttpPut("clients/{accountId}/primary-card/{cardNumber}")]
        public IActionResult SetPrimaryCard(string accountId, string cardNumber)
        {
            try
            {
                _bankService.SetPrimaryCard(accountId, cardNumber);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("cards/{cardNumber}")]
        public IActionResult DeleteCard(string cardNumber)
        {
            try
            {
                _bankService.DeleteCard(cardNumber);
                return NoContent();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        // ── Balance ───────────────────────────────────────────────────────────

        [HttpGet("cards/{cardNumber}/balance")]
        public IActionResult GetCardBalance(string cardNumber)
        {
            try
            {
                var balance = _bankService.GetCardBalance(cardNumber);
                return Ok(new { cardNumber, balance });
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("clients/{accountId}/balance")]
        public IActionResult GetTotalBalance(string accountId)
        {
            var balance = _bankService.GetTotalBalance(accountId);
            return Ok(new { accountId, balance });
        }
    }
}
