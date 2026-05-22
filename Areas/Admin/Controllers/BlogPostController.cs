using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebNangCao.Data;
using WebNangCao.Models;
using WebNangCao.Models.ViewModels.Admin;

namespace WebNangCao.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")] // Customize as needed
    public class BlogPostController : Controller
    {
        private readonly AppDbContext _context;

        public BlogPostController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/BlogPost
        public async Task<IActionResult> Index(string search)
        {
            var query = _context.BlogPosts
                .Include(b => b.Author)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => b.Title.Contains(search) || (b.Author != null && b.Author.FullName.Contains(search)));
                ViewBag.Search = search;
            }

            var blogPosts = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();

            var model = blogPosts.Select(b => new BlogPostListVM
            {
                Id = b.Id,
                Title = b.Title,
                AuthorName = b.Author?.FullName ?? "N/A",
                CreatedAt = b.CreatedAt,
                Status = b.Status,
                IsDeleted = b.IsDeleted
            }).ToList();

            return View(model);
        }

        // GET: Admin/BlogPost/Create
        public IActionResult Create()
        {
            return View(new BlogPostCreateVM());
        }

        // POST: Admin/BlogPost/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPostCreateVM model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                var blogPost = new BlogPost
                {
                    Title = model.Title,
                    Content = model.Content,
                    Status = model.Status,
                    AuthorId = userId,
                    CreatedAt = DateTime.UtcNow.AddHours(7)
                };

                _context.BlogPosts.Add(blogPost);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm bài viết mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            
            return View(model);
        }

        // GET: Admin/BlogPost/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var blogPost = await _context.BlogPosts.FindAsync(id);
            if (blogPost == null) return NotFound();

            var model = new BlogPostEditVM
            {
                Id = blogPost.Id,
                Title = blogPost.Title,
                Content = blogPost.Content,
                Status = blogPost.Status,
                IsDeleted = blogPost.IsDeleted
            };

            return View(model);
        }

        // POST: Admin/BlogPost/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BlogPostEditVM model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var blogPost = await _context.BlogPosts.FindAsync(id);
                    if (blogPost == null) return NotFound();

                    blogPost.Title = model.Title;
                    blogPost.Content = model.Content;
                    blogPost.Status = model.Status;
                    blogPost.IsDeleted = model.IsDeleted;
                    blogPost.UpdatedAt = DateTime.UtcNow.AddHours(7);

                    _context.Update(blogPost);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Cập nhật bài viết thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BlogPostExists(model.Id)) return NotFound();
                    else throw;
                }
            }
            return View(model);
        }

        // GET: Admin/BlogPost/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var blogPost = await _context.BlogPosts
                .Include(b => b.Author)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (blogPost == null) return NotFound();

            return View(blogPost);
        }

        // POST: Admin/BlogPost/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var blogPost = await _context.BlogPosts.FindAsync(id);
            if (blogPost != null)
            {
                _context.BlogPosts.Remove(blogPost);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa bài viết thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài viết để xóa!";
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool BlogPostExists(int id)
        {
            return _context.BlogPosts.Any(e => e.Id == id);
        }
    }
}
