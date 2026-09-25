using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Thanaphong_7918_L005.Models;

namespace Thanaphong_7918_L005.Controllers;

public class HomeController : Controller
{
    // สร้าง List เก็บข้อมูลผู้ใช้ชั่วคราว (จำลองฐานข้อมูลสำหรับหน้า UserList)
    private static List<UserViewModel> _userList = new List<UserViewModel>()
    {
        new UserViewModel { Id = 1, Email = "admin@topup.com", Username = "AdminMaster", Role = "Admin" },
        new UserViewModel { Id = 2, Email = "gamer01@gmail.com", Username = "ProGamer", Role = "User" }
    };

    public IActionResult Index()
    {
        return View();
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
        int x,y,sum;
        x = 2;
        y = 1;
        ViewBag.x = x;
        ViewBag.y = y;
        sum = x + y;
        ViewBag.Sum = sum;
        if(sum > 10)
        {
            ViewBag.Result = "True";
        }
        else if(sum < 10)
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
        N1 = 10;
        N2 = 10;
        N3 = 10;
        N4 = 10;
        N5 = 10;
        N6 = 10;
        N7 = 2;
        N8 = 0;
        N9 = 0;
        N10 = 0;
        Sum = N1 + N2 + N3 + N4 + N5 + N6 + N7 + N8 + N9 + N10;
        ViewBag.Sum = Sum;
        if(Sum >= 80)
        {
            ViewBag.Result = "A";
        }
        else if(Sum >= 76 )
        {
            ViewBag.Result = "B+";
        }
        else if(Sum >= 71 )
        {
            ViewBag.Result = "B";
        }
        else if(Sum >= 66 )
        {
            ViewBag.Result = "C+";
        }
        else if(Sum >= 61 )
        {
            ViewBag.Result = "C";
        }
        else if(Sum >= 56 )
        {
            ViewBag.Result = "D+";
        }
        else if(Sum >= 50 )
        {
            ViewBag.Result = "D";
        }
        else if(Sum < 50)
        {
            ViewBag.Result = "F";
        }

        return View();
    }


    // ==========================================
    // ส่วนของ Register
    // ==========================================
    [HttpGet]
    public IActionResult Register()
    {
        
        return View();
    }

    [HttpPost]
    public IActionResult Register(RegisterViewModel model)
    {
        return RedirectToAction("Index");
    }

    // ==========================================
    // ส่วนของ Login
    // ==========================================
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(LoginViewModel model)
    {
        return RedirectToAction("Index");
    }

    // ==========================================
    // ส่วนของ UserList และ Add User (เพิ่มใหม่)
    // ==========================================
    public IActionResult UserList()
    {
        return View(_userList);
    }

    [HttpGet]
    public IActionResult AddUser()
    {
        return View();
    }

    [HttpPost]
    public IActionResult AddUser(UserViewModel model)
    {
        model.Id = _userList.Count + 1;
        _userList.Add(model);
        return RedirectToAction("UserList");
    }
    // ==========================================

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}