using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Data;

namespace STOREBOOKS.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
[Authorize] // Tất cả API trong controller này đều cần đăng nhập
public class ApiUsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ApiUsersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/ApiUsers/Profile/5
    [HttpGet("{userId:int}")]
    public async Task<IActionResult> Profile(int userId)
    {
        var nguoiDung = await _context.NguoiDung
            .Include(nd => nd.TaiKhoan)
            .FirstOrDefaultAsync(u => u.MaNguoiDung == userId);
        if (nguoiDung == null || nguoiDung.TaiKhoan == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy người dùng" });
        }

        return Ok(new
        {
            success = true,
            data = new
            {
                nguoiDung.MaNguoiDung,
                tenDangNhap = nguoiDung.TaiKhoan.TenDangNhap,
                email = nguoiDung.TaiKhoan.Email,
                vaiTro = nguoiDung.TaiKhoan.VaiTro,
                biChan = nguoiDung.TaiKhoan.BiChan
            }
        });
    }

    public class UpdateUserProfileDto
    {
        public string? TenDangNhap { get; set; }
        public string? Email { get; set; }
    }

    // PUT: api/ApiUsers/UpdateProfile
    [HttpPut]
    public async Task<IActionResult> UpdateProfile(int userId, [FromBody] UpdateUserProfileDto dto)
    {
        var nguoiDung = await _context.NguoiDung
            .Include(nd => nd.TaiKhoan)
            .FirstOrDefaultAsync(u => u.MaNguoiDung == userId);
        if (nguoiDung == null || nguoiDung.TaiKhoan == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy người dùng" });
        }

        if (!string.IsNullOrWhiteSpace(dto.TenDangNhap))
        {
            nguoiDung.TaiKhoan.TenDangNhap = dto.TenDangNhap;
        }

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            nguoiDung.TaiKhoan.Email = dto.Email;
        }

        _context.TaiKhoan.Update(nguoiDung.TaiKhoan);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Cập nhật thông tin thành công" });
    }

    // GET: api/ApiUsers/List
    [HttpGet]
    [Authorize(Roles = "Admin")] // Chỉ Admin mới được xem danh sách user
    public async Task<IActionResult> List(string? keyword = null, string? role = null, int page = 1, int pageSize = 20)
    {
        var query = _context.NguoiDung
            .Include(nd => nd.TaiKhoan)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(u => u.TaiKhoan.TenDangNhap.Contains(keyword) || u.TaiKhoan.Email.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.TaiKhoan.VaiTro == role);
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderBy(u => u.TaiKhoan.TenDangNhap)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.MaNguoiDung,
                tenDangNhap = u.TaiKhoan.TenDangNhap,
                email = u.TaiKhoan.Email,
                vaiTro = u.TaiKhoan.VaiTro,
                biChan = u.TaiKhoan.BiChan
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
}


