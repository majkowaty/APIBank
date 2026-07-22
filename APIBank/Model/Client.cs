using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIBank.Model
{
    public class Client
    {
        [Key]
        public string AccountId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        // Relacja jeden-do-wielu: Client 1 --- * Card (odwrotność Card.Owner).
        [InverseProperty(nameof(Card.Owner))]
        public List<Card> Cards { get; set; } = new();

        // Osobna relacja: wskazanie karty głównej z jawnym kluczem obcym,
        // żeby EF Core nie pomylił jej z kolekcją Cards.
        public string? PrimaryCardNumber { get; set; }

        [ForeignKey(nameof(PrimaryCardNumber))]
        public Card? PrimaryCard { get; set; }

        public void AddCard(Card card)
        {
            Cards.Add(card);
            if (PrimaryCard == null)
            {
                PrimaryCard = card;
                PrimaryCardNumber = card.CardNumber;
            }
        }

        public List<Card> GetCards()
        {
            return Cards;
        }
        public Client(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
            AccountId = GenerateAccountId();
        }

        // Tworzy klienta z gwarancją unikalności numeru konta względem podanego źródła.
        // Teraz sprawdzanie po kolekcji w pamięci, po dodaniu EF Core - po bazie.
        public static Client Create(string firstName, string lastName, Func<string, bool> accountIdExists)
        {
            Client client = new Client(firstName, lastName);
            while (accountIdExists(client.AccountId))
            {
                client.AccountId = GenerateAccountId();
            }
            return client;
        }
        private static readonly Random _rand = new Random();

        private static string GenerateAccountId()
        {
            string accountId = "4269";
            for (int i = 0; i < 22; i++)
            {
                accountId += _rand.Next(0, 10);
            }
            return accountId;
        }
    }
}
