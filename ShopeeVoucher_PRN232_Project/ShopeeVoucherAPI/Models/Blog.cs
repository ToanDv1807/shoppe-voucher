using System;
using System.Collections.Generic;

namespace ShopeeVoucherAPI.Models;

public partial class Blog
{
    public int BlogId { get; set; }

    public string Title { get; set; } = null!;

    public string? Slug { get; set; }

    public string? Content { get; set; }

    public string? Thumbnail { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? AuthorId { get; set; }

    public int? CategoryId { get; set; }

    public virtual User? Author { get; set; }

    public virtual BlogCategory? Category { get; set; }
}
