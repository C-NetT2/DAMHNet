using DAMH.Data;
using DAMH.Models;
using DAMH.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DAMH.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly LibraryContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserService(LibraryContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<ApplicationUser?> GetUserWithDetailsAsync(string userId)
        {
            return await _context.Users
                .Include(u => u.ReadingHistories).ThenInclude(rh => rh.Book)
                .Include(u => u.ReadingHistories).ThenInclude(rh => rh.Chapter)
                .Include(u => u.Reviews).ThenInclude(r => r.Book)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<(IEnumerable<ApplicationUser> Users, int TotalCount)> GetPagedUsersAsync(int page, int pageSize, string searchTerm = "")
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(u => u.Email!.Contains(searchTerm) || (u.FullName != null && u.FullName.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.RegistrationDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, totalCount);
        }

        public async Task<bool> UpdateUserAsync(ApplicationUser user)
        {
            try
            {
                var result = await _userManager.UpdateAsync(user);
                return result.Succeeded;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                var userRoles = await _context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
                var adminRole = await _roleManager.FindByNameAsync("Admin");
                var superAdminRole = await _roleManager.FindByNameAsync("SuperAdmin");

                if ((adminRole != null && userRoles.Any(ur => ur.RoleId == adminRole.Id)) ||
                    (superAdminRole != null && userRoles.Any(ur => ur.RoleId == superAdminRole.Id)))
                {
                    return false;
                }

                var result = await _userManager.DeleteAsync(user);
                return result.Succeeded;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ExtendVipAsync(string userId, int months)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                DateTime newExpiryDate;
                if (months == 999)
                {
                    newExpiryDate = DateTime.Now.AddYears(100);
                }
                else
                {
                    var startDate = user.SubscriptionExpiryDate > DateTime.Now ? user.SubscriptionExpiryDate.Value : DateTime.Now;
                    newExpiryDate = startDate.AddMonths(months);
                }

                user.IsMember = true;
                user.SubscriptionExpiryDate = newExpiryDate;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded) return false;

                var isMember = await _userManager.IsInRoleAsync(user, "Member");
                if (!isMember)
                {
                    await _userManager.AddToRoleAsync(user, "Member");
                }

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

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveVipAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                user.IsMember = false;
                user.SubscriptionExpiryDate = null;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded) return false;

                var memberRole = await _roleManager.FindByNameAsync("Member");
                if (memberRole != null)
                {
                    var userRole = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == memberRole.Id);
                    if (userRole != null)
                    {
                        _context.UserRoles.Remove(userRole);
                        await _context.SaveChangesAsync();
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsUserVipActiveAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            return user.IsMember && (user.SubscriptionExpiryDate == null || user.SubscriptionExpiryDate > DateTime.Now);
        }

        public async Task<int> GetActiveVipCountAsync()
        {
            return await _context.Users.Where(u => u.IsMember && u.SubscriptionExpiryDate > DateTime.Now).CountAsync();
        }

        public async Task<int> GetTotalUsersAsync()
        {
            return await _context.Users.CountAsync();
        }

        public async Task<int> GetNewUsersThisMonthAsync()
        {
            var now = DateTime.Now;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            return await _context.Users.Where(u => u.RegistrationDate >= thisMonthStart).CountAsync();
        }

        public async Task<int> GetNewUsersLastMonthAsync()
        {
            var now = DateTime.Now;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            return await _context.Users.Where(u => u.RegistrationDate >= lastMonthStart && u.RegistrationDate < thisMonthStart).CountAsync();
        }

        public async Task<IEnumerable<ApplicationUser>> GetAdminUsersAsync()
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
            return admins.Concat(superAdmins).OrderBy(a => a.Email);
        }

        public async Task<bool> UserExistsAsync(string userId)
        {
            return await _context.Users.AnyAsync(u => u.Id == userId);
        }

        public async Task<bool> IsAdminAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            return await _userManager.IsInRoleAsync(user, "Admin");
        }

        public async Task<bool> IsSuperAdminAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            return await _userManager.IsInRoleAsync(user, "SuperAdmin");
        }
    }
}