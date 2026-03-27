using Microsoft.AspNetCore.Mvc;
using STOREBOOKS.Models;
using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Data;

[Route("admin/danhgia")]
public class AdminDanhGiaController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminDanhGiaController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Danh sách đánh giá
    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index()
    {
        var danhGias = await _context.DanhGia
            .Include(d => d.Sach)
            .Include(d => d.NguoiDung)
                .ThenInclude(nd => nd.TaiKhoan)
            .OrderByDescending(d => d.NgayDanhGia)
            .ToListAsync();
        return View(danhGias);
    }

    // POST: Duyệt đánh giá
    [HttpPost("duyet/{id}")]
    public async Task<IActionResult> Duyet(int id)
    {
        var dg = await _context.DanhGia.FindAsync(id);
        if (dg == null) return NotFound();

        dg.DaDuyet = true;
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Đã duyệt đánh giá." });
    }

    // POST: Xóa đánh giá
    [HttpPost("xoa/{id}")]
    public async Task<IActionResult> Xoa(int id)
    {
        var dg = await _context.DanhGia.FindAsync(id);
        if (dg == null) return NotFound();

        _context.DanhGia.Remove(dg);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Đã xóa đánh giá." });
    }
}
