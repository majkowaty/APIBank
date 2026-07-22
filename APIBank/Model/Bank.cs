namespace APIBank.Model
{
    public class Bank
    {
        List<Client> clients;
        List<Transaction> transactions;
        public static string CardFirst4Numbers { get; } = "6767";
        public Bank()
        {
            clients = new List<Client>();
            transactions = new List<Transaction>();
        }
        public void AddClient(Client client)
        {
            clients.Add(client);
        }

        public bool AccountIdExists(string accountId)
        {
            return clients.Any(c => c.AccountId == accountId);
        }

        // Tworzy klienta z gwarantowanie unikalnym numerem konta i dodaje go do banku.
        public Client CreateClient(string firstName, string lastName)
        {
            Client client = Client.Create(firstName, lastName, AccountIdExists);
            clients.Add(client);
            return client;
        }
        public void AddTransaction(Transaction transaction)
        {
            transactions.Add(transaction);
        }

        public bool CardNumberExists(string cardNumber)
        {
            return clients.Any(c => c.GetCards().Any(card => card.CardNumber == cardNumber));
        }

        public Card CreateCard(Client owner)
        {
            Card card = Card.Create(owner.AccountId, CardNumberExists);
            card.Owner = owner;
            owner.AddCard(card);
            return card;
        }
    }
}
