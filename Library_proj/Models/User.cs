using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Library_proj.Models
{
    public class User : IdentityUser
    {
        [Required(ErrorMessage = "Роль є обов'язковою.")]
        [StringLength(20, ErrorMessage = "Роль не може бути довшою за 20 символів.")]
        public string Role { get; set; }
        public ICollection<Request> Requests { get; set; }
    }
}