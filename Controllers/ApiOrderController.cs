using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Data;

namespace STOREBOOKS.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
[Authorize] // Tất cả API trong controller này đều cần đăng nhập
public class ApiOrderController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ApiOrderController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy danh sách đơn hàng với phân trang và lọc
    /// </summary>
    /// <param name="status">Lọc theo trạng thái đơn hàng (ví dụ: "Chờ xác nhận", "Đã thanh toán")</param>
    /// <param name="fromDate">Lọc từ ngày (format: yyyy-MM-dd)</param>
    /// <param name="toDate">Lọc đến ngày (format: yyyy-MM-dd)</param>
    /// <param name="page">Số trang (mặc định: 1)</param>
    /// <param name="pageSize">Số lượng item mỗi trang (mặc định: 20)</param>
    /// <returns>Danh sách đơn hàng với thông tin phân trang</returns>
    /// <response code="200">Trả về danh sách đơn hàng</response>
    /// <response code="401">Chưa đăng nhập</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(string? status = null, DateTime? fromDate = null, DateTime? toDate = null, int page = 1, int pageSize = 20)
    {
        var query = _context.Orders
            .Include(o => o.NguoiDung)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Sach)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.TrangThai == status);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.NgayDat >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(o => o.NgayDat <= toDate.Value);
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.NgayDat)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.OrderId,
                o.TenKhachHang,
                o.Email,
                o.SoDienThoai,
                o.DiaChiGiaoHang,
                o.NgayDat,
                o.TongTien,
                o.TrangThai,
                MaNguoiDung = o.MaNguoiDung,
                SoLuongSanPham = o.OrderDetails.Count
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

    /// <summary>
    /// Lấy chi tiết đơn hàng theo ID
    /// </summary>
    /// <param name="orderId">ID của đơn hàng</param>
    /// <returns>Chi tiết đơn hàng bao gồm thông tin khách hàng và danh sách sản phẩm</returns>
    /// <response code="200">Trả về chi tiết đơn hàng</response>
    /// <response code="404">Không tìm thấy đơn hàng</response>
    [HttpGet("{orderId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Details(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.NguoiDung)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Sach)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });
        }

        return Ok(new
        {
            success = true,
            data = new
            {
                order.OrderId,
                order.TenKhachHang,
                order.Email,
                order.SoDienThoai,
                order.DiaChiGiaoHang,
                order.NgayDat,
                order.TongTien,
                order.TrangThai,
                order.MaNguoiDung,
                ChiTiet = order.OrderDetails.Select(od => new
                {
                    od.OrderDetailId,
                    od.MaSach,
                    od.TenSach,
                    od.SoLuong,
                    od.Gia
                }).ToList()
            }
        });
    }

    /// <summary>
    /// Cập nhật trạng thái đơn hàng (Chỉ dành cho Admin)
    /// </summary>
    /// <param name="orderId">ID của đơn hàng</param>
    /// <param name="status">Trạng thái mới (ví dụ: "Đã xác nhận", "Đang giao", "Đã giao")</param>
    /// <returns>Kết quả cập nhật</returns>
    /// <response code="200">Cập nhật thành công</response>
    /// <response code="404">Không tìm thấy đơn hàng</response>
    /// <response code="403">Không có quyền Admin</response>
    [HttpPut]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStatus(int orderId, string status)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });
        }

        order.TrangThai = status;
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Cập nhật trạng thái thành công", orderId, status });
    }

    /// <summary>
    /// Hủy đơn hàng (chỉ có thể hủy khi đơn hàng ở trạng thái "Chờ xác nhận")
    /// </summary>
    /// <param name="orderId">ID của đơn hàng cần hủy</param>
    /// <returns>Kết quả hủy đơn hàng</returns>
    /// <response code="200">Hủy đơn hàng thành công</response>
    /// <response code="400">Không thể hủy đơn hàng (đã xác nhận hoặc đã giao)</response>
    /// <response code="404">Không tìm thấy đơn hàng</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });
        }

        if (order.TrangThai != "Chờ xác nhận")
        {
            return BadRequest(new { success = false, message = "Chỉ có thể hủy đơn hàng ở trạng thái 'Chờ xác nhận'" });
        }

        order.TrangThai = "Đã hủy";
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Đã hủy đơn hàng", orderId });
    }

    /// <summary>
    /// Lấy danh sách đơn hàng của một user cụ thể
    /// </summary>
    /// <param name="userId">ID của user</param>
    /// <param name="page">Số trang (mặc định: 1)</param>
    /// <param name="pageSize">Số lượng item mỗi trang (mặc định: 20)</param>
    /// <returns>Danh sách đơn hàng của user với thông tin phân trang</returns>
    /// <response code="200">Trả về danh sách đơn hàng</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UserOrders(int userId, int page = 1, int pageSize = 20)
    {
        var query = _context.Orders
            .Where(o => o.MaNguoiDung == userId)
            .OrderByDescending(o => o.NgayDat);

        var totalItems = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.OrderId,
                o.NgayDat,
                o.TongTien,
                o.TrangThai
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


