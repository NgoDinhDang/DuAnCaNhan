using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Data;
using STOREBOOKS.Models;
using STOREBOOKS.ViewModels;

namespace STOREBOOKS.Controllers
{
    [Route("admin/thongke")]
    public class ThongKeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ThongKeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        [HttpGet("index")]
        public async Task<IActionResult> Index(DateTime? tuNgay, DateTime? denNgay)
        {
            var model = new ThongKeDoanhThuViewModel();

            // Nếu không chọn ngày → mặc định từ đầu tháng tới hôm nay
            tuNgay ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            denNgay ??= DateTime.Now;

            model.TuNgay = tuNgay.Value;
            model.DenNgay = denNgay.Value;

            // Lấy đơn hàng đã hoàn tất hoặc đã giao kèm OrderDetails
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.NgayDat >= model.TuNgay
                         && o.NgayDat <= model.DenNgay
                         && (o.TrangThai == "Hoàn tất" || o.TrangThai == "Đã giao" || o.TrangThai == "Đã thanh toán"))
                .ToListAsync();

            // Nếu không có đơn hàng → gán doanh thu = 0 và trả về View
            if (!orders.Any())
            {
                model.TongDoanhThu = 0;
                model.DoanhThuTheoNgayList = new List<DoanhThuTheoNgay>();
                model.DoanhThuTheoThangList = new List<DoanhThuTheoThang>();
                model.DoanhThuTheoNamList = new List<DoanhThuTheoNam>();
                return View(model);
            }

            // ✅ Tổng doanh thu - tính từ OrderDetails để đảm bảo chính xác
            model.TongDoanhThu = orders.Sum(o => o.TongTienThucTe);

            // Thống kê theo ngày
            model.DoanhThuTheoNgayList = orders
                .GroupBy(o => o.NgayDat.Date)
                .Select(g => new DoanhThuTheoNgay
                {
                    Ngay = g.Key,
                    TongTien = g.Sum(x => x.TongTienThucTe)
                })
                .OrderBy(x => x.Ngay)
                .ToList();

            // Thống kê theo tháng
            model.DoanhThuTheoThangList = orders
                .GroupBy(o => new { o.NgayDat.Year, o.NgayDat.Month })
                .Select(g => new DoanhThuTheoThang
                {
                    Nam = g.Key.Year,
                    Thang = g.Key.Month,
                    TongTien = g.Sum(x => x.TongTienThucTe)
                })
                .OrderBy(x => x.Nam).ThenBy(x => x.Thang)
                .ToList();

            // Thống kê theo năm
            model.DoanhThuTheoNamList = orders
                .GroupBy(o => o.NgayDat.Year)
                .Select(g => new DoanhThuTheoNam
                {
                    Nam = g.Key,
                    TongTien = g.Sum(x => x.TongTienThucTe)
                })
                .OrderBy(x => x.Nam)
                .ToList();

            return View(model);
        }

        // Thống kê sách bán chạy và lượt xem
        [HttpGet("sachbanchay")]
        public async Task<IActionResult> SachBanChay()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            if (vaiTro != "Admin")
                return RedirectToAction("Login", "Account");

            // Lấy top sách bán chạy (dựa trên số lượng bán)
            var topBanChay = await _context.OrderDetails
                .Where(od => od.Order.TrangThai == "Hoàn tất" || 
                             od.Order.TrangThai == "Đã giao" || 
                             od.Order.TrangThai == "Đã thanh toán")
                .GroupBy(od => new { od.MaSach, od.TenSach })
                .Select(g => new
                {
                    MaSach = g.Key.MaSach,
                    TenSach = g.Key.TenSach,
                    TongSoLuongBan = g.Sum(x => x.SoLuong),
                    DoanhThu = g.Sum(x => x.SoLuong * x.Gia)
                })
                .OrderByDescending(x => x.TongSoLuongBan)
                .Take(20)
                .ToListAsync();

            // Lấy thông tin chi tiết sách
            var maSachList = topBanChay.Select(x => x.MaSach).ToList();
            var sachList = await _context.Sach
                .Include(s => s.DanhMuc)
                .Where(s => maSachList.Contains(s.MaSach))
                .ToListAsync();

            // Kết hợp dữ liệu
            var ketQua = topBanChay.Select(item =>
            {
                var sach = sachList.FirstOrDefault(s => s.MaSach == item.MaSach);
                return new
                {
                    MaSach = item.MaSach,
                    TenSach = item.TenSach,
                    TacGia = sach?.TacGia ?? "",
                    DanhMuc = sach?.DanhMuc?.TenDanhMuc ?? "",
                    HinhAnh = sach?.HinhAnh ?? "",
                    TongSoLuongBan = item.TongSoLuongBan,
                    DoanhThu = item.DoanhThu,
                    SoLuotXem = sach?.SoLuotXem ?? 0,
                    TyLeChuyenDoi = sach != null && sach.SoLuotXem > 0 
                        ? Math.Round((double)item.TongSoLuongBan / sach.SoLuotXem * 100, 2) 
                        : 0
                };
            }).ToList();

            // Top sách được xem nhiều nhất
            var topXemNhieu = await _context.Sach
                .Include(s => s.DanhMuc)
                .OrderByDescending(s => s.SoLuotXem)
                .Take(20)
                .Select(s => new
                {
                    s.MaSach,
                    s.TenSach,
                    s.TacGia,
                    DanhMuc = s.DanhMuc.TenDanhMuc,
                    s.HinhAnh,
                    s.SoLuotXem
                })
                .ToListAsync();

            ViewBag.TopBanChay = ketQua;
            ViewBag.TopXemNhieu = topXemNhieu;

            // Tổng số liệu tổng quan
            var tongSoLuotXem = await _context.Sach.SumAsync(s => s.SoLuotXem);
            var tongSoLuongBan = await _context.OrderDetails
                .Where(od => od.Order.TrangThai == "Hoàn tất" || 
                             od.Order.TrangThai == "Đã giao" || 
                             od.Order.TrangThai == "Đã thanh toán")
                .SumAsync(od => od.SoLuong);
            var tongDoanhThu = await _context.OrderDetails
                .Where(od => od.Order.TrangThai == "Hoàn tất" || 
                             od.Order.TrangThai == "Đã giao" || 
                             od.Order.TrangThai == "Đã thanh toán")
                .SumAsync(od => od.SoLuong * od.Gia);

            ViewBag.TongSoLuotXem = tongSoLuotXem;
            ViewBag.TongSoLuongBan = tongSoLuongBan;
            ViewBag.TongDoanhThu = tongDoanhThu;
            ViewBag.TyLeChuyenDoiTrungBinh = tongSoLuotXem > 0 
                ? Math.Round((double)tongSoLuongBan / tongSoLuotXem * 100, 2) 
                : 0;

            return View();
        }
    }
}
