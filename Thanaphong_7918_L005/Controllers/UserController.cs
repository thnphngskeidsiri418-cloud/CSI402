using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Thanaphong_7918_L005.Models;

namespace Thanaphong_7918_L005.Controllers
{
    public class UserController : Controller
    {
        private readonly Csi402dbContext _db;

        public UserController(Csi402dbContext db)
        {
            _db = db;
        }

        // ================= 1. หน้าเลือกแพ็กเกจเติมเกม (รับ gameId จากหน้าแรก) =================
        [HttpGet]
        public async Task<IActionResult> TopUp(int gameId)
        {
            var game = await _db.Games
                .Include(g => g.Packages)
                .FirstOrDefaultAsync(g => g.GameId == gameId);

            if (game == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ActivePromotions = await _db.Promotions
                .Where(p => p.IsActive == true)
                .ToListAsync();

            return View(game);
        }

        // ================= 2. สร้างคำสั่งซื้อ + อัปโหลดสลิปลง MySQL =================
        [HttpPost]
        [Authorize] // บังคับต้องล็อกอินก่อนสั่งซื้อ
        public async Task<IActionResult> CreateOrder(int packageId, string gameUid, string? gameServer, string? promoCode, IFormFile? slipFile)
        {
            if (string.IsNullOrWhiteSpace(gameUid))
            {
                TempData["Error"] = "กรุณากรอก User ID ในเกม";
                return RedirectToAction("Index", "Home");
            }

            var pkg = await _db.Packages.Include(p => p.Game).FirstOrDefaultAsync(p => p.PackageId == packageId);
            if (pkg == null) return RedirectToAction("Index", "Home");

            // ดึง UserId จาก Cookie ที่ล็อกอินอยู่
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            // คำนวณราคา (เช็กโค้ดส่วนลดถ้าลูกค้ากรอกมา)
            decimal finalPrice = pkg.Price;
            if (!string.IsNullOrWhiteSpace(promoCode))
            {
                string cleanCode = promoCode.Trim().ToUpper();
                var promo = await _db.Promotions
                    .FirstOrDefaultAsync(p => p.PromoCode != null && p.PromoCode.ToUpper() == cleanCode && p.IsActive == true);
                if (promo != null)
                {
                    finalPrice = Math.Max(0, finalPrice - promo.DiscountAmount);
                }
            }

            // บันทึกรูปสลิปโอนเงินเข้า wwwroot/images/slips/
            string? slipPath = null;
            if (slipFile != null && slipFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "slips");
                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(slipFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await slipFile.CopyToAsync(stream);
                }
                slipPath = "/images/slips/" + uniqueFileName;
            }

            var newOrder = new Order
            {
                UserId = currentUserId,
                PackageId = packageId,
                GameUid = gameUid,
                GameServer = gameServer,
                TotalPrice = finalPrice,
                SlipImage = slipPath,
                Status = "PendingPayment", // รอฝ่ายบัญชีตรวจสอบสลิป
                CreatedAt = DateTime.Now
            };

            _db.Orders.Add(newOrder);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"สร้างคำสั่งซื้อ #{newOrder.OrderId} สำเร็จ! กรุณารอฝ่ายบัญชีตรวจสอบสลิปสักครู่";
            return RedirectToAction("History");
        }

        // ================= 3. หน้าประวัติการทำรายการของลูกค้า =================
        [Authorize]
        public async Task<IActionResult> History()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out int currentUserId);

            var orders = await _db.Orders
                .Include(o => o.Package)
                .ThenInclude(p => p.Game)
                .Where(o => o.UserId == currentUserId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // ================= 4. หน้าแต้มสะสม & แลกของรางวัล =================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Rewards()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out int currentUserId);

            var user = await _db.Users.FindAsync(currentUserId);
            return View(user);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RedeemReward(int pointsRequired, string rewardName)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out int currentUserId);

            var user = await _db.Users.FindAsync(currentUserId);
            if (user == null || user.Points < pointsRequired)
            {
                TempData["Error"] = "แต้มสะสมของคุณไม่เพียงพอสำหรับแลกรางวัลนี้";
                return RedirectToAction("Rewards");
            }

            user.Points -= pointsRequired;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"แลกรางวัล '{rewardName}' สำเร็จ! หักแต้มสะสม {pointsRequired} แต้มเรียบร้อยแล้ว";
            return RedirectToAction("Rewards");
        }

        // ================= 5. ลูกค้าส่งคำร้องขอคืนเงิน =================
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RequestRefund(int orderId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "กรุณาระบุเหตุผลการขอคืนเงิน";
                return RedirectToAction("History");
            }

            if (await _db.Refunds.AnyAsync(r => r.OrderId == orderId))
            {
                TempData["Error"] = "ออเดอร์นี้ได้ยื่นคำร้องขอคืนเงินไปแล้ว";
                return RedirectToAction("History");
            }

            _db.Refunds.Add(new Refund
            {
                OrderId = orderId,
                Reason = reason,
                Status = "Pending",
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = $"ส่งคำร้องขอคืนเงินสำหรับออเดอร์ #ORD-{orderId:D4} ให้ฝ่ายบัญชีตรวจสอบแล้ว";
            return RedirectToAction("History");
        }

        // ================= 6. หน้าตั้งค่าบัญชีส่วนตัว =================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out int currentUserId);
            var user = await _db.Users.FindAsync(currentUserId);
            return View(user);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string username)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out int currentUserId);
            var user = await _db.Users.FindAsync(currentUserId);

            if (user != null && !string.IsNullOrWhiteSpace(username))
            {
                user.Username = username.Trim();
                await _db.SaveChangesAsync();
                TempData["Success"] = "อัปเดตชื่อผู้ใช้งานเรียบร้อยแล้ว";
            }
            return RedirectToAction("Settings");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out int currentUserId);
            var user = await _db.Users.FindAsync(currentUserId);

            if (user == null || string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["Error"] = "กรุณากรอกรหัสผ่านให้ครบถ้วน";
                return RedirectToAction("Settings");
            }

            var hasher = new PasswordHasher<User>();
            var verifyResult = hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
            if (verifyResult == PasswordVerificationResult.Failed && user.PasswordHash != currentPassword)
            {
                TempData["Error"] = "รหัสผ่านเดิมไม่ถูกต้อง";
                return RedirectToAction("Settings");
            }

            user.PasswordHash = hasher.HashPassword(user, newPassword);
            await _db.SaveChangesAsync();

            TempData["Success"] = "เปลี่ยนรหัสผ่านใหม่เรียบร้อยแล้ว";
            return RedirectToAction("Settings");
        }

        // ================= 7. หน้าแชทติดต่อแอดมิน / พนักงาน =================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SupportChat()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out int currentUserId);

            var messages = await _db.Messages
                .Include(m => m.Sender)
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            return View(messages);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SendMessage(string messageText)
        {
            if (!string.IsNullOrWhiteSpace(messageText))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int.TryParse(userIdClaim, out int currentUserId);

                _db.Messages.Add(new Message
                {
                    SenderId = currentUserId,
                    ReceiverId = null, // ส่งถึงทีมงาน
                    MessageText = messageText.Trim(),
                    SentAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }
            return RedirectToAction("SupportChat");
        }
    }
}