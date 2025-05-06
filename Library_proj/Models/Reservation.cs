using Library_proj.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Library_proj.Models
{
    public class Reservation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Book")]
        public int BookId { get; set; }
        public Book Book { get; set; } // Навігаційна властивість до книги

        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; }
        public User User { get; set; } // Навігаційна властивість до користувача

        [Required]
        public DateTime ReservationDate { get; set; }
    }
}