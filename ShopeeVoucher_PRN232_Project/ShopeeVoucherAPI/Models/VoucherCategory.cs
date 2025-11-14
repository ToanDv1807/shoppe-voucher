using System;
using System.Collections.Generic;

namespace ShopeeVoucherAPI.Models;

public partial class VoucherCategory
{
    public int VoucherCategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
}
