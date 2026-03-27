using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Data;
using STOREBOOKS.Models;
using Serilog;

namespace STOREBOOKS.Controllers
{
    [Route("admin/danhmuc")]
    public class DanhMucController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DanhMucController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DanhMuc
        [HttpGet("")]
        [HttpGet("index")]
        public IActionResult Index()
        {
            var danhMucList = _context.DanhMuc.ToList();
            return View(danhMucList);
        }

        // GET: DanhMuc/Create
        [HttpGet("create")]
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("VaiTro") != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            return View();
        }

        // POST: DanhMuc/Create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("TenDanhMuc")] DanhMuc danhMuc)
        {
            if (HttpContext.Session.GetString("VaiTro") != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(danhMuc);
                    _context.SaveChanges();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Lỗi khi thêm danh mục: {Message}", ex.Message);
                    ModelState.AddModelError("", "Có lỗi xảy ra khi thêm danh mục: " + ex.Message);
                }
            }
            else
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Log.Error("Validation error in Create DanhMuc: {ErrorMessage}", error.ErrorMessage);
                }
            }

            return View(danhMuc);
        }

        // GET: DanhMuc/Edit/5
        [HttpGet("edit/{id?}")]
        public IActionResult Edit(int? id)
        {
            if (HttpContext.Session.GetString("VaiTro") != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (id == null) return NotFound();

            var danhMuc = _context.DanhMuc.Find(id);
            if (danhMuc == null) return NotFound();

            return View(danhMuc);
        }

        // POST: DanhMuc/Edit/5
        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, [Bind("MaDanhMuc,TenDanhMuc")] DanhMuc danhMuc)
        {
            if (HttpContext.Session.GetString("VaiTro") != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (id != danhMuc.MaDanhMuc) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(danhMuc);
                    _context.SaveChanges();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.DanhMuc.Any(e => e.MaDanhMuc == danhMuc.MaDanhMuc))
                    {
                        return NotFound();
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Lỗi khi chỉnh sửa danh mục: {Message}", ex.Message);
                    ModelState.AddModelError("", "Có lỗi xảy ra khi chỉnh sửa danh mục: " + ex.Message);
                }
            }

            return View(danhMuc);
        }

        // GET: DanhMuc/Delete/5
        [HttpGet("delete/{id?}")]
        public IActionResult Delete(int? id)
        {
            if (HttpContext.Session.GetString("VaiTro") != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (id == null) return NotFound();

            var danhMuc = _context.DanhMuc.FirstOrDefault(m => m.MaDanhMuc == id);
            if (danhMuc == null) return NotFound();

            return View(danhMuc);
        }

        // POST: DanhMuc/Delete/5
        [HttpPost("delete/{id}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (HttpContext.Session.GetString("VaiTro") != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var danhMuc = _context.DanhMuc.Find(id);
            if (danhMuc != null)
            {
                _context.DanhMuc.Remove(danhMuc);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}