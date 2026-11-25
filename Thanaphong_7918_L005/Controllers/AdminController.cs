using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Thanaphong_7918_L005.Models;

namespace Thanaphong_7918_L005.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly Csi402dbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IWebHostEnvironment _env;

        public AdminController(Csi402dbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
            _env = env;
        }

        // 1. หน้าหลักแอดมิน (ดึงข้อมูลจริงจากฐานข้อมูล MySQL)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

            // 1. ยอดขายวันนี้ (เฉพาะออเดอร์ที่ชำระเงินแล้ว Paid หรือเติมสำเร็จ Completed)
            var todaySales = await _context.Orders
                .Where(o => o.CreatedAt.Date == today && (o.Status == "Paid" || o.Status == "Completed"))
                .SumAsync(o => (decimal?)o.TotalPrice) ?? 0m;

            // 2. ยอดขายรวมเดือนนี้
            var monthSales = await _context.Orders
                .Where(o => o.CreatedAt >= firstDayOfMonth && (o.Status == "Paid" || o.Status == "Completed"))
                .SumAsync(o => (decimal?)o.TotalPrice) ?? 0m;

            // 3. จำนวนคำสั่งซื้อสำเร็จทั้งหมด และจำนวนที่รอดำเนินการ
            var completedOrdersCount = await _context.Orders
                .CountAsync(o => o.Status == "Completed");
            var pendingOrdersCount = await _context.Orders
                .CountAsync(o => o.Status == "Paid" || o.Status == "Pending");

            // 4. จำนวนผู้ใช้งานทั้งหมด
            var totalUsersCount = await _context.Users.CountAsync();
            var newUsersTodayCount = 0;

            // 5. เกมที่ทำยอดขายสูงสุด 3 อันดับแรก (คำนวณจากยอดขายจริงใน Orders)
            var topGames = await _context.Orders
                .Include(o => o.Package)
                .ThenInclude(p => p.Game)
                .Where(o => (o.Status == "Paid" || o.Status == "Completed") && o.Package != null && o.Package.Game != null)
                .GroupBy(o => o.Package.Game.GameName)
                .Select(g => new
                {
                    GameName = g.Key,
                    TotalRevenue = g.Sum(x => x.TotalPrice),
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(3)
                .ToListAsync();

            // 6. รายการแพ็กเกจ/เกมในระบบสำหรับตารางฝั่งขวา (ดึงจาก Games + Packages จริง)
            var packagesList = await _context.Packages
                .Include(p => p.Game)
                .OrderByDescending(p => p.PackageId)
                .Take(6)
                .ToListAsync();

            ViewBag.TodaySales = todaySales;
            ViewBag.MonthSales = monthSales;
            ViewBag.CompletedOrdersCount = completedOrdersCount;
            ViewBag.PendingOrdersCount = pendingOrdersCount;
            ViewBag.TotalUsersCount = totalUsersCount;
            ViewBag.NewUsersTodayCount = newUsersTodayCount;
            ViewBag.TopGames = topGames;
            ViewBag.PackagesList = packagesList;

            return View();
        }

        // ================= 2. ระบบจัดการเกม + แพ็กเกจ (ฝั่ง Admin) =================
        [HttpGet]
        public async Task<IActionResult> ManageGames()
        {
            var games = await _context.Games
                .Include(g => g.Packages)
                .OrderByDescending(g => g.GameId)
                .ToListAsync();
            return View(games);
        }

        [HttpPost]
        public async Task<IActionResult> SaveGame(
            int gameId,
            string gameName,
            bool isActive,
            IFormFile? coverFile,
            List<string> packageNames,
            List<decimal> packagePrices,
            List<int> packageAmounts)
        {
            if (string.IsNullOrWhiteSpace(gameName))
            {
                TempData["Error"] = "กรุณากรอกชื่อเกม";
                return RedirectToAction("ManageGames");
            }

            // จัดการอัปโหลดรูปปก (ถ้ามีเลือกไฟล์ใหม่)
            string? uploadedPath = null;
            if (coverFile != null && coverFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "games");
                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(coverFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await coverFile.CopyToAsync(fileStream);
                }
                uploadedPath = "/images/games/" + uniqueFileName;
            }

            if (gameId == 0)
            {
                // กรณีเพิ่มเกมใหม่
                var newGame = new Game
                {
                    GameName = gameName,
                    CoverImage = uploadedPath ?? "/images/games/default.png",
                    IsActive = isActive
                };
                _context.Games.Add(newGame);
                await _context.SaveChangesAsync(); // บันทึกก่อนเพื่อให้ได้ GameId

                if (packageNames != null)
                {
                    for (int i = 0; i < packageNames.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(packageNames[i]))
                        {
                            _context.Packages.Add(new Package
                            {
                                GameId = newGame.GameId,
                                PackageName = packageNames[i],
                                Price = (packagePrices != null && i < packagePrices.Count) ? packagePrices[i] : 0,
                                ItemAmount = (packageAmounts != null && i < packageAmounts.Count) ? packageAmounts[i] : 0
                            });
                        }
                    }
                }
            }
            else
            {
                // กรณีแก้ไขเกมเดิม (รวมถึงการเปิด/ปิดแสดงผลหน้าเว็บ)
                var game = await _context.Games
                    .Include(g => g.Packages)
                    .FirstOrDefaultAsync(g => g.GameId == gameId);
                if (game == null) return RedirectToAction("ManageGames");

                game.GameName = gameName;
                game.IsActive = isActive;
                if (uploadedPath != null) game.CoverImage = uploadedPath;

                // กรองรายการแพ็กเกจที่ส่งมาจากฟอร์ม
                var validInputs = new List<(string Name, decimal Price, int Amount)>();
                if (packageNames != null)
                {
                    for (int i = 0; i < packageNames.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(packageNames[i]))
                        {
                            decimal price = (packagePrices != null && i < packagePrices.Count) ? packagePrices[i] : 0;
                            int amount = (packageAmounts != null && i < packageAmounts.Count) ? packageAmounts[i] : 0;
                            validInputs.Add((packageNames[i], price, amount));
                        }
                    }
                }

                var existingPackages = game.Packages.OrderBy(p => p.PackageId).ToList();

                // 1. อัปเดตทับแพ็กเกจเดิมตามลำดับ (รักษา PackageId เดิมไว้เพื่อไม่ให้ติด Foreign Key ใน Orders)
                int minCount = Math.Min(existingPackages.Count, validInputs.Count);
                for (int i = 0; i < minCount; i++)
                {
                    existingPackages[i].PackageName = validInputs[i].Name;
                    existingPackages[i].Price = validInputs[i].Price;
                    existingPackages[i].ItemAmount = validInputs[i].Amount;
                }

                // 2. ถ้ามีแพ็กเกจเพิ่มมาใหม่ ให้สร้างต่อท้าย
                for (int i = minCount; i < validInputs.Count; i++)
                {
                    _context.Packages.Add(new Package
                    {
                        GameId = game.GameId,
                        PackageName = validInputs[i].Name,
                        Price = validInputs[i].Price,
                        ItemAmount = validInputs[i].Amount
                    });
                }

                // 3. ถ้ามีแพ็กเกจถูกลบออก ให้ลบเฉพาะรายการที่ยังไม่เคยมีประวัติการสั่งซื้อใน Orders
                for (int i = minCount; i < existingPackages.Count; i++)
                {
                    var pkgToRemove = existingPackages[i];
                    bool hasOrders = await _context.Orders.AnyAsync(o => o.PackageId == pkgToRemove.PackageId);
                    if (!hasOrders)
                    {
                        _context.Packages.Remove(pkgToRemove);
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = gameId == 0 ? $"เพิ่มเกม '{gameName}' สำเร็จ!" : $"อัปเดตข้อมูลเกม '{gameName}' สำเร็จ!";
            return RedirectToAction("ManageGames");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGame(int gameId)
        {
            var game = await _context.Games.Include(g => g.Packages).FirstOrDefaultAsync(g => g.GameId == gameId);
            if (game != null)
            {
                var packageIds = game.Packages.Select(p => p.PackageId).ToList();
                bool hasOrders = await _context.Orders.AnyAsync(o => packageIds.Contains(o.PackageId));

                if (hasOrders)
                {
                    // ถ้ามีประวัติสั่งซื้อแล้ว ให้ปิดการแสดงผลแทนการลบเพื่อป้องกัน Error Foreign Key
                    game.IsActive = false;
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"เกม '{game.GameName}' มีประวัติการสั่งซื้อในระบบ จึงเปลี่ยนสถานะเป็น 'ปิดแสดงหน้าเว็บ' แทนการลบถาวร";
                    return RedirectToAction("ManageGames");
                }

                _context.Packages.RemoveRange(game.Packages);
                _context.Games.Remove(game);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"ลบเกม '{game.GameName}' เรียบร้อยแล้ว";
            }
            return RedirectToAction("ManageGames");
        }

        // ================= 3. ระบบจัดการโปรโมชั่น & แบนเนอร์หน้าแรก =================
        [HttpGet]
        public async Task<IActionResult> Promotions()
        {
            var promos = await _context.Promotions.OrderByDescending(p => p.PromoId).ToListAsync();
            return View(promos);
        }

        [HttpGet]
        public IActionResult ManagePromotions() => RedirectToAction("Promotions");

        [HttpPost]
        public async Task<IActionResult> SavePromotion(int promoId, string promoCode, string title, decimal discountAmount, bool isActive, IFormFile? bannerFile)
        {
            if (string.IsNullOrWhiteSpace(promoCode) || string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "กรุณากรอกชื่อโปรโมชั่นและรหัสโค้ดให้ครบถ้วน";
                return RedirectToAction("Promotions");
            }

            string cleanCode = promoCode.Trim().ToUpper();

            // จัดการอัปโหลดรูปแบนเนอร์ (ถ้าเลือกไฟล์ใหม่)
            string? uploadedBanner = null;
            if (bannerFile != null && bannerFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "banners");
                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(bannerFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await bannerFile.CopyToAsync(stream);
                }
                uploadedBanner = "/images/banners/" + uniqueFileName;
            }

            if (promoId == 0)
            {
                // กรณีสร้างโปรโมชั่นใหม่
                if (await _context.Promotions.AnyAsync(p => p.PromoCode == cleanCode))
                {
                    TempData["Error"] = $"รหัสโค้ด '{cleanCode}' มีอยู่ในระบบแล้ว";
                    return RedirectToAction("Promotions");
                }

                _context.Promotions.Add(new Promotion
                {
                    PromoCode = cleanCode,
                    Title = title,
                    DiscountAmount = discountAmount,
                    BannerImage = uploadedBanner,
                    IsActive = isActive
                });
                TempData["Success"] = $"สร้างโปรโมชั่น '{cleanCode}' เรียบร้อยแล้ว!";
            }
            else
            {
                // กรณีแก้ไขโปรโมชั่นเดิม
                var promo = await _context.Promotions.FindAsync(promoId);
                if (promo != null)
                {
                    promo.PromoCode = cleanCode;
                    promo.Title = title;
                    promo.DiscountAmount = discountAmount;
                    promo.IsActive = isActive;
                    if (uploadedBanner != null)
                    {
                        promo.BannerImage = uploadedBanner;
                    }
                    TempData["Success"] = $"อัปเดตโปรโมชั่น '{cleanCode}' เรียบร้อยแล้ว!";
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Promotions");
        }

        [HttpPost]
        public async Task<IActionResult> DeletePromotion(int promoId)
        {
            var promo = await _context.Promotions.FindAsync(promoId);
            if (promo != null)
            {
                _context.Promotions.Remove(promo);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"ลบโปรโมชั่น '{promo.PromoCode}' เรียบร้อยแล้ว";
            }
            return RedirectToAction("Promotions");
        }

        // ================= 4. หน้าจัดการสิทธิ์ผู้ใช้งาน (เฉพาะ Admin, Staff, Accountant) =================
        [HttpGet]
        public async Task<IActionResult> Permissions()
        {
            var staffUsers = await _context.Users
                .Where(u => u.Role == "Admin" || u.Role == "Staff" || u.Role == "Accountant")
                .OrderBy(u => u.UserId)
                .ToListAsync();
            return View(staffUsers);
        }

        [HttpGet]
        public IActionResult ManageRoles() => RedirectToAction("Permissions");

        [HttpPost]
        public async Task<IActionResult> UpdateUserRole(int userId, string newRole)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null && (newRole == "Admin" || newRole == "Staff" || newRole == "Accountant"))
            {
                user.Role = newRole;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"อัปเดตสิทธิ์ของ {user.Username} เป็น {newRole} เรียบร้อยแล้ว";
            }
            return RedirectToAction("Permissions");
        }

        [HttpPost]
        public async Task<IActionResult> CreateStaffAccount(string username, string email, string password, string role)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "กรุณากรอกข้อมูลให้ครบถ้วน";
                return RedirectToAction("Permissions");
            }

            if (await _context.Users.AnyAsync(u => u.Email == email.Trim() || u.Username == username.Trim()))
            {
                TempData["Error"] = "ชื่อผู้ใช้หรืออีเมลนี้มีอยู่ในระบบแล้ว";
                return RedirectToAction("Permissions");
            }

            var validRole = (role == "Admin" || role == "Staff" || role == "Accountant") ? role : "Staff";
            var newStaff = new User
            {
                Username = username.Trim(),
                Email = email.Trim(),
                Role = validRole
            };
            newStaff.PasswordHash = _passwordHasher.HashPassword(newStaff, password);

            _context.Users.Add(newStaff);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"สร้างบัญชี {username} ({validRole}) เรียบร้อยแล้ว";
            return RedirectToAction("Permissions");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStaffAccount(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"ลบบัญชีพนักงาน {user.Username} เรียบร้อยแล้ว";
            }
            return RedirectToAction("Permissions");
        }
    }
}