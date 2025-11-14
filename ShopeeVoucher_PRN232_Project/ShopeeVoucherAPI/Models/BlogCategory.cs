using System;
using System.Collections.Generic;

namespace ShopeeVoucherAPI.Models;

public partial class BlogCategory
{
    public int BlogCategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public virtual ICollection<Blog> Blogs { get; set; } = new List<Blog>();
}
