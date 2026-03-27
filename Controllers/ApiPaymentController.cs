using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Data;

namespace STOREBOOKS.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
[Authorize] // Cần đăng nhập để xem lịch sử thanh toán
public class ApiPaymentController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ApiPaymentController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/ApiPayment/History
    [HttpGet]
    public async Task<IActionResult> History(int? orderId = null, int? userId = null)
    {
        var query = _context.Payments
            .Include(p => p.Order)
            .AsQueryable();

        if (orderId.HasValue)
        {
            query = query.Where(p => p.OrderId == orderId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(p => p.Order.MaNguoiDung == userId.Value);
        }

        var data = await query
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new
            {
                p.PaymentId,
                p.OrderId,
                p.PaymentMethod,
                p.Amount,
                p.Status,
                p.PaymentDate,
                p.TransactionId,
                p.PaymentGatewayOrderId,
                p.ResponseCode,
                p.ResponseMessage,
                p.PaymentType
            })
            .ToListAsync();

        return Ok(new { success = true, data });
    }

    // GET: api/ApiPayment/Status/5
    [HttpGet("{paymentId:int}")]
    public async Task<IActionResult> Status(int paymentId)
    {
        var p = await _context.Payments
            .Include(x => x.Order)
            .FirstOrDefaultAsync(x => x.PaymentId == paymentId);

        if (p == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy thanh toán" });
        }

        return Ok(new
        {
            success = true,
            data = new
            {
                p.PaymentId,
                p.OrderId,
                p.PaymentMethod,
                p.Amount,
                p.Status,
                p.PaymentDate,
                p.TransactionId,
                p.PaymentGatewayOrderId,
                p.ResponseCode,
                p.ResponseMessage,
                p.PaymentType
            }
        });
    }
}


