using System;
using System.Collections.Generic;

namespace Thanaphong_7918_L005.Models;

public partial class Package
{
    public int PackageId { get; set; }

    public int GameId { get; set; }

    public string PackageName { get; set; } = null!;

    public decimal Price { get; set; }

    public int ItemAmount { get; set; }

    public virtual Game Game { get; set; } = null!;
}
