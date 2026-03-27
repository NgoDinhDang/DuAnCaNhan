using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Data;

namespace STOREBOOKS.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ApiReviewsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ApiReviewsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/ApiReviews/ByBook/5
    [HttpGet("{bookId:int}")]
    public async Task<IActionResult> ByBook(int bookId, int page = 1, int pageSize = 20)
    {
        var query = _context.DanhGia
            .Include(d => d.NguoiDung)
                .ThenInclude(nd => nd.TaiKhoan)
            .Where(d => d.MaSach == bookId && d.DaDuyet);

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(d => d.NgayDanhGia)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new
            {
                d.Id,
                d.SoSao,
                d.BinhLuan,
                d.NgayDanhGia,
                TenNguoiDung = d.NguoiDung.TaiKhoan.TenDangNhap
            })
            .ToListAsync();

        double average = totalItems > 0
            ? Math.Round(await query.AverageAsync(d => d.SoSao), 1)
            : 0;

        return Ok(new
        {
            success = true,
            data = items,
            averageRating = average,
            pagination = new
            {
                page,
                pageSize,
                totalItems,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            }
        });
    }

    public class CreateReviewDto
    {
        public int MaSach { get; set; }
        public int MaNguoiDung { get; set; }
        public int SoSao { get; set; }
        public string? BinhLuan { get; set; }
    }

    // POST: api/ApiReviews/Create
    [HttpPost]
    [Authorize] // Cần đăng nhập để tạo đánh giá
    public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
    {
        var user = await _context.NguoiDung.FirstOrDefaultAsync(u => u.MaNguoiDung == dto.MaNguoiDung);
        if (user == null)
        {
            return BadRequest(new { success = false, message = "Người dùng không hợp lệ" });
        }

        bool existed = await _context.DanhGia
            .AnyAsync(d => d.MaSach == dto.MaSach && d.MaNguoiDung == dto.MaNguoiDung);
        if (existed)
        {
            return BadRequest(new { success = false, message = "Bạn đã đánh giá sách này rồi" });
        }

        var review = new Models.DanhGia
        {
            MaSach = dto.MaSach,
            MaNguoiDung = dto.MaNguoiDung,
            SoSao = dto.SoSao,
            BinhLuan = dto.BinhLuan,
            NgayDanhGia = DateTime.Now,
            DaDuyet = true
        };

        _context.DanhGia.Add(review);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Đã thêm đánh giá", id = review.Id });
    }

    // DELETE: api/ApiReviews/Delete/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")] // Chỉ Admin mới được xóa đánh giá
    public async Task<IActionResult> Delete(int id)
    {
        var review = await _context.DanhGia.FirstOrDefaultAsync(d => d.Id == id);
        if (review == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy đánh giá" });
        }

        _context.DanhGia.Remove(review);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Đã xóa đánh giá" });
    }
}


