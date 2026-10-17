using System;
using System.Collections.Generic;

namespace Thanaphong_7918_L005.Models;

public partial class Game
{
    public int GameId { get; set; }

    public string GameName { get; set; } = null!;

    public string? CoverImage { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Package> Packages { get; set; } = new List<Package>();
}
