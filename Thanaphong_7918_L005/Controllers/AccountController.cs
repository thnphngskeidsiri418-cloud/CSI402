using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Thanaphong_7918_L005.Models;

namespace Thanaphong_7918_L005.Controllers
{
    public class AccountController : Controller
    {
        private readonly Csi402dbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public AccountController(Csi402dbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        // ================= 1. สมัครสมาชิก (REGISTER) =================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string username, string email, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "กรุณากรอกข้อมูลให้ครบทุกช่อง";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "รหัสผ่านและการยืนยันรหัสผ่านไม่ตรงกัน";
                return View();
            }

            bool isDuplicate = await _context.Users.AnyAsync(u => u.Email == email || u.Username == username);
            if (isDuplicate)
            {
                ViewBag.Error = "ชื่อผู้ใช้หรืออีเมลนี้ถูกใช้งานแล้ว";
                return View();
            }

            var newUser = new User
            {
                Username = username,
                Email = email,
                Role = "User"
            };
            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, password);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["Success"] = "สมัครสมาชิกสำเร็จ! กรุณาเข้าสู่ระบบ";
            return RedirectToAction("Login", "Account");
        }

        // ================= 2. เข้าสู่ระบบ (LOGIN) =================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "กรุณากรอกอีเมลและรหัสผ่าน";
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email || u.Username == email);
            if (user == null)
            {
                ViewBag.Error = "ไม่พบบัญชีผู้ใช้นี้ในระบบ";
                return View();
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
            {
                ViewBag.Error = "รหัสผ่านไม่ถูกต้อง";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return user.Role switch
            {
                "Admin" => RedirectToAction("Index", "Admin"),
                "Staff" => RedirectToAction("Index", "Staff"),
                "Accountant" => RedirectToAction("Index", "Accountant"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        // ================= 3. ออกจากระบบ (LOGOUT) =================
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}