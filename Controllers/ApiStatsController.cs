using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Data;

namespace STOREBOOKS.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
[Authorize(Roles = "Admin")] // Chỉ Admin mới được xem thống kê
public class ApiStatsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ApiStatsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/ApiStats/Dashboard
    [HttpGet]
    public IActionResult Dashboard()
    {
        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        var totalOrders = _context.Orders.Count();
        
        // ✅ Tính doanh thu từ OrderDetails để đảm bảo chính xác
        var completedOrders = _context.Orders
            .Include(o => o.OrderDetails)
            .Where(o => o.TrangThai == "Hoàn tất" || o.TrangThai == "Đã giao" || o.TrangThai == "Đã thanh toán")
            .ToList();
        
        var totalRevenue = completedOrders.Sum(o => o.TongTienThucTe);

        var thisMonthOrders = _context.Orders
            .Include(o => o.OrderDetails)
            .Where(o => o.NgayDat >= startOfMonth &&
                        (o.TrangThai == "Hoàn tất" || o.TrangThai == "Đã giao" || o.TrangThai == "Đã thanh toán"))
            .ToList();
        
        var thisMonthRevenue = thisMonthOrders.Sum(o => o.TongTienThucTe);

        var totalUsers = _context.NguoiDung.Count();
        var totalBooks = _context.Sach.Count();

        return Ok(new
        {
            success = true,
            data = new
            {
                totalOrders,
                totalRevenue,
                thisMonthRevenue,
                totalUsers,
                totalBooks
            }
        });
    }

    // GET: api/ApiStats/RevenueByDate
    [HttpGet]
    public IActionResult RevenueByDate(DateTime? fromDate = null, DateTime? toDate = null)
    {
        fromDate ??= DateTime.Today.AddDays(-30);
        toDate ??= DateTime.Today;

        // ✅ Include OrderDetails để tính doanh thu chính xác
        var orders = _context.Orders
            .Include(o => o.OrderDetails)
            .Where(o => o.NgayDat.Date >= fromDate.Value.Date &&
                        o.NgayDat.Date <= toDate.Value.Date &&
                        (o.TrangThai == "Hoàn tất" || o.TrangThai == "Đã giao" || o.TrangThai == "Đã thanh toán"))
            .ToList();

        var data = orders
            .GroupBy(o => o.NgayDat.Date)
            .Select(g => new
            {
                Date = g.Key,
                Total = g.Sum(x => x.TongTienThucTe)
            })
            .OrderBy(x => x.Date)
            .ToList();

        return Ok(new { success = true, data });
    }

    // GET: api/ApiStats/TopSellingBooks
    [HttpGet]
    public IActionResult TopSellingBooks(int limit = 10)
    {
        var data = _context.OrderDetails
            .GroupBy(od => od.MaSach)
            .Select(g => new
            {
                MaSach = g.Key,
                SoLuongBan = g.Sum(x => x.SoLuong)
            })
            .OrderByDescending(x => x.SoLuongBan)
            .Take(limit)
            .Join(_context.Sach,
                g => g.MaSach,
                s => s.MaSach,
                (g, s) => new
                {
                    s.MaSach,
                    s.TenSach,
                    s.TacGia,
                    s.HinhAnh,
                    g.SoLuongBan
                })
            .ToList();

        return Ok(new { success = true, data });
    }

    /// <summary>
    /// Kiểm tra tính toàn vẹn dữ liệu - So sánh TongTien với tổng OrderDetails
    /// </summary>
    /// <returns>Danh sách đơn hàng có sai lệch dữ liệu</returns>
    [HttpGet]
    public IActionResult ValidateOrderIntegrity()
    {
        var orders = _context.Orders
            .Include(o => o.OrderDetails)
            .ToList();

        var invalidOrders = orders
            .Where(o => !o.IsTongTienValid)
            .Select(o => new
            {
                o.OrderId,
                o.TenKhachHang,
                o.NgayDat,
                o.TrangThai,
                TongTienLuuTru = o.TongTien,
                TongTienThucTe = o.TongTienThucTe,
                ChenhLech = o.TongTien - o.TongTienThucTe,
                SoLuongSanPham = o.OrderDetails.Count
            })
            .ToList();

        return Ok(new
        {
            success = true,
            totalOrders = orders.Count,
            invalidOrders = invalidOrders.Count,
            data = invalidOrders,
            message = invalidOrders.Any() 
                ? $"Tìm thấy {invalidOrders.Count} đơn hàng có dữ liệu không khớp" 
                : "Tất cả đơn hàng đều có dữ liệu chính xác"
        });
    }

    /// <summary>
    /// Sửa chữa dữ liệu - Cập nhật TongTien từ OrderDetails cho tất cả đơn hàng
    /// </summary>
    /// <returns>Kết quả sửa chữa</returns>
    [HttpPost]
    public IActionResult FixOrderIntegrity()
    {
        var orders = _context.Orders
            .Include(o => o.OrderDetails)
            .ToList();

        var fixedCount = 0;
        foreach (var order in orders)
        {
            if (!order.IsTongTienValid)
            {
                order.RecalculateTongTien();
                fixedCount++;
            }
        }

        _context.SaveChanges();

        return Ok(new
        {
            success = true,
            message = $"Đã sửa chữa {fixedCount} đơn hàng",
            totalOrders = orders.Count,
            fixedOrders = fixedCount
        });
    }
}


