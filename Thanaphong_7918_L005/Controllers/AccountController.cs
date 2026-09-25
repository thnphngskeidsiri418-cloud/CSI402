using Microsoft.AspNetCore.Mvc;
using Thanaphong_7918_L005.Models;

namespace Thanaphong_7918_L005.Controllers
{
   public class AccountController : Controller
    {
        // ================= 1. ส่วนของ Login =================
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel data)
        {
            // รับค่าแล้ว ส่ง Email ไปแสดงที่หน้า Home/Index[cite: 2]
            return RedirectToAction("Index", "Home", new { email = data.Email });
        }

        // ================= 2. ส่วนของ Register =================
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel data)
        {
            // รับค่าแล้ว ส่ง Name และ Email ไปแสดงที่หน้า Home/Index[cite: 2]
            return RedirectToAction("Index", "Home", new { name = data.Name, email = data.Email });
        }
    }
}
