using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Data;
using STOREBOOKS.Models;

namespace STOREBOOKS.Controllers
{
    [Route("admin/nguoidung")]
    public class NguoiDungController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NguoiDungController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: NguoiDung
        [HttpGet("")]
        [HttpGet("index")]
        public async Task<IActionResult> Index(string keyword, string roleFilter, int page = 1, int pageSize = 10)
        {
            var query = _context.NguoiDung
                .Include(nd => nd.TaiKhoan)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(x => x.TaiKhoan.TenDangNhap.Contains(keyword) || x.TaiKhoan.Email.Contains(keyword));
            }

            if (!string.IsNullOrEmpty(roleFilter))
            {
                query = query.Where(x => x.TaiKhoan.VaiTro == roleFilter);
            }

            int totalItems = await query.CountAsync();
            var users = await query
                .OrderBy(u => u.TaiKhoan.TenDangNhap)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.Keyword = keyword;
            ViewBag.RoleFilter = roleFilter;

            return View(users);
        }

        // GET: NguoiDung/Details/5
        [HttpGet("details/{id?}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var nguoiDung = await _context.NguoiDung
                .Include(nd => nd.TaiKhoan)
                .FirstOrDefaultAsync(m => m.MaNguoiDung == id);
            if (nguoiDung == null) return NotFound();
            return View(nguoiDung);
        }

        // GET: NguoiDung/Create
        [HttpGet("create")]
        public IActionResult Create()
        {
            return View(new NguoiDung
            {
                TaiKhoan = new TaiKhoan()
            });
        }

        // POST: NguoiDung/Create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NguoiDung nguoiDung)
        {
            if (ModelState.IsValid)
            {
                _context.Add(nguoiDung);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(nguoiDung);
        }

        // GET: NguoiDung/Edit/5
        [HttpGet("edit/{id?}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var nguoiDung = await _context.NguoiDung
                .Include(nd => nd.TaiKhoan)
                .FirstOrDefaultAsync(nd => nd.MaNguoiDung == id);
            if (nguoiDung == null) return NotFound();
            return View(nguoiDung);
        }

        // POST: NguoiDung/Edit/5
        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NguoiDung nguoiDung)
        {
            if (id != nguoiDung.MaNguoiDung) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(nguoiDung);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.NguoiDung.Any(e => e.MaNguoiDung == id))
                        return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(nguoiDung);
        }

        // GET: NguoiDung/Delete/5
        [HttpGet("delete/{id?}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var nguoiDung = await _context.NguoiDung
                .Include(nd => nd.TaiKhoan)
                .FirstOrDefaultAsync(m => m.MaNguoiDung == id);
            if (nguoiDung == null) return NotFound();
            return View(nguoiDung);
        }

        // POST: NguoiDung/Delete/5
        [HttpPost("delete/{id}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nguoiDung = await _context.NguoiDung.FindAsync(id);
            if (nguoiDung != null)
            {
                _context.NguoiDung.Remove(nguoiDung);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ========== Chặn người dùng ==========
        [HttpPost("channguoidung/{id}")]
        public async Task<IActionResult> ChanNguoiDung(int id)
        {
            var nguoiDung = await _context.NguoiDung
                .Include(nd => nd.TaiKhoan)
                .FirstOrDefaultAsync(nd => nd.MaNguoiDung == id);
            if (nguoiDung == null || nguoiDung.TaiKhoan == null) return NotFound();

            nguoiDung.TaiKhoan.BiChan = true;
            _context.Update(nguoiDung.TaiKhoan);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ========== Phân quyền ==========
        [HttpGet("phanquyen/{id?}")]
        public async Task<IActionResult> PhanQuyen(int? id)
        {
            if (id == null) return NotFound();
            var nguoiDung = await _context.NguoiDung
                .Include(nd => nd.TaiKhoan)
                .FirstOrDefaultAsync(nd => nd.MaNguoiDung == id);
            if (nguoiDung == null) return NotFound();
            return View(nguoiDung);
        }

        [HttpPost("phanquyen")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PhanQuyen(int MaNguoiDung, string VaiTro)
        {
            var nguoiDung = await _context.NguoiDung
                .Include(nd => nd.TaiKhoan)
                .FirstOrDefaultAsync(nd => nd.MaNguoiDung == MaNguoiDung);
            if (nguoiDung == null || nguoiDung.TaiKhoan == null) return NotFound();

            nguoiDung.TaiKhoan.VaiTro = VaiTro;
            _context.Update(nguoiDung.TaiKhoan);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ========== Thống kê ==========
        [HttpGet("thongke")]
        public async Task<IActionResult> ThongKe()
        {
            var nguoiDungs = await _context.NguoiDung
                .Include(nd => nd.TaiKhoan)
                .ToListAsync();
            ViewBag.TongNguoiDung = nguoiDungs.Count;
            ViewBag.SoAdmin = nguoiDungs.Count(x => x.TaiKhoan?.VaiTro == "Admin");
            ViewBag.SoUser = nguoiDungs.Count(x => x.TaiKhoan?.VaiTro == "User");
            ViewBag.SoBiChan = nguoiDungs.Count(x => x.TaiKhoan?.VaiTro == "Blocked");

            return View();
        }
    }
}
