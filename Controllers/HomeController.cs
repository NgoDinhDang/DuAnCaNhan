using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Data;
using STOREBOOKS.Models;

namespace STOREBOOKS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get featured books (books with discount)
            var sachGiamGia = await _context.Sach
                .Where(s => s.GiamGia > 0)
                .OrderByDescending(s => s.GiamGia)
                .Take(8)
                .ToListAsync();

            // Get best-selling books (you might want to add a sales tracking field later)
            var sachNoiBat = await _context.Sach
                .OrderByDescending(s => s.Gia) // Temporary: sort by price, you can improve this
                .Take(8)
                .ToListAsync();

            // Get categories for navigation
            var danhMucs = await _context.DanhMuc.ToListAsync();

            ViewBag.SachGiamGia = sachGiamGia;
            ViewBag.SachNoiBat = sachNoiBat;
            ViewBag.DanhMuc = danhMucs;

            // Check if user is logged in
            var tenDangNhap = HttpContext.Session.GetString("TenDangNhap");
            if (!string.IsNullOrEmpty(tenDangNhap))
            {
                ViewBag.IsLoggedIn = true;
                var vaiTro = HttpContext.Session.GetString("VaiTro");
                ViewBag.IsAdmin = vaiTro == "Admin";

                // Get user's favorites if logged in
                var user = await _context.NguoiDung
                    .Include(u => u.TaiKhoan)
                    .FirstOrDefaultAsync(u => u.TaiKhoan.TenDangNhap == tenDangNhap);
                if (user != null)
                {
                    var yeuThichList = await _context.YeuThich
                        .Where(y => y.MaNguoiDung == user.MaNguoiDung)
                        .Select(y => y.MaSach)
                        .ToListAsync();
                    ViewBag.ListYeuThich = yeuThichList;
                }
                else
                {
                    ViewBag.ListYeuThich = new List<int>();
                }
            }
            else
            {
                ViewBag.IsLoggedIn = false;
                ViewBag.ListYeuThich = new List<int>();
            }

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}