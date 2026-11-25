using System;
using System.Collections.Generic;

namespace Thanaphong_7918_L005.Models;

public partial class Promotion
{
    public int PromoId { get; set; }

    public string PromoCode { get; set; } = null!;

    public string Title { get; set; } = null!;

    public decimal DiscountAmount { get; set; }

    public string? BannerImage { get; set; }

    public bool? IsActive { get; set; }
}
