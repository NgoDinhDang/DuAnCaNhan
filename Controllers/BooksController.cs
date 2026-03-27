using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Data;
using Newtonsoft.Json;
using STOREBOOKS.Models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace STOREBOOKS.Controllers
{
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public BooksController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Books
        public IActionResult Index(int? maDanhMuc, string keyword)
        {
            var sachQuery = _context.Sach
                .Include(s => s.DanhMuc)
                .AsQueryable();

            if (maDanhMuc.HasValue)
                sachQuery = sachQuery.Where(s => s.MaDanhMuc == maDanhMuc);

            if (!string.IsNullOrEmpty(keyword))
                sachQuery = sachQuery.Where(s =>
                    s.TenSach.Contains(keyword) ||
                    s.TacGia.Contains(keyword)
                );

            var sachGiamGia = _context.Sach.Where(s => s.IsKhuyenMai && s.GiamGia > 0).ToList();
            var sachNoiBat = _context.Sach.Where(s => s.IsNoiBat).ToList();
            var sach = sachQuery.OrderByDescending(s => s.IsNoiBat).ToList();

            ViewBag.DanhMuc = _context.DanhMuc.ToList();
            ViewBag.SachGiamGia = sachGiamGia;
            ViewBag.SachNoiBat = sachNoiBat;

            var vaiTro = HttpContext.Session.GetString("VaiTro");
            ViewBag.IsAdmin = (vaiTro == "Admin");
            ViewBag.IsLoggedIn = !string.IsNullOrEmpty(HttpContext.Session.GetString("NguoiDungId"));

            return View(sach);
        }

        // GET: Books/Details/5
        public IActionResult Details(int? id)
        {
            if (id == null) return NotFound();

            var sach = _context.Sach
                .Include(s => s.DanhMuc)
                .FirstOrDefault(m => m.MaSach == id);
            if (sach == null) return NotFound();

            // ✅ Tăng số lượt xem
            sach.SoLuotXem++;
            _context.SaveChanges();

            // Lấy đánh giá đã duyệt
            var danhGia = _context.DanhGia
                .Include(d => d.NguoiDung)
                .Where(d => d.MaSach == id && d.DaDuyet)
                .ToList();

            ViewBag.DanhGia = danhGia;
            ViewBag.SoSaoTrungBinh = danhGia.Any() ? Math.Round(danhGia.Average(d => d.SoSao), 1) : 0;

            // Gợi ý sách: 4 sách cùng danh mục, khác sách hiện tại
            var goiYSach = _context.Sach
                .Where(s => s.MaDanhMuc == sach.MaDanhMuc && s.MaSach != sach.MaSach)
                .OrderByDescending(s => s.IsNoiBat)
                .Take(4)
                .ToList();

            ViewBag.GoiYSach = goiYSach;

            // Kiểm tra quyền đánh giá
            var email = HttpContext.Session.GetString("Email");
            if (!string.IsNullOrEmpty(email))
            {
                var taiKhoan = _context.TaiKhoan
                    .Include(tk => tk.NguoiDung)
                    .FirstOrDefault(tk => tk.Email == email);
                var nguoiDung = taiKhoan?.NguoiDung;
                if (nguoiDung != null)
                {
                    var daMua = _context.OrderDetails
                        .Include(od => od.Order)
                        .Any(od => od.MaSach == id && od.Order.Email == email);

                    var daDanhGia = _context.DanhGia
                        .Any(dg => dg.MaSach == id && dg.MaNguoiDung == nguoiDung.MaNguoiDung);

                    ViewBag.HienThiFormDanhGia = daMua && !daDanhGia;
                    ViewBag.MaNguoiDung = nguoiDung.MaNguoiDung;
                }
                else
                {
                    ViewBag.HienThiFormDanhGia = false;
                }
            }
            else
            {
                ViewBag.HienThiFormDanhGia = false;
            }

            return View(sach);
        }

        // GET: Books/Create
        public IActionResult Create()
        {
            ViewBag.DanhMuc = new SelectList(_context.DanhMuc, "MaDanhMuc", "TenDanhMuc");
            return View();
        }

        // POST: Books/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Sach sach, IFormFile HinhAnhFile)
        {
            if (ModelState.IsValid)
            {
                if (HinhAnhFile?.Length > 0)
                {
                    var fileName = Path.GetFileName(HinhAnhFile.FileName);

                    // Đổi tên file để tránh trùng, ví dụ: timestamp + tên gốc
                    var uniqueFileName = $"{System.Guid.NewGuid()}_{fileName}";

                    var filePath = Path.Combine(_environment.WebRootPath, "images", uniqueFileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await HinhAnhFile.CopyToAsync(stream);

                    sach.HinhAnh = uniqueFileName;
                }

                _context.Add(sach);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.DanhMuc = new SelectList(_context.DanhMuc, "MaDanhMuc", "TenDanhMuc", sach.MaDanhMuc);
            return View(sach);
        }

        // GET: Books/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null) return NotFound();

            var sach = _context.Sach.Find(id);
            if (sach == null) return NotFound();

            ViewBag.DanhMuc = new SelectList(_context.DanhMuc, "MaDanhMuc", "TenDanhMuc", sach.MaDanhMuc);
            return View(sach);
        }

        // POST: Books/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Sach sach, IFormFile HinhAnhFile)
        {
            if (id != sach.MaSach) return NotFound();

            if (ModelState.IsValid)
            {
                var existing = await _context.Sach.FindAsync(id);
                if (existing == null) return NotFound();

                existing.TenSach = sach.TenSach;
                existing.TacGia = sach.TacGia;
                existing.Gia = sach.Gia;
                existing.MoTa = sach.MoTa;
                existing.MaDanhMuc = sach.MaDanhMuc;
                existing.GiamGia = sach.GiamGia;
                existing.IsNoiBat = sach.IsNoiBat;
                existing.IsKhuyenMai = sach.IsKhuyenMai;

                if (HinhAnhFile?.Length > 0)
                {
                    // Xóa file hình cũ nếu tồn tại
                    if (!string.IsNullOrEmpty(existing.HinhAnh))
                    {
                        var oldImgPath = Path.Combine(_environment.WebRootPath, "images", existing.HinhAnh);
                        if (System.IO.File.Exists(oldImgPath))
                        {
                            System.IO.File.Delete(oldImgPath);
                        }
                    }

                    var fileName = Path.GetFileName(HinhAnhFile.FileName);
                    var uniqueFileName = $"{System.Guid.NewGuid()}_{fileName}";
                    var filePath = Path.Combine(_environment.WebRootPath, "images", uniqueFileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await HinhAnhFile.CopyToAsync(stream);
                    existing.HinhAnh = uniqueFileName;
                }

                _context.Update(existing);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.DanhMuc = new SelectList(_context.DanhMuc, "MaDanhMuc", "TenDanhMuc", sach.MaDanhMuc);
            return View(sach);
        }

        // GET: Books/Delete/5
        public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();

            var sach = _context.Sach.Include(s => s.DanhMuc).FirstOrDefault(m => m.MaSach == id);
            if (sach == null) return NotFound();

            return View(sach);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var sach = _context.Sach.Find(id);
            if (sach != null)
            {
                if (!string.IsNullOrEmpty(sach.HinhAnh))
                {
                    var img = Path.Combine(_environment.WebRootPath, "images", sach.HinhAnh);
                    if (System.IO.File.Exists(img))
                        System.IO.File.Delete(img);
                }
                _context.Sach.Remove(sach);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        // Mua ngay
        public IActionResult BuyNow(int id)
        {
            var sach = _context.Sach.FirstOrDefault(s => s.MaSach == id);
            if (sach == null) return NotFound();

            var cartItem = new List<CartItem>
            {
                new CartItem
                {
                    MaSach = sach.MaSach,
                    TenSach = sach.TenSach,
                    Gia = sach.GiaSauGiam,
                    SoLuong = 1
                }
            };

            TempData["BuyNow"] = JsonConvert.SerializeObject(cartItem);
            return RedirectToAction("CheckoutBuyNow", "Cart");
        }
    }
}
