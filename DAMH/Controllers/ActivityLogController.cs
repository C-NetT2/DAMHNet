using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DAMH.Services.Interfaces;
using DAMH.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DAMH.Controllers
{
    // Both Admin and SuperAdmin can view activity logs
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ActivityLogController : Controller
    {
        private readonly IActivityLogService _activityLogService;
        private readonly LibraryContext _context;

        public ActivityLogController(IActivityLogService activityLogService, LibraryContext context)
        {
            _activityLogService = activityLogService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? userId = null,
            string? action = null,
            string? section = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1)
        {
            const int pageSize = 50;

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isSuperAdmin = User.IsInRole("SuperAdmin");

            // Apply role-based filtering
            string? effectiveUserId = userId;
            string? effectiveSection = section;

            if (!isSuperAdmin)
            {
                // Regular Admin can only see their own logs
                effectiveUserId = currentUserId;

                // Regular Admin cannot see SuperAdmin section logs
                if (section == "SuperAdmin")
                {
                    effectiveSection = null;
                }
            }

            var (logs, totalCount) = await _activityLogService.GetPagedLogsAsync(
                page, pageSize, effectiveUserId, action, effectiveSection, startDate, endDate);

            // Additional filtering for regular Admin: remove SuperAdmin section logs
            if (!isSuperAdmin)
            {
                logs = logs.Where(l => l.Section != "SuperAdmin").ToList();
                totalCount = logs.Count();
            }

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.UserId = userId;
            ViewBag.Action = action;
            ViewBag.Section = section;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.IsSuperAdmin = isSuperAdmin;

            // FIXED: Show ALL users in the dropdown, not just users with logs
            // This ensures SuperAdmin users appear even if they haven't created logs yet
            if (isSuperAdmin)
            {
                // SuperAdmin can see all users
                ViewBag.Users = await _context.Users
                    .OrderBy(u => u.Email)
                    .ToListAsync();
            }
            else
            {
                // Regular Admin only sees themselves
                ViewBag.Users = await _context.Users
                    .Where(u => u.Id == currentUserId)
                    .ToListAsync();
            }

            return View(logs);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var log = await _context.ActivityLogs
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.ActivityLogId == id);

            if (log == null)
                return NotFound();

            // Permission check
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isSuperAdmin = User.IsInRole("SuperAdmin");

            if (!isSuperAdmin)
            {
                // Regular Admin cannot view logs from other users or SuperAdmin section
                if (log.UserId != currentUserId || log.Section == "SuperAdmin")
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền xem log này.";
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(log);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")] // Only SuperAdmin can clear old logs
        public async Task<IActionResult> ClearOldLogs(int days = 90)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-days);
                var oldLogs = await _context.ActivityLogs
                    .Where(a => a.Timestamp < cutoffDate)
                    .ToListAsync();

                _context.ActivityLogs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã xóa {oldLogs.Count} bản ghi cũ hơn {days} ngày.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xóa: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Export(
            string? userId = null,
            string? action = null,
            string? section = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _context.ActivityLogs.Include(a => a.User).AsQueryable();

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isSuperAdmin = User.IsInRole("SuperAdmin");

            if (!isSuperAdmin)
            {
                // Regular Admin can only export their own logs
                query = query.Where(a => a.UserId == currentUserId && a.Section != "SuperAdmin");
            }
            else
            {
                // SuperAdmin can export with filters
                if (!string.IsNullOrEmpty(userId))
                    query = query.Where(a => a.UserId == userId);

                if (!string.IsNullOrEmpty(section))
                    query = query.Where(a => a.Section == section);
            }

            if (!string.IsNullOrEmpty(action))
                query = query.Where(a => a.Action == action);

            if (startDate.HasValue)
                query = query.Where(a => a.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.Timestamp <= endDate.Value.AddDays(1).AddSeconds(-1));

            var logs = await query.OrderByDescending(a => a.Timestamp).ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Thời gian,Người dùng,Hành động,Phần,Đối tượng,Tên đối tượng,Địa chỉ IP");

            foreach (var log in logs)
            {
                csv.AppendLine($"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.User.Email}\",\"{log.Action}\",\"{log.Section}\",\"{log.EntityId}\",\"{log.EntityName}\",\"{log.IpAddress}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"ActivityLog_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
    }
}