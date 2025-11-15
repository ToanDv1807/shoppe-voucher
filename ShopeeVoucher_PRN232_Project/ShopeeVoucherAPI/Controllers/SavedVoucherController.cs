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
    [Authorize]
    public class SavedVoucherController : ControllerBase
    {
        private readonly ShopeeVoucherDbContext _context;
        private readonly ILogger<SavedVoucherController> _logger;

        public SavedVoucherController(ShopeeVoucherDbContext context, ILogger<SavedVoucherController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/SavedVoucher
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SavedVoucherDto>>> GetSavedVouchers()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            _logger.LogInformation("=== GET SavedVouchers - UserId: {UserId} ===", userId);

            var savedVouchers = await _context.SavedVouchers
                .Include(sv => sv.Voucher)
                .ThenInclude(v => v!.Category)
                .Where(sv => sv.UserId == userId)
                .Select(sv => new SavedVoucherDto
                {
                    SavedId = sv.SavedId,
                    UserId = sv.UserId,
                    VoucherId = sv.VoucherId,
                    SavedAt = sv.SavedAt,
                    Voucher = new VoucherDto
                    {
                        VoucherId = sv.Voucher!.VoucherId,
                        Title = sv.Voucher.Title ?? "",
                        Description = sv.Voucher.Description ?? "",
                        VoucherCode = sv.Voucher.VoucherCode ?? "",
                        DiscountValue = sv.Voucher.DiscountValue,
                        MinOrder = sv.Voucher.MinOrder,
                        ExpiryDate = sv.Voucher.ExpiryDate,
                        Link = sv.Voucher.Link,
                        CategoryId = sv.Voucher.CategoryId,
                        CategoryName = sv.Voucher.Category != null ? sv.Voucher.Category.CategoryName : "",
                        Color = GetColorByCategory(sv.Voucher.Category != null ? sv.Voucher.Category.CategoryName : "")
                    }
                })
                .ToListAsync();

            _logger.LogInformation("Found {Count} saved vouchers", savedVouchers.Count);
            return Ok(savedVouchers);
        }

        // POST: api/SavedVoucher
        [HttpPost]
        public async Task<ActionResult<SavedVoucherDto>> SaveVoucher([FromBody] int voucherId)
        {
            _logger.LogInformation("=== POST SaveVoucher START ===");
            _logger.LogInformation("Request Body (voucherId): {VoucherId}", voucherId);
            _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
            _logger.LogInformation("Request Headers: {Headers}", string.Join(", ", Request.Headers.Select(h => $"{h.Key}={h.Value}")));

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("User Claim (NameIdentifier): {UserIdClaim}", userIdClaim);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogError("User claim is null or empty!");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var userId = int.Parse(userIdClaim);
            _logger.LogInformation("Parsed UserId: {UserId}", userId);

            // Validate voucherId
            if (voucherId <= 0)
            {
                _logger.LogWarning("Invalid voucherId: {VoucherId}", voucherId);
                return BadRequest(new { message = "Invalid voucher ID" });
            }

            // Check if voucher exists
            var voucherExists = await _context.Vouchers.AnyAsync(v => v.VoucherId == voucherId);
            _logger.LogInformation("Voucher exists check - VoucherId: {VoucherId}, Exists: {Exists}", voucherId, voucherExists);

            if (!voucherExists)
            {
                _logger.LogWarning("Voucher not found - VoucherId: {VoucherId}", voucherId);
                return NotFound(new { message = "Voucher not found" });
            }

            // Check if already saved
            var existingSaved = await _context.SavedVouchers
                .FirstOrDefaultAsync(sv => sv.UserId == userId && sv.VoucherId == voucherId);

            _logger.LogInformation("Existing saved check - UserId: {UserId}, VoucherId: {VoucherId}, Found: {Found}", 
                userId, voucherId, existingSaved != null);

            if (existingSaved != null)
            {
                _logger.LogWarning("Voucher already saved - SavedId: {SavedId}", existingSaved.SavedId);
                return BadRequest(new { message = "Voucher already saved" });
            }

            var savedVoucher = new SavedVoucher
            {
                UserId = userId,
                VoucherId = voucherId,
                SavedAt = DateTime.Now
            };

            _logger.LogInformation("Creating new SavedVoucher - UserId: {UserId}, VoucherId: {VoucherId}", userId, voucherId);

            try
            {
                _context.SavedVouchers.Add(savedVoucher);
                await _context.SaveChangesAsync();

                _logger.LogInformation("SavedVoucher created successfully - SavedId: {SavedId}", savedVoucher.SavedId);
                return Ok(new { message = "Voucher saved successfully", savedId = savedVoucher.SavedId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving voucher to database");
                return StatusCode(500, new { message = "An error occurred while saving voucher", error = ex.Message });
            }
        }

        // DELETE: api/SavedVoucher/5
        [HttpDelete("{voucherId}")]
        public async Task<IActionResult> UnsaveVoucher(int voucherId)
        {
            _logger.LogInformation("=== DELETE UnsaveVoucher - VoucherId: {VoucherId} ===", voucherId);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            _logger.LogInformation("UserId: {UserId}", userId);

            var savedVoucher = await _context.SavedVouchers
                .FirstOrDefaultAsync(sv => sv.UserId == userId && sv.VoucherId == voucherId);

            if (savedVoucher == null)
            {
                _logger.LogWarning("SavedVoucher not found - UserId: {UserId}, VoucherId: {VoucherId}", userId, voucherId);
                return NotFound(new { message = "Saved voucher not found" });
            }

            _logger.LogInformation("Deleting SavedVoucher - SavedId: {SavedId}", savedVoucher.SavedId);

            _context.SavedVouchers.Remove(savedVoucher);
            await _context.SaveChangesAsync();

            _logger.LogInformation("SavedVoucher deleted successfully");
            return Ok(new { message = "Voucher removed from saved" });
        }

        // GET: api/SavedVoucher/Check/5
        [HttpGet("Check/{voucherId}")]
        public async Task<ActionResult<bool>> CheckIfSaved(int voucherId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var isSaved = await _context.SavedVouchers
                .AnyAsync(sv => sv.UserId == userId && sv.VoucherId == voucherId);

            _logger.LogDebug("Check if saved - UserId: {UserId}, VoucherId: {VoucherId}, IsSaved: {IsSaved}", 
                userId, voucherId, isSaved);

            return Ok(new { isSaved });
        }

        private static string GetColorByCategory(string categoryName)
        {
            return categoryName.ToLower() switch
            {
                "shopee" => "#ee4d2d",
                "lazada" => "#0f1cae",
                "tiki" => "#1a94ff",
                _ => "#667eea"
            };
        }
    }
}