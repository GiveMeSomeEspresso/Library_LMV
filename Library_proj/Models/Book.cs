using System.Collections.Generic;

namespace Library_proj.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Genre { get; set; }
        public string ISBN { get; set; }
        public int? PublicationYear { get; set; } // Зробіть nullable, якщо рік публікації може бути невідомий
        public int Quantity { get; set; }
        public int AvailableQuantity { get; set; }

        public ICollection<Borrowing> Borrowings { get; set; }
        public ICollection<Reservation> Reservations { get; set; }
        public ICollection<Request> Requests { get; set; }
    }
}