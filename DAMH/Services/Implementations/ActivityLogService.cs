
using DAMH.Data;
using DAMH.Models;
using DAMH.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DAMH.Services.Implementations
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly LibraryContext _context;

        public ActivityLogService(LibraryContext context)
        {
            _context = context;
        }

        public async Task LogActivityAsync(string userId, string action, string section,
            string? entityId = null, string? entityName = null, object? oldValues = null,
            object? newValues = null, string? notes = null, string? ipAddress = null)
        {
            try
            {
                var log = new ActivityLog
                {
                    UserId = userId,
                    Action = action,
                    Section = section,
                    EntityId = entityId,
                    EntityName = entityName,
                    OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                    NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                    Notes = notes,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.Now
                };

                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging activity: {ex.Message}");
            }
        }

        public async Task<(IEnumerable<ActivityLog> Logs, int TotalCount)> GetPagedLogsAsync(
            int page, int pageSize, string? userId = null, string? action = null,
            string? section = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.ActivityLogs.Include(a => a.User).AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(a => a.UserId == userId);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(a => a.Action == action);

            if (!string.IsNullOrEmpty(section))
                query = query.Where(a => a.Section == section);

            if (startDate.HasValue)
                query = query.Where(a => a.Timestamp >= startDate.Value);

            if (endDate.HasValue)
            {
                var endDateTime = endDate.Value.AddDays(1).AddSeconds(-1);
                query = query.Where(a => a.Timestamp <= endDateTime);
            }

            var totalCount = await query.CountAsync();

            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (logs, totalCount);
        }

        public async Task<IEnumerable<ActivityLog>> GetUserActivitiesAsync(string userId, int limit = 50)
        {
            return await _context.ActivityLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivityLog>> GetRecentActivitiesAsync(int limit = 100)
        {
            return await _context.ActivityLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToListAsync();
        }
    }
}