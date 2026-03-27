using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Data;
using STOREBOOKS.Models;
using System.Linq;
using System.Threading.Tasks;

namespace STOREBOOKS.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminDonHangController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminDonHangController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/DonHang
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.NguoiDung)
                .OrderByDescending(o => o.NgayDat)
                .ToListAsync();
            return View(orders);
        }

        // GET: Admin/DonHang/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Sach)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // GET: Admin/DonHang/CapNhatTrangThai/5
        public async Task<IActionResult> CapNhatTrangThai(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            return View(order);
        }

        // POST: Admin/DonHang/CapNhatTrangThai/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatTrangThai(int id, string trangThai)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.TrangThai = trangThai;
            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Cập nhật trạng thái đơn hàng thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
