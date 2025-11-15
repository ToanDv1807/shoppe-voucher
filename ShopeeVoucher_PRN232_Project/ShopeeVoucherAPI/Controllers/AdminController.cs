using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopeeVoucherAPI.DTOs;
using ShopeeVoucherAPI.Models;
using System.Security.Claims;

namespace ShopeeVoucherAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ShopeeVoucherDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ShopeeVoucherDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Admin/Stats
        [HttpGet("Stats")]
        public async Task<ActionResult<AdminStatsDto>> GetStats()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalVouchers = await _context.Vouchers.CountAsync();
            var activeVouchers = await _context.Vouchers.CountAsync(v => v.ExpiryDate == null || v.ExpiryDate > DateTime.Now);
            var expiredVouchers = await _context.Vouchers.CountAsync(v => v.ExpiryDate != null && v.ExpiryDate <= DateTime.Now);
            var totalSavedVouchers = await _context.SavedVouchers.CountAsync();

            return Ok(new AdminStatsDto
            {
                TotalUsers = totalUsers,
                TotalVouchers = totalVouchers,
                ActiveVouchers = activeVouchers,
                ExpiredVouchers = expiredVouchers,
                TotalSavedVouchers = totalSavedVouchers
            });
        }

        // GET: api/Admin/Users
        [HttpGet("Users")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Roles)
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    CreatedAt = u.CreatedAt,
                    Roles = u.Roles.Select(r => r.RoleName).ToList()
                })
                .ToListAsync();

            return Ok(users);
        }

        // PUT: api/Admin/Users/5/ToggleRole
        [HttpPut("Users/{userId}/ToggleRole")]
        public async Task<IActionResult> ToggleUserRole(int userId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (userId == currentUserId)
            {
                return BadRequest(new { message = "Cannot modify your own role" });
            }

            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");

            if (adminRole == null || userRole == null)
            {
                return StatusCode(500, new { message = "Roles not properly configured" });
            }

            // ✅ NEW LOGIC: Toggle between ONLY Admin or ONLY User (exclusive)
            if (user.Roles.Any(r => r.RoleId == adminRole.RoleId))
            {
                // Currently Admin → Change to User only
                user.Roles.Clear(); // Remove all roles
                user.Roles.Add(userRole); // Add only User role
            }
            else
            {
                // Currently User → Change to Admin only
                user.Roles.Clear(); // Remove all roles
                user.Roles.Add(adminRole); // Add only Admin role
            }

            await _context.SaveChangesAsync();

            var currentRoles = user.Roles.Select(r => r.RoleName).ToList();
            var roleStatus = currentRoles.Contains("Admin") ? "Admin" : "User";

            return Ok(new { 
                message = $"User role changed to {roleStatus}", 
                roles = currentRoles 
            });
        }

        // DELETE: api/Admin/Users/5
        [HttpDelete("Users/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            _logger.LogInformation("=== DELETE USER - UserId: {UserId} ===", userId);

            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (userId == currentUserId)
            {
                _logger.LogWarning("Attempted to delete own account - UserId: {UserId}", userId);
                return BadRequest(new { message = "Cannot delete your own account" });
            }

            // ✅ Load user with all relationships
            var user = await _context.Users
                .Include(u => u.Roles) // Include roles for cascade delete
                .Include(u => u.SavedVouchers) // Include saved vouchers
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                _logger.LogWarning("User not found - UserId: {UserId}", userId);
                return NotFound(new { message = "User not found" });
            }

            try
            {
                // ✅ Explicitly remove role relationships
                user.Roles.Clear();

                // ✅ Remove saved vouchers (optional - or let cascade handle it)
                if (user.SavedVouchers.Any())
                {
                    _logger.LogInformation("Removing {Count} saved vouchers for user {UserId}", 
                        user.SavedVouchers.Count, userId);
                    _context.SavedVouchers.RemoveRange(user.SavedVouchers);
                }

                // ✅ Remove user
                _context.Users.Remove(user);
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("User deleted successfully - UserId: {UserId}", userId);
                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while deleting user" });
            }
        }

        // GET: api/Admin/Vouchers
        [HttpGet("Vouchers")]
        public async Task<ActionResult<IEnumerable<AdminVoucherDto>>> GetVouchers()
        {
            var vouchers = await _context.Vouchers
                .Include(v => v.Category)
                .Select(v => new AdminVoucherDto
                {
                    VoucherId = v.VoucherId,
                    Title = v.Title ?? "",
                    VoucherCode = v.VoucherCode ?? "",
                    DiscountValue = v.DiscountValue,
                    MinOrder = v.MinOrder,
                    ExpiryDate = v.ExpiryDate,
                    Link = v.Link,
                    CategoryId = v.CategoryId,
                    CategoryName = v.Category != null ? v.Category.CategoryName : "",
                    IsActive = v.ExpiryDate == null || v.ExpiryDate > DateTime.Now,
                    CreatedAt = v.CreatedAt
                })
                .ToListAsync();

            return Ok(vouchers);
        }

        // GET: api/Admin/Categories
        [HttpGet("Categories")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            var categories = await _context.VoucherCategories
                .Select(c => new CategoryDto
                {
                    CategoryId = c.VoucherCategoryId,
                    CategoryName = c.CategoryName
                })
                .ToListAsync();

            return Ok(categories);
        }

        // POST: api/Admin/Vouchers
        [HttpPost("Vouchers")]
        public async Task<ActionResult<AdminVoucherDto>> CreateVoucher([FromBody] CreateVoucherDto dto)
        {
            _logger.LogInformation("=== CREATE VOUCHER - Title: {Title} ===", dto.Title);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if category exists
            var categoryExists = await _context.VoucherCategories.AnyAsync(c => c.VoucherCategoryId == dto.CategoryId);
            if (!categoryExists)
            {
                return BadRequest(new { message = "Invalid category" });
            }

            // Check if voucher code already exists
            var codeExists = await _context.Vouchers.AnyAsync(v => v.VoucherCode == dto.VoucherCode);
            if (codeExists)
            {
                return BadRequest(new { message = "Voucher code already exists" });
            }

            var voucher = new Voucher
            {
                Title = dto.Title,
                Description = dto.Description,
                VoucherCode = dto.VoucherCode,
                DiscountValue = dto.DiscountValue,
                MinOrder = dto.MinOrder,
                ExpiryDate = dto.ExpiryDate,
                Link = dto.Link,
                CategoryId = dto.CategoryId,
                CreatedAt = DateTime.Now
            };

            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();

            // Load category for response
            await _context.Entry(voucher).Reference(v => v.Category).LoadAsync();

            var result = new AdminVoucherDto
            {
                VoucherId = voucher.VoucherId,
                Title = voucher.Title ?? "",
                VoucherCode = voucher.VoucherCode ?? "",
                DiscountValue = voucher.DiscountValue,
                MinOrder = voucher.MinOrder,
                ExpiryDate = voucher.ExpiryDate,
                Link = voucher.Link,
                CategoryId = voucher.CategoryId,
                CategoryName = voucher.Category?.CategoryName,
                IsActive = voucher.ExpiryDate == null || voucher.ExpiryDate > DateTime.Now,
                CreatedAt = voucher.CreatedAt
            };

            _logger.LogInformation("Voucher created successfully - VoucherId: {VoucherId}", voucher.VoucherId);
            return CreatedAtAction(nameof(GetVouchers), new { id = voucher.VoucherId }, result);
        }

        // PUT: api/Admin/Vouchers/5
        [HttpPut("Vouchers/{id}")]
        public async Task<IActionResult> UpdateVoucher(int id, [FromBody] UpdateVoucherDto dto)
        {
            _logger.LogInformation("=== UPDATE VOUCHER - VoucherId: {VoucherId} ===", id);

            if (id != dto.VoucherId)
            {
                return BadRequest(new { message = "Voucher ID mismatch" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                return NotFound(new { message = "Voucher not found" });
            }

            // Check if category exists
            var categoryExists = await _context.VoucherCategories.AnyAsync(c => c.VoucherCategoryId == dto.CategoryId);
            if (!categoryExists)
            {
                return BadRequest(new { message = "Invalid category" });
            }

            // Check if voucher code already exists (excluding current voucher)
            var codeExists = await _context.Vouchers.AnyAsync(v => v.VoucherCode == dto.VoucherCode && v.VoucherId != id);
            if (codeExists)
            {
                return BadRequest(new { message = "Voucher code already exists" });
            }

            // Update voucher
            voucher.Title = dto.Title;
            voucher.Description = dto.Description;
            voucher.VoucherCode = dto.VoucherCode;
            voucher.DiscountValue = dto.DiscountValue;
            voucher.MinOrder = dto.MinOrder;
            voucher.ExpiryDate = dto.ExpiryDate;
            voucher.Link = dto.Link;
            voucher.CategoryId = dto.CategoryId;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Voucher updated successfully - VoucherId: {VoucherId}", id);
                return Ok(new { message = "Voucher updated successfully" });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Vouchers.AnyAsync(v => v.VoucherId == id))
                {
                    return NotFound(new { message = "Voucher not found" });
                }
                throw;
            }
        }

        // DELETE: api/Admin/Vouchers/5
        [HttpDelete("Vouchers/{id}")]
        public async Task<IActionResult> DeleteVoucher(int id)
        {
            _logger.LogInformation("=== DELETE VOUCHER - VoucherId: {VoucherId} ===", id);

            var voucher = await _context.Vouchers
                .Include(v => v.SavedVouchers)
                .FirstOrDefaultAsync(v => v.VoucherId == id);

            if (voucher == null)
            {
                return NotFound(new { message = "Voucher not found" });
            }

            // Check if voucher has saved vouchers
            if (voucher.SavedVouchers.Any())
            {
                _logger.LogWarning("Cannot delete voucher - Has {Count} saved references", voucher.SavedVouchers.Count);
                return BadRequest(new { message = $"Cannot delete voucher. It has been saved by {voucher.SavedVouchers.Count} user(s). Consider expiring it instead." });
            }

            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Voucher deleted successfully - VoucherId: {VoucherId}", id);
            return Ok(new { message = "Voucher deleted successfully" });
        }

        // PUT: api/Admin/Vouchers/5/ToggleActive
        [HttpPut("Vouchers/{id}/ToggleActive")]
        public async Task<IActionResult> ToggleVoucherActive(int id)
        {
            _logger.LogInformation("=== TOGGLE VOUCHER ACTIVE - VoucherId: {VoucherId} ===", id);

            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                return NotFound(new { message = "Voucher not found" });
            }

            // Toggle by setting expiry date
            if (voucher.ExpiryDate == null || voucher.ExpiryDate > DateTime.Now)
            {
                // Make inactive - set expiry to now
                voucher.ExpiryDate = DateTime.Now.AddDays(-1);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Voucher set to inactive - VoucherId: {VoucherId}", id);
                return Ok(new { message = "Voucher deactivated", isActive = false });
            }
            else
            {
                // Make active - set expiry to 30 days from now
                voucher.ExpiryDate = DateTime.Now.AddDays(30);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Voucher set to active - VoucherId: {VoucherId}", id);
                return Ok(new { message = "Voucher activated", isActive = true });
            }
        }
    }
}