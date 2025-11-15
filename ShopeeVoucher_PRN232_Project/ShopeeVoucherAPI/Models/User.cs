using System;
using System.Collections.Generic;

namespace ShopeeVoucherAPI.Models;

public partial class User
{
    public int UserId { get; set; }

    public string? FullName { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<SavedVoucher> SavedVouchers { get; set; } = new List<SavedVoucher>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
