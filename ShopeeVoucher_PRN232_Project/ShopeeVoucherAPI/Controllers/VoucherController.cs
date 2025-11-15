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
    public class VoucherController : ControllerBase
    {
        private readonly ShopeeVoucherDbContext _context;

        public VoucherController(ShopeeVoucherDbContext context)
        {
            _context = context;
        }

        // GET: api/Voucher
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VoucherDto>>> GetVouchers()
        {
            var vouchers = await _context.Vouchers
                .Include(v => v.Category)
                .Select(v => new VoucherDto
                {
                    VoucherId = v.VoucherId,
                    Title = v.Title ?? "",
                    Description = v.Description ?? "",
                    VoucherCode = v.VoucherCode ?? "",
                    DiscountValue = v.DiscountValue,
                    MinOrder = v.MinOrder,
                    ExpiryDate = v.ExpiryDate,
                    Link = v.Link,
                    CategoryId = v.CategoryId,
                    CategoryName = v.Category != null ? v.Category.CategoryName : "",
                    Color = GetColorByCategory(v.Category != null ? v.Category.CategoryName : "")
                })
                .ToListAsync();

            return Ok(vouchers);
        }

        // GET: api/Voucher/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VoucherDto>> GetVoucher(int id)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.Category)
                .FirstOrDefaultAsync(v => v.VoucherId == id);

            if (voucher == null)
            {
                return NotFound();
            }

            var voucherDto = new VoucherDto
            {
                VoucherId = voucher.VoucherId,
                Title = voucher.Title ?? "",
                Description = voucher.Description ?? "",
                VoucherCode = voucher.VoucherCode ?? "",
                DiscountValue = voucher.DiscountValue,
                MinOrder = voucher.MinOrder,
                ExpiryDate = voucher.ExpiryDate,
                Link = voucher.Link,
                CategoryId = voucher.CategoryId,
                CategoryName = voucher.Category?.CategoryName ?? "",
                Color = GetColorByCategory(voucher.Category?.CategoryName ?? "")
            };

            return Ok(voucherDto);
        }

        // GET: api/Voucher/Category/{categoryName}
        [HttpGet("Category/{categoryName}")]
        public async Task<ActionResult<IEnumerable<VoucherDto>>> GetVouchersByCategory(string categoryName)
        {
            var vouchers = await _context.Vouchers
                .Include(v => v.Category)
                .Where(v => v.Category != null && v.Category.CategoryName.ToLower() == categoryName.ToLower())
                .Select(v => new VoucherDto
                {
                    VoucherId = v.VoucherId,
                    Title = v.Title ?? "",
                    Description = v.Description ?? "",
                    VoucherCode = v.VoucherCode ?? "",
                    DiscountValue = v.DiscountValue,
                    MinOrder = v.MinOrder,
                    ExpiryDate = v.ExpiryDate,
                    Link = v.Link,
                    CategoryId = v.CategoryId,
                    CategoryName = v.Category.CategoryName,
                    Color = GetColorByCategory(v.Category.CategoryName)
                })
                .ToListAsync();

            return Ok(vouchers);
        }

        // POST: api/Voucher (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<VoucherDto>> CreateVoucher(VoucherDto voucherDto)
        {
            var voucher = new Voucher
            {
                Title = voucherDto.Title,
                Description = voucherDto.Description,
                VoucherCode = voucherDto.VoucherCode,
                DiscountValue = voucherDto.DiscountValue,
                MinOrder = voucherDto.MinOrder,
                ExpiryDate = voucherDto.ExpiryDate,
                Link = voucherDto.Link,
                CategoryId = voucherDto.CategoryId,
                CreatedAt = DateTime.Now
            };

            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();

            voucherDto.VoucherId = voucher.VoucherId;
            return CreatedAtAction(nameof(GetVoucher), new { id = voucher.VoucherId }, voucherDto);
        }

        // PUT: api/Voucher/5 (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVoucher(int id, VoucherDto voucherDto)
        {
            if (id != voucherDto.VoucherId)
            {
                return BadRequest();
            }

            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                return NotFound();
            }

            voucher.Title = voucherDto.Title;
            voucher.Description = voucherDto.Description;
            voucher.VoucherCode = voucherDto.VoucherCode;
            voucher.DiscountValue = voucherDto.DiscountValue;
            voucher.MinOrder = voucherDto.MinOrder;
            voucher.ExpiryDate = voucherDto.ExpiryDate;
            voucher.Link = voucherDto.Link;
            voucher.CategoryId = voucherDto.CategoryId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VoucherExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Voucher/5 (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVoucher(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                return NotFound();
            }

            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool VoucherExists(int id)
        {
            return _context.Vouchers.Any(e => e.VoucherId == id);
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
