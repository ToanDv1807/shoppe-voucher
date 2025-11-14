using System;
using System.Collections.Generic;

namespace ShopeeVoucherAPI.Models;

public partial class SavedVoucher
{
    public int SavedId { get; set; }

    public int UserId { get; set; }

    public int VoucherId { get; set; }

    public DateTime? SavedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual Voucher Voucher { get; set; } = null!;
}
