using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Data;
using STOREBOOKS.Models;

namespace STOREBOOKS.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================== ADMIN: Xem tất cả đơn hàng ==================
        [Route("admin/order")]
        [Route("admin/order/index")]
        public async Task<IActionResult> Index()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            if (vaiTro != "Admin")
                return RedirectToAction("Login", "Account");

            var orders = await _context.Orders
                .Include(o => o.NguoiDung)
                .OrderByDescending(o => o.NgayDat)
                .ToListAsync();

            return View(orders);
        }

        // ================== ADMIN: Xem chi tiết đơn hàng ==================
        [Route("admin/order/details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            if (vaiTro != "Admin")
                return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Sach)
                .Include(o => o.NguoiDung)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // ================== ADMIN: Cập nhật trạng thái đơn hàng ==================
        [HttpGet]
        [Route("admin/order/capnhattrangthai/{id?}")]
        public async Task<IActionResult> CapNhatTrangThai(int? id)
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            if (vaiTro != "Admin")
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            return View(order);
        }

        [HttpPost]
        [Route("admin/order/capnhattrangthai")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatTrangThai(int id, string trangThai)
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            if (vaiTro != "Admin")
                return RedirectToAction("Login", "Account");

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            order.TrangThai = trangThai;
            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Cập nhật trạng thái đơn hàng thành công.";
            return RedirectToAction(nameof(Index));
        }

        // ================== USER: Xem lịch sử mua hàng ==================
        [Route("Order/LichSu")]
        public async Task<IActionResult> LichSu()
        {
            var maNguoiDung = HttpContext.Session.GetInt32("MaNguoiDung");
            if (maNguoiDung == null)
                return RedirectToAction("Login", "Account");

            var donHangs = await _context.Orders
                .Where(o => o.MaNguoiDung == maNguoiDung)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Sach)
                .OrderByDescending(o => o.NgayDat)
                .ToListAsync();

            return View(donHangs);
        }

        // ================== USER: Xem chi tiết đơn hàng đã mua ==================
        [Route("Order/ChiTietDonMua/{id}")]
        public async Task<IActionResult> ChiTietDonMua(int id)
        {
            var maNguoiDung = HttpContext.Session.GetInt32("MaNguoiDung");
            if (maNguoiDung == null)
                return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Sach)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.MaNguoiDung == maNguoiDung);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // ================== USER: Hủy đơn hàng ==================
        [HttpPost]
        [Route("Order/HuyDonHang")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyDonHang(int id)
        {
            var maNguoiDung = HttpContext.Session.GetInt32("MaNguoiDung");
            if (maNguoiDung == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập" });

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id && o.MaNguoiDung == maNguoiDung);

            if (order == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

            // Chỉ cho phép hủy nếu đơn hàng đang ở trạng thái "Chờ xác nhận"
            if (order.TrangThai != "Chờ xác nhận")
            {
                return Json(new { success = false, message = "Không thể hủy đơn hàng đã được xác nhận" });
            }

            order.TrangThai = "Đã hủy";
            _context.Update(order);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã hủy đơn hàng thành công" });
        }
    }
}
