using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebNangCao.Data;
using WebNangCao.Models;

namespace WebNangCao.Controllers
{
    [Authorize]
    public class BlogController : Controller
    {
        private readonly AppDbContext _context;

        public BlogController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Blog
        public async Task<IActionResult> Index(string search)
        {
            var query = _context.BlogPosts
                .Include(b => b.Author)
                .Where(b => !b.IsDeleted && b.Status == BlogPostStatus.Published)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => b.Title.Contains(search) || b.Content.Contains(search));
                ViewBag.Search = search;
            }

            var blogPosts = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();

            return View(blogPosts);
        }

        // GET: Blog/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var blogPost = await _context.BlogPosts
                .Include(b => b.Author)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted && m.Status == BlogPostStatus.Published);

            if (blogPost == null)
            {
                return NotFound();
            }

            return View(blogPost);
        }
    }
}
