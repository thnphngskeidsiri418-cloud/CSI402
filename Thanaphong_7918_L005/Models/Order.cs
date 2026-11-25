using System;
using System.Collections.Generic;

namespace Thanaphong_7918_L005.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public int PackageId { get; set; }

    public string GameUid { get; set; } = null!;

    public string? GameServer { get; set; }

    public decimal TotalPrice { get; set; }

    public string? SlipImage { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Package Package { get; set; } = null!;

    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();

    public virtual User User { get; set; } = null!;
}
