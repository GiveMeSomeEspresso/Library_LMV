using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Library_proj.Data;
using Library_proj.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Library_proj.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Linq; // Додайте цей using

namespace Library_proj.Controllers
{
    public class BookController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public BookController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Book
        public async Task<IActionResult> Index()
        {
            var books = await _context.Books
                .Include(b => b.Requests) // Завантажуємо заявки
                .ToListAsync();
            ViewBag.CurrentUserId = _userManager.GetUserId(User); // Передаємо UserId
            return View(books);
        }

        [Authorize]
        public async Task<IActionResult> RequestBook(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            int bookId = id.Value;
            var book = await _context.Books.FindAsync(bookId);
            if (book == null)
            {
                return NotFound();
            }
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToPage("/Identity/Account/Login");
            }
            var existingRequest = await _context.Requests
                .FirstOrDefaultAsync(r => r.BookId == bookId && r.UserId == userId);
            if (existingRequest != null)
            {
                ViewBag.ErrorMessage = "Ви вже подали заявку на цю книгу.";
                return View("RequestResult");
            }
            var request = new Request { BookId = bookId, UserId = userId };
            _context.Requests.Add(request);
            await _context.SaveChangesAsync();
            ViewBag.Message = "Вашу заявку на книгу подано.";
            return View("RequestResult");
        }

        //get
        public async Task<IActionResult> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                var allBooks = await _context.Books.ToListAsync();
                return View("Index", allBooks);
            }
            var searchResults = await _context.Books
                .Where(b => b.Title.Contains(searchTerm) || b.Author.Contains(searchTerm) || b.Genre.Contains(searchTerm))
                .ToListAsync();
            return View("Index", searchResults);
        }

        // GET: Borrow
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Borrow(int? id, string userId)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            var users = await _context.Users.ToListAsync();
            var userList = users.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = u.UserName,
                Selected = (u.Id == userId) // Позначаємо користувача як обраного, якщо userId передано
            }).ToList();

            var borrowViewModel = new BorrowViewModel
            {
                BookId = book.Id,
                BookTitle = book.Title,
                Users = userList,
                UserId = userId // Передаємо UserId у ViewModel для можливості відображення
            };

            return View(borrowViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Borrow(BorrowViewModel borrowViewModel)
        {
            var book = await _context.Books.FindAsync(borrowViewModel.BookId);
            var user = await _context.Users.FindAsync(borrowViewModel.UserId);

            if (book == null || user == null)
            {
                return NotFound();
            }

            if (book.AvailableQuantity > 0)
            {
                var borrowing = new Borrowing
                {
                    BookId = borrowViewModel.BookId,
                    UserId = borrowViewModel.UserId,
                    BorrowDate = DateTime.UtcNow,
                    ReturnDueDate = DateTime.UtcNow.AddDays(14)
                };

                _context.Borrowings.Add(borrowing);
                book.AvailableQuantity--;
                _context.Update(book);

                // Видалення відповідної заявки
                var requestToRemove = await _context.Requests
                    .FirstOrDefaultAsync(r => r.BookId == borrowViewModel.BookId && r.UserId == borrowViewModel.UserId);

                if (requestToRemove != null)
                {
                    _context.Requests.Remove(requestToRemove);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var users = await _context.Users.ToListAsync();
            var userList = users.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = u.UserName
            }).ToList();

            borrowViewModel.Users = userList;
            ViewBag.ErrorMessage = "Книга недоступна для видачі.";
            return View(borrowViewModel);
        }

        //get
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReturnBook(int? bookId)
        {
            if (bookId == null)
            {
                return NotFound();
            }
            var borrowings = await _context.Borrowings
                .Include(b => b.Book)
                .Include(b => b.User)
                .Where(b => b.BookId == bookId && b.ReturnDate == null)
                .ToListAsync();
            if (borrowings == null || !borrowings.Any())
            {
                ViewBag.ErrorMessage = "Для цієї книги не знайдено активних видач.";
                return View(null);
            }
            return View(borrowings);
        }

        //post
        [HttpPost, ActionName("ReturnBook")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReturnBookConfirmed(int id)
        {
            // Логіка повернення книги з ID видачі = id
            var borrowing = await _context.Borrowings
                .FindAsync(id); // Шукаємо видачу за її унікальним ID

            if (borrowing != null && borrowing.ReturnDate == null) // Перевіряємо, чи видача існує та ще не повернута
            {
                borrowing.ReturnDate = DateTime.UtcNow;
                _context.Update(borrowing);
                // Оновлення AvailableQuantity книги (збільшення на 1)
                var book = await _context.Books.FindAsync(borrowing.BookId); // Отримуємо книгу за BookId з видачі
                if (book != null)
                {
                    book.AvailableQuantity++;
                    _context.Update(book);
                }
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index)); // Перенаправлення на список книг
            }
            else
            {
                return NotFound(); // Або інша обробка, якщо видачу не знайдено або вона вже повернута
            }
        }

        // POST: Book/CancelRequest/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest(int id)
        {
            var requestToRemove = await _context.Requests.FindAsync(id);

            if (requestToRemove != null)
            {
                _context.Requests.Remove(requestToRemove);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        // GET: Book/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var book = await _context.Books
                .FirstOrDefaultAsync(m => m.Id == id);
            if (book == null)
            {
                return NotFound();
            }
            return View(book);
        }

        // POST: Book/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Book/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }
            return View(book);
        }

        // POST: Book/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Author,Genre,PublicationYear,ISBN,Quantity,AvailableQuantity")] Book book)
        {
            if (id != book.Id)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(book);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.Id == id);
        }
    }
}