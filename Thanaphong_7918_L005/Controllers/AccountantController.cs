using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Thanaphong_7918_L005.Models;

namespace Thanaphong_7918_L005.Controllers
{
    [Authorize(Roles = "Accountant,Admin")]
    public class AccountantController : Controller
    {
        private readonly Csi402dbContext _context;

        public AccountantController(Csi402dbContext context)
        {
            _context = context;
        }

        // 1. หน้าหลักฝ่ายบัญชี: ดึงรายการออเดอร์ที่รอตรวจสอบสลิป และประวัติการตรวจล่าสุด
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Package)
                .ThenInclude(p => p.Game)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // 2. กดอนุมัติสลิป -> เปลี่ยนสถานะเป็น Paid เพื่อส่งต่อให้พนักงานเติมเกม
        [HttpPost]
        public async Task<IActionResult> ApprovePayment(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null && order.Status == "PendingPayment")
            {
                order.Status = "Paid";
                await _context.SaveChangesAsync();
                TempData["Success"] = $"อนุมัติสลิปออเดอร์ #ORD-{orderId:D4} เรียบร้อยแล้ว! ส่งคิวไปยังฝ่ายเติมเกมทันที";
            }
            return RedirectToAction("Index");
        }

        // 3. กดปฏิเสธสลิป -> เปลี่ยนสถานะเป็น Rejected
        [HttpPost]
        public async Task<IActionResult> RejectPayment(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null && order.Status == "PendingPayment")
            {
                order.Status = "Rejected";
                await _context.SaveChangesAsync();
                TempData["Error"] = $"ปฏิเสธสลิปออเดอร์ #ORD-{orderId:D4} เรียบร้อยแล้ว";
            }
            return RedirectToAction("Index");
        }

        // 4. หน้าตรวจสอบคำร้องขอคืนเงิน
        [HttpGet]
        public async Task<IActionResult> Refunds()
        {
            var refunds = await _context.Refunds
                .Include(r => r.Order)
                .ThenInclude(o => o.User)
                .Include(r => r.Order)
                .ThenInclude(o => o.Package)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(refunds);
        }

        // 5. ฝ่ายบัญชีอนุมัติ / ปฏิเสธ คำร้องขอคืนเงิน
        [HttpPost]
        public async Task<IActionResult> ProcessRefund(int refundId, string decision)
        {
            var refund = await _context.Refunds.Include(r => r.Order).FirstOrDefaultAsync(r => r.RefundId == refundId);
            if (refund != null && refund.Status == "Pending")
            {
                refund.Status = decision; // Approved หรือ Rejected
                if (decision == "Approved" && refund.Order != null)
                {
                    refund.Order.Status = "Refunded";
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = $"อัปเดตสถานะคำร้องขอคืนเงิน #{refundId} เรียบร้อยแล้ว";
            }
            return RedirectToAction("Refunds");
        }
    }
}