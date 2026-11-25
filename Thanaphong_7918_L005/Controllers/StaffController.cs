using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Thanaphong_7918_L005.Models;

namespace Thanaphong_7918_L005.Controllers
{
    [Authorize(Roles = "Admin,Staff,Accountant")]
    public class StaffController : Controller
    {
        private readonly Csi402dbContext _context;
        private readonly IWebHostEnvironment _env;

        public StaffController(Csi402dbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // 1. หน้าหลักของพนักงาน: คิวรอเติมเกม (สถานะ Paid) และรายการที่เติมเสร็จแล้ว
        [Authorize(Roles = "Staff,Admin")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Package)
                .ThenInclude(p => p.Game)
                .Where(o => o.Status == "Paid" || o.Status == "Completed")
                .OrderByDescending(o => o.Status == "Paid") // เอาคิวรอเติม (Paid) ขึ้นบนสุด
                .ThenBy(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // 2. พนักงานกดยืนยันเติมเกมสำเร็จ -> เปลี่ยนสถานะ Completed + แจกแต้มสะสมให้ลูกค้า
        [Authorize(Roles = "Staff,Admin")]
        [HttpPost]
        public async Task<IActionResult> CompleteTopUp(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order != null && order.Status == "Paid")
            {
                order.Status = "Completed";

                // คำนวณแต้มสะสม: ทุกๆ 10 บาท รับ 1 แต้ม (ขั้นต่ำ 1 แต้ม)
                int earnedPoints = Math.Max(1, (int)(order.TotalPrice / 10));
                if (order.User != null)
                {
                    order.User.Points += earnedPoints;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"ปิดงานออเดอร์ #ORD-{orderId:D4} สำเร็จ! ลูกค้าได้รับ +{earnedPoints} แต้มสะสม";
            }

            return RedirectToAction("Index");
        }

        // 3. พนักงานกดยกเลิกกรณี UID ผิด -> เปลี่ยนสถานะเป็น Rejected เพื่อให้ลูกค้าแจ้งแก้ไขหรือขอคืนเงิน
        [Authorize(Roles = "Staff,Admin")]
        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null && order.Status == "Paid")
            {
                order.Status = "Rejected";
                await _context.SaveChangesAsync();
                TempData["Error"] = $"ยกเลิกออเดอร์ #ORD-{orderId:D4} (แจ้งลูกค้าตรวจสอบ UID)";
            }
            return RedirectToAction("Index");
        }

        // 4. หน้าจัดการเกม (ฝั่ง Staff)
        [Authorize(Roles = "Staff,Admin")]
        [HttpGet]
        public async Task<IActionResult> ManageGames()
        {
            var games = await _context.Games
                .Include(g => g.Packages)
                .OrderByDescending(g => g.GameId)
                .ToListAsync();
            return View(games);
        }

        [Authorize(Roles = "Staff,Admin")]
        [HttpPost]
        public async Task<IActionResult> AddGame(string gameName, IFormFile? coverFile, bool isActive = true)
        {
            if (string.IsNullOrWhiteSpace(gameName))
            {
                TempData["Error"] = "กรุณากรอกชื่อเกม";
                return RedirectToAction("ManageGames");
            }

            string imagePath = "/images/games/default.png";
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
                imagePath = "/images/games/" + uniqueFileName;
            }

            _context.Games.Add(new Game
            {
                GameName = gameName,
                CoverImage = imagePath,
                IsActive = isActive
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = $"เพิ่มเกม '{gameName}' ลงฐานข้อมูลสำเร็จ!";
            return RedirectToAction("ManageGames");
        }

        [Authorize(Roles = "Staff,Admin")]
        [HttpPost]
        public async Task<IActionResult> ToggleGameStatus(int gameId)
        {
            var game = await _context.Games.FindAsync(gameId);
            if (game != null)
            {
                game.IsActive = !(game.IsActive ?? false);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ManageGames");
        }

        [Authorize(Roles = "Staff,Admin")]
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

            Game game;
            if (gameId == 0)
            {
                game = new Game
                {
                    GameName = gameName,
                    CoverImage = uploadedPath ?? "/images/games/default.png",
                    IsActive = isActive
                };
                _context.Games.Add(game);
                await _context.SaveChangesAsync();
            }
            else
            {
                game = await _context.Games.Include(g => g.Packages).FirstOrDefaultAsync(g => g.GameId == gameId);
                if (game == null) return RedirectToAction("ManageGames");

                game.GameName = gameName;
                game.IsActive = isActive;
                if (uploadedPath != null) game.CoverImage = uploadedPath;

                _context.Packages.RemoveRange(game.Packages);
            }

            if (packageNames != null)
            {
                for (int i = 0; i < packageNames.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(packageNames[i]))
                    {
                        _context.Packages.Add(new Package
                        {
                            GameId = game.GameId,
                            PackageName = packageNames[i],
                            Price = (packagePrices != null && i < packagePrices.Count) ? packagePrices[i] : 0,
                            ItemAmount = (packageAmounts != null && i < packageAmounts.Count) ? packageAmounts[i] : 0
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = gameId == 0 ? $"เพิ่มเกม '{gameName}' สำเร็จ!" : $"อัปเดตข้อมูลเกม '{gameName}' สำเร็จ!";
            return RedirectToAction("ManageGames");
        }

        [Authorize(Roles = "Staff,Admin")]
        [HttpPost]
        public async Task<IActionResult> DeleteGame(int gameId)
        {
            var game = await _context.Games.Include(g => g.Packages).FirstOrDefaultAsync(g => g.GameId == gameId);
            if (game != null)
            {
                _context.Packages.RemoveRange(game.Packages);
                _context.Games.Remove(game);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"ลบเกม '{game.GameName}' เรียบร้อยแล้ว";
            }
            return RedirectToAction("ManageGames");
        }

        // ================= 4.5 หน้าแชทช่วยเหลือลูกค้า (แยกออกมาให้ Staff เลือกตอบรายคนได้) =================
        [Authorize(Roles = "Staff,Admin")]
        [HttpGet]
        public async Task<IActionResult> SupportChat(int? customerId)
        {
            var customerIds = await _context.Messages
                .Where(m => (m.ReceiverId == null || m.ReceiverId == 0) && m.SenderId > 0)
                .Select(m => m.SenderId)
                .Distinct()
                .ToListAsync();

            var customers = await _context.Users
                .Where(u => customerIds.Contains(u.UserId))
                .ToListAsync();

            int? selectedId = customerId ?? customers.FirstOrDefault()?.UserId;
            ViewBag.Customers = customers;
            ViewBag.SelectedCustomer = customers.FirstOrDefault(c => c.UserId == selectedId);

            var chatMessages = new List<Message>();
            if (selectedId != null)
            {
                chatMessages = await _context.Messages
                    .Include(m => m.Sender)
                    .Where(m => (m.SenderId == selectedId && m.ReceiverId == null) || m.ReceiverId == selectedId)
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();
            }

            return View(chatMessages);
        }

        [Authorize(Roles = "Staff,Admin")]
        [HttpPost]
        public async Task<IActionResult> ReplyMessage(int customerId, string messageText)
        {
            if (customerId > 0 && !string.IsNullOrWhiteSpace(messageText))
            {
                var staffIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int.TryParse(staffIdClaim, out int staffId);

               _context.Messages.Add(new Message
                {
                    SenderId = staffId,
                    ReceiverId = customerId,
                    MessageText = messageText.Trim(),
                    SentAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("SupportChat", new { customerId = customerId });
        }

        // ================= 5. หน้าโปรไฟล์ของทีมงาน (Admin / Staff / Accountant) =================
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(currentUserId);
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string username)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out int currentUserId);
            var user = await _context.Users.FindAsync(currentUserId);

            if (user != null && !string.IsNullOrWhiteSpace(username))
            {
                user.Username = username.Trim();
                await _context.SaveChangesAsync();
                TempData["Success"] = "อัปเดตชื่อผู้ใช้งานเรียบร้อยแล้ว";
            }
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out int currentUserId);
            var user = await _context.Users.FindAsync(currentUserId);

            if (user == null || string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["Error"] = "กรุณากรอกรหัสผ่านให้ครบถ้วน";
                return RedirectToAction("Profile");
            }

            var hasher = new PasswordHasher<User>();
            var verifyResult = hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
            if (verifyResult == PasswordVerificationResult.Failed && user.PasswordHash != currentPassword)
            {
                TempData["Error"] = "รหัสผ่านเดิมไม่ถูกต้อง";
                return RedirectToAction("Profile");
            }

            user.PasswordHash = hasher.HashPassword(user, newPassword);
            await _context.SaveChangesAsync();

            TempData["Success"] = "เปลี่ยนรหัสผ่านใหม่เรียบร้อยแล้ว";
            return RedirectToAction("Profile");
        }
    }
}