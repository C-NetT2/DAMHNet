using DAMH.Data;
using DAMH.Models;
using DAMH.Services.Interfaces;

namespace DAMH.Services.Interfaces
{
    public interface IActivityLogService
    {
        Task LogActivityAsync(string userId, string action, string section, string? entityId = null,
            string? entityName = null, object? oldValues = null, object? newValues = null,
            string? notes = null, string? ipAddress = null);

        Task<(IEnumerable<ActivityLog> Logs, int TotalCount)> GetPagedLogsAsync(
            int page, int pageSize, string? userId = null, string? action = null,
            string? section = null, DateTime? startDate = null, DateTime? endDate = null);

        Task<IEnumerable<ActivityLog>> GetUserActivitiesAsync(string userId, int limit = 50);
        Task<IEnumerable<ActivityLog>> GetRecentActivitiesAsync(int limit = 100);
    }
}