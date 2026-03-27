using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Data;
using STOREBOOKS.Models;
using System.Linq;
using System.Threading.Tasks;

namespace STOREBOOKS.Controllers
{
    public class YeuThichController : Controller
    {
        private readonly ApplicationDbContext _context;

        public YeuThichController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách yêu thích
        public async Task<IActionResult> Index()
        {
            var tenDangNhap = HttpContext.Session.GetString("TenDangNhap");
            if (string.IsNullOrEmpty(tenDangNhap))
                return RedirectToAction("DangNhap", "TaiKhoan");

            var taiKhoan = await _context.TaiKhoan
                .Include(tk => tk.NguoiDung)
                .FirstOrDefaultAsync(x => x.TenDangNhap == tenDangNhap);
            if (taiKhoan?.NguoiDung == null) return RedirectToAction("Login", "Account");
            var nguoiDung = taiKhoan.NguoiDung;

            var danhSach = await _context.YeuThich
                .Where(y => y.MaNguoiDung == nguoiDung.MaNguoiDung)
                .Include(y => y.Sach)
                .ToListAsync();

            return View(danhSach);
        }

        // Thêm sách vào yêu thích
        [HttpPost]
        public async Task<IActionResult> Them(int maSach)
        {
            var tenDangNhap = HttpContext.Session.GetString("TenDangNhap");
            if (string.IsNullOrEmpty(tenDangNhap))
                return Json(new { success = false, message = "Bạn cần đăng nhập để sử dụng chức năng này." });

            var taiKhoan = await _context.TaiKhoan
                .Include(tk => tk.NguoiDung)
                .FirstOrDefaultAsync(x => x.TenDangNhap == tenDangNhap);
            if (taiKhoan?.NguoiDung == null) return Json(new { success = false, message = "Không tìm thấy người dùng." });
            var nguoiDung = taiKhoan.NguoiDung;

            var daTonTai = await _context.YeuThich
                .AnyAsync(y => y.MaNguoiDung == nguoiDung.MaNguoiDung && y.MaSach == maSach);

            if (daTonTai)
            {
                return Json(new { success = false, message = "Sách đã có trong danh sách yêu thích." });
            }

            _context.YeuThich.Add(new YeuThich
            {
                MaSach = maSach,
                MaNguoiDung = nguoiDung.MaNguoiDung
            });

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã thêm vào yêu thích!" });
        }

        // Xóa sách khỏi yêu thích
        [HttpPost]
        public async Task<IActionResult> Xoa([FromBody] int id)
        {
            var yeuThich = await _context.YeuThich.FindAsync(id);
            if (yeuThich != null)
            {
                _context.YeuThich.Remove(yeuThich);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}
