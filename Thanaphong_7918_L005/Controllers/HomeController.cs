using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Thanaphong_7918_L005.Models;
using System.Linq;

namespace Thanaphong_7918_L005.Controllers;

public class HomeController : Controller
{
    private readonly Csi402dbContext _db;

    public HomeController(Csi402dbContext db)
    {
        _db = db;
    }

    // ==========================================
    // ส่วนงานเดิมของคุณ (Lab9, Privacy, Lad2, ASSESSMENT3)
    // ==========================================
    public IActionResult Lab9()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Lab9(User data)
    {
        if (data.PasswordHash != null)
        {
            if (ModelState.IsValid)
            {
                var CheckUser = (from a in _db.Users where a.Email == data.Email select a).FirstOrDefault();
                if (CheckUser == null)
                {
                    User s = new();
                    s.Username = data.Username;
                    s.PasswordHash = data.PasswordHash;
                    s.Email = data.Email;
                    s.Role = data.Role;

                    _db.Add(s);
                    _db.SaveChanges();
                    return View(data);
                }
                else
                {
                    ViewBag.Status = "Email ซ้ำ";
                    return View(data);
                }
            }
        }
        else
        {
            ViewBag.Status = "Password Null";
            return View(data);
        }

        return View(data);
    }

    public IActionResult Lab9_1()
    {
        var users = (from s in _db.Users select s).ToList();
        return View(users);
    }

    // ==========================================
    // หน้าแรกของลูกค้า: ดึงเกมที่เปิดใช้งาน + ค้นหาเกม + แบนเนอร์โปรโมชั่นจาก MySQL มาแสดง
    // ==========================================
    public async Task<IActionResult> Index(string? search)
    {
        var query = _db.Games.Where(g => g.IsActive == true);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(g => g.GameName.Contains(search.Trim()));
            ViewBag.SearchTerm = search.Trim();
        }

        var activeGames = await query.ToListAsync();

        var activePromotions = await _db.Promotions
            .Where(p => p.IsActive == true)
            .OrderByDescending(p => p.PromoId)
            .ToListAsync();

        ViewBag.Promotions = activePromotions;
        ViewBag.Banner = activePromotions.FirstOrDefault(p => p.BannerImage != null) ?? activePromotions.FirstOrDefault();

        return View(activeGames);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Lad2()
    {
        char A = 'a';
        string Name, LastName;
        Name = "Thanaphong";
        LastName = "Koedsiri";
        ViewBag.Name = Name;
        int x, y, sum;
        x = 2;
        y = 1;
        ViewBag.x = x;
        ViewBag.y = y;
        sum = x + y;
        ViewBag.Sum = sum;
        if (sum > 10)
        {
            ViewBag.Result = "True";
        }
        else if (sum < 10)
        {
            ViewBag.Result = "True2";
        }
        else
        {
            ViewBag.Result = "False";
        }
        return View();
    }

    public IActionResult ASSESSMENT3()
    {
        string Fullname, room, year, language;
        Fullname = "Thanaphong Koedsiri";
        room = "L005";
        year = "ปี3";
        language = "CSS";
        ViewBag.Fullname = Fullname;
        ViewBag.room = room;
        ViewBag.year = year;
        ViewBag.language = language;
        int N1, N2, N3, N4, N5, N6, N7, N8, N9, N10, Sum;
        N1 = 10; N2 = 10; N3 = 10; N4 = 10; N5 = 10;
        N6 = 10; N7 = 2; N8 = 0; N9 = 0; N10 = 0;
        Sum = N1 + N2 + N3 + N4 + N5 + N6 + N7 + N8 + N9 + N10;
        ViewBag.Sum = Sum;
        if (Sum >= 80) ViewBag.Result = "A";
        else if (Sum >= 76) ViewBag.Result = "B+";
        else if (Sum >= 71) ViewBag.Result = "B";
        else if (Sum >= 66) ViewBag.Result = "C+";
        else if (Sum >= 61) ViewBag.Result = "C";
        else if (Sum >= 56) ViewBag.Result = "D+";
        else if (Sum >= 50) ViewBag.Result = "D";
        else if (Sum < 50) ViewBag.Result = "F";

        return View();
    }

    // ==========================================
    // ส่วนงานเดิมของคุณ: UserList, Edit, Delete
    // ==========================================
    public IActionResult UserList()
    {
        var users = (from s in _db.Users select s).ToList();
        return View(users);
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var user = (from u in _db.Users where u.UserId == id select u).FirstOrDefault();
        if (user == null)
        {
            return RedirectToAction("UserList");
        }
        return View(user);
    }

    [HttpPost]
    public IActionResult Edit(User data)
    {
        if (ModelState.IsValid)
        {
            _db.Users.Update(data);
            _db.SaveChanges();
            return RedirectToAction("UserList");
        }
        return View(data);
    }

    public IActionResult Delete(int id)
    {
        var user = (from u in _db.Users where u.UserId == id select u).FirstOrDefault();
        if (user != null)
        {
            _db.Users.Remove(user);
            _db.SaveChanges();
        }
        return RedirectToAction("UserList");
    }

    // ==========================================
    // System Error
    // ==========================================
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}