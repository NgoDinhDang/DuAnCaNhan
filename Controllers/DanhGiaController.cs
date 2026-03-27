using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using System.Linq;
using STOREBOOKS.Data;
using STOREBOOKS.Models;

public class DanhGiaController : Controller
{
    private readonly ApplicationDbContext _context;

    public DanhGiaController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Hiển thị danh sách đánh giá của sách
    public async Task<IActionResult> Index(int maSach)
    {
        var danhGias = await _context.DanhGia
            .Include(d => d.NguoiDung)
            .Where(dg => dg.MaSach == maSach && dg.DaDuyet)
            .ToListAsync();

        ViewBag.MaSach = maSach;
        return View(danhGias);
    }

    // Form đánh giá (không dùng nếu xử lý AJAX)
    [HttpGet]
    public IActionResult TaoMoi(int maSach)
    {
        ViewBag.MaSach = maSach;
        return View();
    }

    // Xử lý submit đánh giá qua AJAX
    [HttpPost]
    public IActionResult Create(int maSach, int soSao, string binhLuan)
    {
        var tenDangNhap = HttpContext.Session.GetString("TenDangNhap");
        if (string.IsNullOrEmpty(tenDangNhap))
        {
            return Json(new { success = false, message = "Bạn cần đăng nhập để đánh giá sách." });
        }

        var taiKhoan = _context.TaiKhoan
            .Include(tk => tk.NguoiDung)
            .FirstOrDefault(u => u.TenDangNhap == tenDangNhap);
        if (taiKhoan?.NguoiDung == null)
        {
            return Json(new { success = false, message = "Không tìm thấy người dùng." });
        }
        var nguoiDung = taiKhoan.NguoiDung;

        bool daDanhGia = _context.DanhGia.Any(dg => dg.MaSach == maSach && dg.MaNguoiDung == nguoiDung.MaNguoiDung);
        if (daDanhGia)
        {
            return Json(new { success = false, message = "Bạn đã đánh giá sách này rồi." });
        }

        var danhGia = new DanhGia
        {
            MaSach = maSach,
            MaNguoiDung = nguoiDung.MaNguoiDung,
            SoSao = soSao,
            BinhLuan = binhLuan,
            NgayDanhGia = DateTime.Now,
            DaDuyet = true // Hiển thị ngay, không cần chờ duyệt
        };

        _context.DanhGia.Add(danhGia);
        _context.SaveChanges();

        // Lấy thông tin người dùng để trả về
        var tenNguoiDung = taiKhoan.TenDangNhap;
        
        return Json(new
        {
            success = true,
            message = "Đánh giá của bạn đã được gửi thành công!",
            danhGia = new
            {
                tenDangNhap = tenNguoiDung,
                soSao = soSao,
                binhLuan = binhLuan,
                ngay = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
            }
        });
    }
    [HttpGet]
    public IActionResult LayDanhGia(int maSach)
    {
        var danhGias = _context.DanhGia
            .Where(dg => dg.MaSach == maSach && dg.DaDuyet)
            .Include(dg => dg.NguoiDung)
                .ThenInclude(nd => nd.TaiKhoan)
            .OrderByDescending(dg => dg.NgayDanhGia)
            .Select(dg => new
            {
                tenDangNhap = dg.NguoiDung.TaiKhoan.TenDangNhap,
                soSao = dg.SoSao,
                binhLuan = dg.BinhLuan,
                ngay = dg.NgayDanhGia.ToString("dd/MM/yyyy HH:mm")
            })
            .ToList();

        double trungBinh = danhGias.Any() ? Math.Round(danhGias.Average(d => d.soSao), 1) : 0;

        return Json(new { danhGias, trungBinh });
    }

    // ===============================================
    // === CHỨC NĂNG QUẢN LÝ ĐÁNH GIÁ CHO ADMIN =======
    // ===============================================

    // Hiển thị danh sách đánh giá chưa duyệt
    public IActionResult QuanLy()
    {
        var vaiTro = HttpContext.Session.GetString("VaiTro");
        if (vaiTro != "Admin")
        {
            return RedirectToAction("Index", "Home");
        }

        var danhGias = _context.DanhGia
            .Include(d => d.Sach)
            .Include(d => d.NguoiDung)
            .OrderByDescending(d => d.NgayDanhGia)
            .ToList();

        return View(danhGias);
    }

    // Duyệt đánh giá
    [HttpPost]
    public IActionResult Duyet(int id)
    {
        var vaiTro = HttpContext.Session.GetString("VaiTro");
        if (vaiTro != "Admin")
        {
            return Json(new { success = false, message = "Bạn không có quyền duyệt đánh giá." });
        }

        var danhGia = _context.DanhGia.Find(id);
        if (danhGia == null)
        {
            return Json(new { success = false, message = "Không tìm thấy đánh giá." });
        }

        danhGia.DaDuyet = true;
        _context.SaveChanges();

        return Json(new { success = true, message = "Đã duyệt đánh giá thành công." });
    }

    // Xóa đánh giá
    [HttpPost]
    public IActionResult Xoa(int id)
    {
        var vaiTro = HttpContext.Session.GetString("VaiTro");
        if (vaiTro != "Admin")
        {
            return Json(new { success = false, message = "Bạn không có quyền xóa đánh giá." });
        }

        var danhGia = _context.DanhGia.Find(id);
        if (danhGia == null)
        {
            return Json(new { success = false, message = "Không tìm thấy đánh giá." });
        }

        _context.DanhGia.Remove(danhGia);
        _context.SaveChanges();

        return Json(new { success = true, message = "Đã xóa đánh giá thành công." });
    }
}
