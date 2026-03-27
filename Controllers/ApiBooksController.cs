using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Data;

namespace STOREBOOKS.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ApiBooksController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ApiBooksController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/ApiBooks/List
    [HttpGet]
    public async Task<IActionResult> List(int page = 1, int pageSize = 20, int? categoryId = null)
    {
        var query = _context.Sach
            .Include(s => s.DanhMuc)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(s => s.MaDanhMuc == categoryId.Value);
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(s => s.IsNoiBat)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.MaSach,
                s.TenSach,
                s.TacGia,
                s.Gia,
                s.GiamGia,
                GiaSauGiam = s.GiaSauGiam,
                s.IsNoiBat,
                s.IsKhuyenMai,
                s.HinhAnh,
                DanhMuc = s.DanhMuc != null ? s.DanhMuc.TenDanhMuc : null
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = items,
            pagination = new
            {
                page,
                pageSize,
                totalItems,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            }
        });
    }

    // GET: api/ApiBooks/Search
    [HttpGet]
    public async Task<IActionResult> Search(string keyword, int page = 1, int pageSize = 20)
    {
        keyword = keyword ?? string.Empty;

        var query = _context.Sach
            .Include(s => s.DanhMuc)
            .Where(s =>
                s.TenSach.Contains(keyword) ||
                s.TacGia.Contains(keyword));

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(s => s.IsNoiBat)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.MaSach,
                s.TenSach,
                s.TacGia,
                s.Gia,
                s.GiamGia,
                GiaSauGiam = s.GiaSauGiam,
                s.IsNoiBat,
                s.IsKhuyenMai,
                s.HinhAnh,
                DanhMuc = s.DanhMuc != null ? s.DanhMuc.TenDanhMuc : null
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = items,
            pagination = new
            {
                page,
                pageSize,
                totalItems,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            }
        });
    }

    // GET: api/ApiBooks/BestSellers
    [HttpGet]
    public async Task<IActionResult> BestSellers(int limit = 10)
    {
        // Tạm thời dùng IsNoiBat làm best seller
        var items = await _context.Sach
            .Where(s => s.IsNoiBat)
            .OrderByDescending(s => s.Gia)
            .Take(limit)
            .Select(s => new
            {
                s.MaSach,
                s.TenSach,
                s.TacGia,
                s.Gia,
                s.GiamGia,
                GiaSauGiam = s.GiaSauGiam,
                s.HinhAnh
            })
            .ToListAsync();

        return Ok(new { success = true, data = items });
    }

    // GET: api/ApiBooks/Promotions
    [HttpGet]
    public async Task<IActionResult> Promotions(int limit = 10)
    {
        var items = await _context.Sach
            .Where(s => s.IsKhuyenMai && s.GiamGia > 0)
            .OrderByDescending(s => s.GiamGia)
            .Take(limit)
            .Select(s => new
            {
                s.MaSach,
                s.TenSach,
                s.TacGia,
                s.Gia,
                s.GiamGia,
                GiaSauGiam = s.GiaSauGiam,
                s.HinhAnh
            })
            .ToListAsync();

        return Ok(new { success = true, data = items });
    }

    // GET: api/ApiBooks/Related
    [HttpGet]
    public async Task<IActionResult> Related(int bookId, int limit = 5)
    {
        var sach = await _context.Sach.FindAsync(bookId);
        if (sach == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy sách" });
        }

        var items = await _context.Sach
            .Where(s => s.MaDanhMuc == sach.MaDanhMuc && s.MaSach != sach.MaSach)
            .OrderByDescending(s => s.IsNoiBat)
            .Take(limit)
            .Select(s => new
            {
                s.MaSach,
                s.TenSach,
                s.TacGia,
                s.Gia,
                s.GiamGia,
                GiaSauGiam = s.GiaSauGiam,
                s.HinhAnh
            })
            .ToListAsync();

        return Ok(new { success = true, data = items });
    }
}


