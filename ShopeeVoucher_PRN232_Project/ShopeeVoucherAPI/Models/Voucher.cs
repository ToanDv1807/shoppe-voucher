using System;
using System.Collections.Generic;

namespace ShopeeVoucherAPI.Models;

public partial class Voucher
{
    public int VoucherId { get; set; }

    public string? VoucherCode { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? DiscountValue { get; set; }

    public decimal? MinOrder { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public string? Link { get; set; }

    public int? CategoryId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual VoucherCategory? Category { get; set; }

    public virtual ICollection<SavedVoucher> SavedVouchers { get; set; } = new List<SavedVoucher>();
}
