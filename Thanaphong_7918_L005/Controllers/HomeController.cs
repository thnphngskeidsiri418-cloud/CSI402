using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Thanaphong_7918_L005.Models;

namespace Thanaphong_7918_L005.Controllers;

public class HomeController : Controller
{
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
        string Fullname, room, year, language; ;
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
