using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIBank.Model
{
    public class Card
    {
        private static readonly Random _rand = new Random();

        [Key]
        public string CardNumber { get; set; } = string.Empty;

        public string AccountId { get; set; } = string.Empty;

        [ForeignKey(nameof(AccountId))]
        public Client? Owner { get; set; }

        public int CVV { get; set; }
        public decimal Balance { get; set; }
        public DateOnly ExpirationDate { get; }

        private Card() { }

        public Card(string AccountId)
        {
            this.AccountId = AccountId;
            CardNumber = GenerateCardNumber();
            CVV = GenerateCVV();
            Balance = 0;
            ExpirationDate = GenerateExpirationDate();
        }

        public static Card Create(string accountId, Func<string, bool> cardNumberExists)
        {
            Card card = new Card(accountId);
            while (cardNumberExists(card.CardNumber))
            {
                card.CardNumber = GenerateCardNumber();
            }
            return card;
        }

        private static int GenerateCVV()
        {
            return _rand.Next(1000);
        }

        private static string GenerateCardNumber()
        {
            String CardNumber = Bank.CardFirst4Numbers;
            for (int i = 0; i < 11; i++)
            {
                CardNumber += _rand.Next(0, 10);
            }
            return CardNumber;
        }

        private DateOnly GenerateExpirationDate()
        {
            return DateOnly.FromDateTime(DateTime.Now).AddYears(3);
        }
    }
}
