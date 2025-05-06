using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Library_proj.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Library_proj.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RequestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var requests = await _context.Requests
                .Include(r => r.Book)
                .Include(r => r.User)
                .ToListAsync();

            return View(requests);
        }
    }
}