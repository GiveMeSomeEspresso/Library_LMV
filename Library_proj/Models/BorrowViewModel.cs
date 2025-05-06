using Microsoft.AspNetCore.Mvc.Rendering;

namespace Library_proj.Models.ViewModels
{
    public class BorrowViewModel
    {
        public int BookId { get; set; }
        public string BookTitle { get; set; }
        public string UserId { get; set; }

        // Добавим список пользователей для DropDown
        public List<SelectListItem> Users { get; set; }
    }
}
