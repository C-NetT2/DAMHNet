using DAMH.Controllers;
using DAMH.Data;
using DAMH.Helpers;
using DAMH.Models;
using DAMH.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DAMH.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminController : Controller
    {
        private readonly LibraryContext _context;

        public AdminController(LibraryContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var books = await _context.Books.ToListAsync();
            return View(books);
        }

        [HttpGet]
        public IActionResult CreateBook()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBook(Book book)
        {
            if (ModelState.IsValid)
            {
                book.CreatedDate = DateTime.Now;
                book.LastUpdated = DateTime.Now;
                _context.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        [HttpGet]
        public async Task<IActionResult> EditBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();
            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBook(int id, Book book)
        {
            if (id != book.BookId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    book.LastUpdated = DateTime.Now;
                    _context.Update(book);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Books.Any(e => e.BookId == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        [HttpPost, ActionName("DeleteBook")]
        [ValidateAntiForgeryToken]
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

        public async Task<IActionResult> ViewBookChapters(int bookId)
        {
            var book = await _context.Books
                .Include(b => b.Chapters.OrderBy(c => c.ChapterOrder))
                .FirstOrDefaultAsync(m => m.BookId == bookId);

            if (book == null) return NotFound();

            return View(book);
        }

        [HttpGet]
        public async Task<IActionResult> AddChapter(int bookId)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book == null) return NotFound("Không tìm thấy sách.");

            ViewBag.BookTitle = book.Title;

            var newChapter = new Chapter
            {
                BookId = bookId,
                ChapterOrder = 1,
                IsFree = false
            };

            return View(newChapter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddChapter(Chapter chapter)
        {
            ModelState.Remove("Book"); 

            if (ModelState.IsValid)
            {
                _context.Add(chapter);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ViewBookChapters), new { bookId = chapter.BookId });
            }

            var book = await _context.Books.FindAsync(chapter.BookId);
            ViewBag.BookTitle = book?.Title;
            return View(chapter);
        }

        [HttpGet]
        public async Task<IActionResult> EditChapter(int id)
        {
            var chapter = await _context.Chapters.FindAsync(id);
            if (chapter == null) return NotFound();
            return View(chapter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditChapter(int id, Chapter chapter)
        {
            if (id != chapter.ChapterId) return NotFound();

            ModelState.Remove("Book"); 

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(chapter);
                    await _context.SaveChangesAsync();
                    
                    return RedirectToAction(nameof(ViewBookChapters), new { bookId = chapter.BookId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Chapters.Any(e => e.ChapterId == id)) return NotFound();
                    else throw;
                }
            }
            return View(chapter);
        }

        [HttpPost]
        public async Task<IActionResult> UploadChapterMedia(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Vui lòng chọn file." });

            var extension = Path.GetExtension(file.FileName).ToLower();
            var allowedImageTypes = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var allowedVideoTypes = new[] { ".mp4", ".webm", ".ogg" };

            bool isImage = allowedImageTypes.Contains(extension);
            bool isVideo = allowedVideoTypes.Contains(extension);

            if (!isImage && !isVideo)
                return Json(new { success = false, message = "Chỉ hỗ trợ file ảnh hoặc video (mp4)." });

            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "chapters");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            var fileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            var url = "/uploads/chapters/" + fileName;

            string htmlTag;
            if (isImage)
                htmlTag = $"<img src='{url}' class='img-fluid my-3 rounded shadow' alt='Minh họa' />";
            else
                htmlTag = $"<video controls class='w-100 my-3 rounded shadow'><source src='{url}' type='video/mp4'></video>";

            return Json(new { success = true, url = url, html = htmlTag });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteChapter(int id)
        {
            var chapter = await _context.Chapters.FindAsync(id);
            if (chapter != null)
            {
                int bookId = chapter.BookId;
                _context.Chapters.Remove(chapter);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ViewBookChapters), new { bookId = bookId });
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ManageMedia(int bookId)
        {
            var book = await _context.Books
                .Include(b => b.MediaFiles)
                .FirstOrDefaultAsync(b => b.BookId == bookId);

            if (book == null) return NotFound();
            return View(book);
        }

        [HttpGet]
        public IActionResult AddMedia(int bookId)
        {
            ViewBag.BookId = bookId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMedia(BookMedia media)
        {
            ModelState.Remove("Book");

            if (ModelState.IsValid)
            {
                media.UploadedDate = DateTime.Now;
                _context.BookMedias.Add(media);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageMedia), new { bookId = media.BookId });
            }
            ViewBag.BookId = media.BookId;
            return View(media);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMedia(int id)
        {
            var media = await _context.BookMedias.FindAsync(id);
            if (media != null)
            {
                int bookId = media.BookId;
                _context.BookMedias.Remove(media);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageMedia), new { bookId = bookId });
            }
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> UserDetails(string id)
        {
            var user = await _context.Users
                .Include(u => u.ReadingHistories).ThenInclude(rh => rh.Book)
                .Include(u => u.ReadingHistories).ThenInclude(rh => rh.Chapter)
                .Include(u => u.Reviews).ThenInclude(r => r.Book)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();
            return View(user);
        }

        public async Task<IActionResult> ManageReviews()
        {
            var reviews = await _context.Reviews
                .Include(r => r.Book)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
            return View(reviews);
        }

        [HttpGet]
        public async Task<IActionResult> EditReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();
            return View(review);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReview(int id, Review review)
        {
            if (id != review.ReviewId) return NotFound();

            var existingReview = await _context.Reviews.AsNoTracking().FirstOrDefaultAsync(r => r.ReviewId == id);
            if (existingReview == null) return NotFound();

            review.UserId = existingReview.UserId;
            review.BookId = existingReview.BookId;
            review.CreatedDate = existingReview.CreatedDate;
            review.UpdatedDate = DateTime.Now;

            ModelState.Remove("User");
            ModelState.Remove("Book");

            if (ModelState.IsValid)
            {
                _context.Update(review);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageReviews));
            }
            return View(review);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageReviews));
        }

        public async Task<IActionResult> Analytics()
        {
            var viewModel = new AdvancedAnalyticsViewModel();

            // Basic counts
            viewModel.TotalUsers = await _context.Users.CountAsync();
            viewModel.TotalBooks = await _context.Books.CountAsync();
            viewModel.TotalFavorites = await _context.Favorites.CountAsync();
            viewModel.TotalReadings = await _context.ReadingHistories.CountAsync();

            // Date calculations
            var now = DateTime.Now;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);

            // User Growth
            viewModel.NewUsersThisMonth = await _context.Users
                .Where(u => u.RegistrationDate >= thisMonthStart)
                .CountAsync();

            viewModel.NewUsersLastMonth = await _context.Users
                .Where(u => u.RegistrationDate >= lastMonthStart && u.RegistrationDate < thisMonthStart)
                .CountAsync();

            viewModel.UserGrowthPercentage = viewModel.NewUsersLastMonth > 0
                ? Math.Round(((double)(viewModel.NewUsersThisMonth - viewModel.NewUsersLastMonth) / viewModel.NewUsersLastMonth) * 100, 1)
                : viewModel.NewUsersThisMonth > 0 ? 100 : 0;

            // VIP Statistics
            viewModel.TotalVipUsers = await _context.Users
                .Where(u => u.IsMember && u.SubscriptionExpiryDate > DateTime.Now)
                .CountAsync();

            var vipTransactionsThisMonth = await _context.PaymentTransactions
                .Where(t => t.TransactionDate >= thisMonthStart && t.Status == "Completed")
                .ToListAsync();

            var vipTransactionsLastMonth = await _context.PaymentTransactions
                .Where(t => t.TransactionDate >= lastMonthStart && t.TransactionDate < thisMonthStart && t.Status == "Completed")
                .ToListAsync();

            viewModel.NewVipThisMonth = vipTransactionsThisMonth.Count;
            viewModel.NewVipLastMonth = vipTransactionsLastMonth.Count;

            viewModel.VipGrowthPercentage = viewModel.NewVipLastMonth > 0
                ? Math.Round(((double)(viewModel.NewVipThisMonth - viewModel.NewVipLastMonth) / viewModel.NewVipLastMonth) * 100, 1)
                : viewModel.NewVipThisMonth > 0 ? 100 : 0;

            // Revenue Statistics
            viewModel.TotalRevenue = await _context.PaymentTransactions
                .Where(t => t.Status == "Completed")
                .SumAsync(t => t.Amount);

            viewModel.RevenueThisMonth = vipTransactionsThisMonth.Sum(t => t.Amount);
            viewModel.RevenueLastMonth = vipTransactionsLastMonth.Sum(t => t.Amount);

            viewModel.RevenueGrowthPercentage = viewModel.RevenueLastMonth > 0
                ? Math.Round(((double)(viewModel.RevenueThisMonth - viewModel.RevenueLastMonth) / (double)viewModel.RevenueLastMonth) * 100, 1)
                : viewModel.RevenueThisMonth > 0 ? 100 : 0;

            // Package Sales Distribution
            var packageSales = await _context.PaymentTransactions
                .Where(t => t.Status == "Completed")
                .GroupBy(t => t.PackageType)
                .Select(g => new { Package = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var sale in packageSales)
            {
                viewModel.PackageSales[sale.Package.GetName()] = sale.Count;
            }

            // Monthly Revenue for last 6 months
            for (int i = 5; i >= 0; i--)
            {
                var monthStart = thisMonthStart.AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);

                var monthlyData = await _context.PaymentTransactions
                    .Where(t => t.TransactionDate >= monthStart && t.TransactionDate < monthEnd && t.Status == "Completed")
                    .GroupBy(t => 1)
                    .Select(g => new MonthlyRevenueData
                    {
                        Month = monthStart.ToString("MM/yyyy"),
                        Revenue = g.Sum(t => t.Amount),
                        VipCount = g.Count()
                    })
                    .FirstOrDefaultAsync();

                if (monthlyData == null)
                {
                    monthlyData = new MonthlyRevenueData
                    {
                        Month = monthStart.ToString("MM/yyyy"),
                        Revenue = 0,
                        VipCount = 0
                    };
                }

                viewModel.MonthlyRevenue.Add(monthlyData);
            }

            var favGenres = await _context.Favorites
                .Include(f => f.Book)
                .GroupBy(f => f.Book.Genre)
                .Select(g => new { Genre = g.Key, Count = g.Count() })
                .ToListAsync();

            int totalFavs = favGenres.Sum(g => g.Count);
            if (totalFavs > 0)
            {
                viewModel.FavoriteGenreStats = favGenres
                    .Select(g => new GenreStatistic
                    {
                        Genre = g.Genre,
                        Count = g.Count,
                        Percentage = Math.Round((double)g.Count / totalFavs * 100, 1)
                    })
                    .OrderByDescending(s => s.Percentage)
                    .ToList();
            }

            viewModel.MostFavoritedBooks = await _context.Books
                .Select(b => new BookStatistic
                {
                    Book = b,
                    FavoriteCount = _context.Favorites.Count(f => f.BookId == b.BookId)
                })
                .OrderByDescending(b => b.FavoriteCount)
                .Take(10)
                .ToListAsync();

            viewModel.MostReadBooks = await _context.Books
                .Select(b => new BookStatistic
                {
                    Book = b,
                    ReadCount = _context.ReadingHistories.Count(rh => rh.BookId == b.BookId)
                })
                .OrderByDescending(b => b.ReadCount)
                .Take(10)
                .ToListAsync();

            return View(viewModel);
        }

        public async Task<IActionResult> ManageFavorites(string? userId, int? bookId, int page = 1)
        {
            const int pageSize = 30;
            
            var query = _context.Favorites
                .Include(f => f.User)
                .Include(f => f.Book)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId)) query = query.Where(f => f.UserId == userId);
            if (bookId.HasValue) query = query.Where(f => f.BookId == bookId);

            var totalCount = await query.CountAsync();
            var totalPages = (totalCount + pageSize - 1) / pageSize;

            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var favorites = await query
                .OrderByDescending(f => f.DateAdded)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Users = await _context.Users.ToListAsync();
            ViewBag.Books = await _context.Books.OrderBy(b => b.Title).ToListAsync();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.UserId = userId;
            ViewBag.BookId = bookId;

            return View(favorites);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFavorite(int id)
        {
            var fav = await _context.Favorites.FindAsync(id);
            if (fav != null)
            {
                _context.Favorites.Remove(fav);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageFavorites));
        }


        [HttpGet]
        public async Task<IActionResult> ManageUsers(string searchTerm = "", int page = 1)
        {
            const int pageSize = 30;

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(u => u.Email!.Contains(searchTerm) ||
                                         (u.FullName != null && u.FullName.Contains(searchTerm)));
            }

            var totalUsers = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var users = await query
                .OrderByDescending(u => u.RegistrationDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.SearchTerm = searchTerm;

            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var viewModel = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                IsMember = user.IsMember,
                SubscriptionExpiryDate = user.SubscriptionExpiryDate,
                RegistrationDate = user.RegistrationDate
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FindAsync(model.Id);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;

            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật thông tin người dùng thành công!";
            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtendVip(string userId, int months)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            try
            {
                DateTime newExpiryDate;

                if (months == 999) 
                {
                    newExpiryDate = DateTime.Now.AddYears(100);
                }
                else
                {
                    var startDate = user.SubscriptionExpiryDate > DateTime.Now ?
                                   user.SubscriptionExpiryDate.Value : DateTime.Now;
                    newExpiryDate = startDate.AddMonths(months);
                }

                user.IsMember = true;
                user.SubscriptionExpiryDate = newExpiryDate;

                _context.Update(user);
                await _context.SaveChangesAsync();

                var transaction = new PaymentTransaction
                {
                    UserId = userId,
                    PackageType = months switch
                    {
                        1 => VipPackageType.OneMonth,
                        3 => VipPackageType.ThreeMonths,
                        6 => VipPackageType.SixMonths,
                        12 => VipPackageType.OneYear,
                        _ => VipPackageType.Lifetime
                    },
                    Amount = 0, 
                    TransactionDate = DateTime.Now,
                    Status = "Completed",
                    Notes = "Gia hạn bởi Admin"
                };

                _context.PaymentTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                var durationText = months == 999 ? "trọn đời" : $"{months} tháng";
                TempData["SuccessMessage"] = $"Đã gia hạn VIP {durationText} cho người dùng {user.Email}. Hết hạn: {newExpiryDate:dd/MM/yyyy}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi gia hạn VIP: " + ex.Message;
            }

            return RedirectToAction(nameof(EditUser), new { id = userId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction(nameof(ManageUsers));
            }

            var userRoles = await _context.UserRoles.Where(ur => ur.UserId == id).ToListAsync();
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin" || r.Name == "SuperAdmin");

            if (adminRole != null && userRoles.Any(ur => ur.RoleId == adminRole.Id))
            {
                TempData["ErrorMessage"] = "Không thể xóa tài khoản Admin từ trang này. Vui lòng sử dụng trang Quản lý Admin.";
                return RedirectToAction(nameof(ManageUsers));
            }

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa người dùng: {user.Email}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa người dùng: " + ex.Message;
            }

            return RedirectToAction(nameof(ManageUsers));
        }


        [HttpGet]
        public async Task<IActionResult> ManageReviews(
            string searchTerm = "",
            int? rating = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string sortBy = "newest",
            int page = 1)
        {
            const int pageSize = 30;

            var query = _context.Reviews
                .Include(r => r.Book)
                .Include(r => r.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(r =>
                    r.Book.Title.Contains(searchTerm) ||
                    r.User.Email!.Contains(searchTerm) ||
                    (r.Comment != null && r.Comment.Contains(searchTerm)));
            }

            if (rating.HasValue && rating.Value >= 1 && rating.Value <= 5)
            {
                query = query.Where(r => r.Rating == rating.Value);
            }
            if (startDate.HasValue)
            {
                query = query.Where(r => r.CreatedDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                var endDateTime = endDate.Value.AddDays(1).AddSeconds(-1);
                query = query.Where(r => r.CreatedDate <= endDateTime);
            }

            query = sortBy switch
            {
                "oldest" => query.OrderBy(r => r.CreatedDate),
                "rating_high" => query.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedDate),
                "rating_low" => query.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedDate),
                "name_az" => query.OrderBy(r => r.Book.Title),
                "name_za" => query.OrderByDescending(r => r.Book.Title),
                _ => query.OrderByDescending(r => r.CreatedDate) 
            };

            var totalReviews = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalReviews / (double)pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var reviews = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalReviews = totalReviews;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.Rating = rating;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.SortBy = sortBy;

            return View(reviews);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveVIP(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction(nameof(ManageUsers));
            }

            if (!user.IsMember)
            {
                TempData["ErrorMessage"] = "Người dùng này không phải VIP.";
                return RedirectToAction(nameof(ManageUsers));
            }

            try
            {
                user.IsMember = false;
                user.SubscriptionExpiryDate = null;
                _context.Users.Update(user);

                // Xóa role "Member"
                var memberRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Member");
                if (memberRole != null)
                {
                    var userRole = await _context.UserRoles
                        .FirstOrDefaultAsync(ur => ur.UserId == id && ur.RoleId == memberRole.Id);
                    if (userRole != null)
                    {
                        _context.UserRoles.Remove(userRole);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa VIP của người dùng: {user.Email}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa VIP: " + ex.Message;
            }

            return RedirectToAction(nameof(ManageUsers));
        }

        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> SuperAdminAnalytics()
        {
            var now = DateTime.Now;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var lastMonthEnd = thisMonthStart;

            var model = new dynamic[6];

            // 1. Người dùng mới tháng này vs tháng trước
            var newUsersThisMonth = await _context.Users
                .Where(u => u.RegistrationDate >= thisMonthStart)
                .CountAsync();

            var newUsersLastMonth = await _context.Users
                .Where(u => u.RegistrationDate >= lastMonthStart && u.RegistrationDate < lastMonthEnd)
                .CountAsync();

            var userGrowthPercent = newUsersLastMonth > 0
                ? Math.Round(((double)(newUsersThisMonth - newUsersLastMonth) / newUsersLastMonth) * 100, 1)
                : (newUsersThisMonth > 0 ? 100 : 0);

            ViewBag.NewUsersThisMonth = newUsersThisMonth;
            ViewBag.NewUsersLastMonth = newUsersLastMonth;
            ViewBag.UserGrowthPercent = userGrowthPercent;

            // 2. Doanh thu tháng này vs tháng trước
            var revenueThisMonth = await _context.PaymentTransactions
                .Where(t => t.TransactionDate >= thisMonthStart && t.Status == "Completed")
                .SumAsync(t => t.Amount);

            var revenueLastMonth = await _context.PaymentTransactions
                .Where(t => t.TransactionDate >= lastMonthStart && t.TransactionDate < lastMonthEnd && t.Status == "Completed")
                .SumAsync(t => t.Amount);

            var revenueGrowthPercent = revenueLastMonth > 0
                ? Math.Round(((double)(revenueThisMonth - revenueLastMonth) / (double)revenueLastMonth) * 100, 1)
                : (revenueThisMonth > 0 ? 100 : 0);

            ViewBag.RevenueThisMonth = revenueThisMonth;
            ViewBag.RevenueLastMonth = revenueLastMonth;
            ViewBag.RevenueGrowthPercent = revenueGrowthPercent;

            // 3. VIP conversions tháng này vs tháng trước
            var vipThisMonth = await _context.PaymentTransactions
                .Where(t => t.TransactionDate >= thisMonthStart && t.Status == "Completed")
                .CountAsync();

            var vipLastMonth = await _context.PaymentTransactions
                .Where(t => t.TransactionDate >= lastMonthStart && t.TransactionDate < lastMonthEnd && t.Status == "Completed")
                .CountAsync();

            var vipGrowthPercent = vipLastMonth > 0
                ? Math.Round(((double)(vipThisMonth - vipLastMonth) / vipLastMonth) * 100, 1)
                : (vipThisMonth > 0 ? 100 : 0);

            ViewBag.VipThisMonth = vipThisMonth;
            ViewBag.VipLastMonth = vipLastMonth;
            ViewBag.VipGrowthPercent = vipGrowthPercent;

            // 4. Biểu đồ VIP conversions theo ngày trong tháng này
            var vipConversionsByDay = await _context.PaymentTransactions
                .Where(t => t.TransactionDate >= thisMonthStart && t.Status == "Completed")
                .GroupBy(t => t.TransactionDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count(),
                    Revenue = g.Sum(t => t.Amount)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var dailyLabels = new List<string>();
            var dailyCounts = new List<int>();
            var dailyRevenues = new List<decimal>();

            for (int i = 0; i < thisMonthStart.AddMonths(1).Day; i++)
            {
                var date = thisMonthStart.AddDays(i);
                if (date >= now) break;

                dailyLabels.Add(date.Day.ToString());
                var data = vipConversionsByDay.FirstOrDefault(x => x.Date == date.Date);
                dailyCounts.Add(data?.Count ?? 0);
                dailyRevenues.Add(data?.Revenue ?? 0);
            }

            ViewBag.DailyLabels = System.Text.Json.JsonSerializer.Serialize(dailyLabels);
            ViewBag.DailyCounts = System.Text.Json.JsonSerializer.Serialize(dailyCounts);
            ViewBag.DailyRevenues = System.Text.Json.JsonSerializer.Serialize(dailyRevenues);

            // 5. Biểu đồ VIP conversions theo giờ hôm nay
            var todayStart = now.Date;
            var hourlyVips = await _context.PaymentTransactions
                .Where(t => t.TransactionDate >= todayStart && t.Status == "Completed")
                .GroupBy(t => t.TransactionDate.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .ToListAsync();

            var hourlyLabels = new List<string>();
            var hourlyCounts = new List<int>();

            for (int h = 0; h < 24; h++)
            {
                hourlyLabels.Add($"{h:D2}:00");
                var data = hourlyVips.FirstOrDefault(x => x.Hour == h);
                hourlyCounts.Add(data?.Count ?? 0);
            }

            ViewBag.HourlyLabels = System.Text.Json.JsonSerializer.Serialize(hourlyLabels);
            ViewBag.HourlyCounts = System.Text.Json.JsonSerializer.Serialize(hourlyCounts);

            // 6. Top VIP packages
            var topPackages = await _context.PaymentTransactions
                .Where(t => t.Status == "Completed")
                .GroupBy(t => t.PackageType)
                .Select(g => new { Package = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            ViewBag.TopPackagesLabels = System.Text.Json.JsonSerializer.Serialize(
                topPackages.Select(x => x.Package.GetName()).ToList());
            ViewBag.TopPackagesCounts = System.Text.Json.JsonSerializer.Serialize(
                topPackages.Select(x => x.Count).ToList());

            return View();
        }
    }
}