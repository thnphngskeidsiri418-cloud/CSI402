using Microsoft.AspNetCore.Authentication.Cookies; // เพิ่มบรรทัดนี้
using Microsoft.EntityFrameworkCore;
using Thanaphong_7918_L005.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// โค้ดเชื่อมต่อ MySQL เดิมของคุณ (คงไว้เหมือนเดิมทุกประการ)
builder.Services.AddDbContext<Csi402dbContext>(
    options => options.UseMySql(
    builder.Configuration.GetConnectionString("DefaultConnection"), 
    ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

// เพิ่มระบบ Cookie Authentication ต่อท้ายตรงนี้
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication(); // เพิ่มบรรทัดนี้ไว้ก่อน UseAuthorization()
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();