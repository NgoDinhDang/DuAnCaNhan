using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Data;
using STOREBOOKS.Models;
using STOREBOOKS.ViewModels;

namespace STOREBOOKS.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminPaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminPaymentController> _logger;
        private static readonly string[] AllowedStatuses = new[] { "Pending", "Success", "Failed", "Cancelled" };

        public AdminPaymentController(ApplicationDbContext context, ILogger<AdminPaymentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
            string status = "all",
            string method = "all",
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? keyword = null)
        {
            var query = _context.Payments
                .Include(p => p.Order)
                .AsQueryable();

            _logger.LogInformation("AdminPayment Index called with filters: Status={Status}, Method={Method}, FromDate={FromDate}, ToDate={ToDate}, Keyword={Keyword}",
                status, method, fromDate, toDate, keyword);

            if (!string.IsNullOrWhiteSpace(status) && status != "all")
            {
                query = query.Where(p => p.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(method) && method != "all")
            {
                query = query.Where(p => p.PaymentMethod == method);
            }

            if (fromDate.HasValue)
            {
                var start = fromDate.Value.Date;
                query = query.Where(p => p.CreatedAt >= start);
            }

            if (toDate.HasValue)
            {
                var end = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(p => p.CreatedAt <= end);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(p =>
                    p.PaymentId.ToString().Contains(keyword) ||
                    p.OrderId.ToString().Contains(keyword) ||
                    (p.TransactionId != null && p.TransactionId.Contains(keyword)) ||
                    (p.Order != null && p.Order.TenKhachHang.Contains(keyword)));
            }

            var paymentEntities = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("AdminPayment Index returning {Count} payments", paymentEntities.Count);

            var viewModel = new PaymentAdminIndexViewModel
            {
                Filters = new PaymentFilterViewModel
                {
                    Status = status,
                    Method = method,
                    FromDate = fromDate,
                    ToDate = toDate,
                    Keyword = keyword
                },
                Payments = paymentEntities.Select(p => new PaymentListItemViewModel
                {
                    PaymentId = p.PaymentId,
                    OrderId = p.OrderId,
                    CustomerName = p.Order?.TenKhachHang ?? "Khách lẻ",
                    PaymentMethod = p.PaymentMethod,
                    Status = p.Status,
                    Amount = p.Amount,
                    CreatedAt = p.CreatedAt,
                    PaymentDate = p.PaymentDate,
                    TransactionId = p.TransactionId,
                    ResponseMessage = p.ResponseMessage,
                    Payment = p
                }).ToList()
            };

            viewModel.Summary = new PaymentSummaryViewModel
            {
                TotalPayments = paymentEntities.Count,
                SuccessCount = paymentEntities.Count(p => p.Status == "Success"),
                PendingCount = paymentEntities.Count(p => p.Status == "Pending"),
                FailedCount = paymentEntities.Count(p => p.Status == "Failed"),
                SuccessAmount = paymentEntities
                    .Where(p => p.Status == "Success")
                    .Sum(p => p.Amount)
            };

            viewModel.AvailableMethods = paymentEntities
                .Select(p => p.PaymentMethod)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            if (!viewModel.AvailableMethods.Any())
            {
                viewModel.AvailableMethods = new List<string> { "MoMo", "COD", "VNPay" };
            }

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.Sach)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
            {
                _logger.LogWarning("AdminPayment Details requested for PaymentId={PaymentId} but not found", id);
                return NotFound();
            }

            _logger.LogInformation("AdminPayment Details loaded for PaymentId={PaymentId}", id);
            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? returnStatus, string? returnMethod, string? keyword, DateTime? fromDate, DateTime? toDate)
        {
            if (string.IsNullOrWhiteSpace(status) || !AllowedStatuses.Contains(status))
            {
                TempData["Error"] = "Trạng thái không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                _logger.LogWarning("AdminPayment UpdateStatus PaymentId={PaymentId} not found", id);
                TempData["Error"] = "Không tìm thấy thông tin thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            payment.Status = status;
            payment.UpdatedAt = DateTime.Now;

            if (status == "Success")
            {
                if (payment.PaymentDate == default)
                {
                    payment.PaymentDate = DateTime.Now;
                }
                var order = await _context.Orders.FindAsync(payment.OrderId);
                if (order != null && order.TrangThai != "Đã thanh toán")
                {
                    order.TrangThai = "Đã thanh toán";
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("AdminPayment UpdateStatus updated PaymentId={PaymentId} to Status={Status}", payment.PaymentId, status);
            TempData["Message"] = $"Cập nhật trạng thái thanh toán #{payment.PaymentId} thành công.";

            return RedirectToAction(nameof(Index), new
            {
                status = returnStatus ?? "all",
                method = returnMethod ?? "all",
                keyword,
                fromDate,
                toDate
            });
        }
    }
}


