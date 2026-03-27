using Microsoft.AspNetCore.Mvc;
using STOREBOOKS.Data;
using STOREBOOKS.Models;
using STOREBOOKS.Services;
using Newtonsoft.Json;

namespace STOREBOOKS.Controllers
{
    public class PaymentController : Controller
    {
        private readonly MoMoService _momoService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(MoMoService momoService, ApplicationDbContext context, ILogger<PaymentController> logger)
        {
            _momoService = momoService;
            _context = context;
            _logger = logger;
        }

#if DEBUG
        // ==================== TRANG TEST MOMO (CHỈ DEVELOPMENT) ====================
        // Xóa hoặc comment đoạn này khi deploy lên production!
        [HttpGet]
        public IActionResult TestPayment()
        {
            return View();
        }
#endif

        [HttpPost]
        public async Task<IActionResult> CreateMoMoPayment(int orderId)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                var amount = (long)order.TongTien;
                var orderInfo = $"Thanh toán đơn hàng #{orderId} - STOREBOOKS";
                var momoOrderId = $"ORDER_{orderId}_{DateTime.Now.Ticks}";

                var response = await _momoService.CreatePaymentAsync(momoOrderId, amount, orderInfo);

                if (response != null && response.resultCode == 0)
                {
                    // Lưu thông tin payment vào database
                    var payment = new Payment
                    {
                        OrderId = orderId,
                        PaymentMethod = "MoMo",
                        Amount = order.TongTien,
                        Status = "Pending",
                        PaymentGatewayOrderId = momoOrderId,
                        PaymentInfo = orderInfo,
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        CreatedAt = DateTime.Now
                    };
                    
                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();

                    // Lưu thông tin vào Session
                    HttpContext.Session.SetString("MoMoOrderId", momoOrderId);
                    HttpContext.Session.SetInt32("OrderId", orderId);
                    HttpContext.Session.SetInt32("PaymentId", payment.PaymentId);

                    return Json(new 
                    { 
                        success = true, 
                        payUrl = response.payUrl,
                        message = "Đã tạo link thanh toán MoMo thành công"
                    });
                }
                else
                {
                    _logger.LogError($"MoMo payment creation failed: {response?.message}");
                    
                    // Lưu payment thất bại
                    var payment = new Payment
                    {
                        OrderId = orderId,
                        PaymentMethod = "MoMo",
                        Amount = order.TongTien,
                        Status = "Failed",
                        PaymentGatewayOrderId = momoOrderId,
                        PaymentInfo = orderInfo,
                        ResponseCode = response?.resultCode.ToString(),
                        ResponseMessage = response?.message,
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        CreatedAt = DateTime.Now
                    };
                    
                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();

                    return Json(new 
                    { 
                        success = false, 
                        message = $"Không thể tạo thanh toán MoMo: {response?.message}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating MoMo payment");
                return Json(new 
                { 
                    success = false, 
                    message = "Có lỗi xảy ra khi tạo thanh toán MoMo"
                });
            }
        }

        // Callback khi thanh toán thành công/thất bại (người dùng được redirect về)
        [HttpGet]
        public IActionResult MoMoReturn(
            string partnerCode,
            string orderId,
            string requestId,
            long amount,
            string orderInfo,
            string orderType,
            long transId,
            int resultCode,
            string message,
            string payType,
            long responseTime,
            string extraData,
            string signature)
        {
            try
            {
                _logger.LogInformation("=== MOMO RETURN CALLBACK ===");
                _logger.LogInformation($"OrderId: {orderId}");
                _logger.LogInformation($"ResultCode: {resultCode}");
                _logger.LogInformation($"TransId: {transId}");
                _logger.LogInformation($"Amount: {amount}");
                _logger.LogInformation($"Message: {message}");

                var response = new MoMoExecuteResponse
                {
                    partnerCode = partnerCode,
                    orderId = orderId,
                    requestId = requestId,
                    amount = amount,
                    orderInfo = orderInfo,
                    orderType = orderType,
                    transId = transId,
                    resultCode = resultCode,
                    message = message,
                    payType = payType,
                    responseTime = responseTime,
                    extraData = extraData,
                    signature = signature
                };

                // Validate signature
                var isValidSignature = _momoService.ValidateSignature(response);

                if (!isValidSignature)
                {
                    _logger.LogWarning("❌ INVALID MOMO SIGNATURE!");
                    ViewBag.Success = false;
                    ViewBag.Message = "Chữ ký không hợp lệ. Vui lòng liên hệ hỗ trợ.";
                    ViewBag.OrderId = null;
                    return View("PaymentResult");
                }

                // Parse OrderId từ MoMo (format: ORDER_{orderId}_{timestamp})
                int? realOrderId = null;
                try
                {
                    var parts = orderId.Split('_');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int parsedOrderId))
                    {
                        realOrderId = parsedOrderId;
                        _logger.LogInformation($"✅ Parsed OrderId: {realOrderId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing orderId");
                }

                // Lấy OrderId từ session hoặc từ parsed orderId
                var orderIdFromSession = HttpContext.Session.GetInt32("OrderId");
                var finalOrderId = realOrderId ?? orderIdFromSession;

                if (!finalOrderId.HasValue)
                {
                    _logger.LogError("❌ Cannot determine OrderId");
                    ViewBag.Success = false;
                    ViewBag.Message = "Không tìm thấy thông tin đơn hàng.";
                    return View("PaymentResult");
                }

                _logger.LogInformation($"Final OrderId: {finalOrderId}");

                // ========== XỬ LÝ THANH TOÁN THÀNH CÔNG ==========
                if (resultCode == 0)
                {
                    _logger.LogInformation("✅ PAYMENT SUCCESS!");

                    // 1. Cập nhật Order
                    var order = _context.Orders.Find(finalOrderId.Value);
                    if (order != null)
                    {
                        order.TrangThai = "Đã thanh toán";
                        _context.SaveChanges();
                        _logger.LogInformation($"✅ Updated Order #{order.OrderId} to 'Đã thanh toán'");
                    }
                    else
                    {
                        _logger.LogWarning($"⚠️ Order #{finalOrderId} not found");
                    }

                    // 2. Cập nhật hoặc tạo Payment record
                    // Tìm payment theo OrderId hoặc PaymentGatewayOrderId
                    var payment = _context.Payments
                        .FirstOrDefault(p => p.OrderId == finalOrderId.Value && p.PaymentMethod == "MoMo");

                    // Nếu không tìm thấy, thử tìm theo PaymentGatewayOrderId
                    if (payment == null)
                    {
                        payment = _context.Payments
                            .FirstOrDefault(p => p.PaymentGatewayOrderId == orderId && p.PaymentMethod == "MoMo");
                        _logger.LogInformation($"🔍 Tìm payment theo PaymentGatewayOrderId: {orderId}");
                    }

                    if (payment != null)
                    {
                        // Cập nhật payment hiện có
                        payment.Status = "Success";
                        payment.TransactionId = transId.ToString();
                        payment.ResponseCode = resultCode.ToString();
                        payment.ResponseMessage = message;
                        payment.PaymentType = payType;
                        payment.PaymentDate = DateTime.Now;
                        payment.UpdatedAt = DateTime.Now;
                        
                        // Đảm bảo OrderId đúng (trường hợp tìm theo PaymentGatewayOrderId)
                        if (payment.OrderId != finalOrderId.Value)
                        {
                            payment.OrderId = finalOrderId.Value;
                        }
                        
                        _context.SaveChanges();
                        _logger.LogInformation($"✅ RETURN: Updated Payment #{payment.PaymentId} to Success");
                    }
                    else if (order != null)
                    {
                        // Tạo payment record mới (trường hợp session mất hoặc chưa có)
                        payment = new Payment
                        {
                            OrderId = finalOrderId.Value,
                            PaymentMethod = "MoMo",
                            Amount = order.TongTien,
                            Status = "Success",
                            TransactionId = transId.ToString(),
                            PaymentGatewayOrderId = orderId,
                            PaymentInfo = orderInfo,
                            ResponseCode = resultCode.ToString(),
                            ResponseMessage = message,
                            PaymentType = payType,
                            PaymentDate = DateTime.Now,
                            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };
                        _context.Payments.Add(payment);
                        _context.SaveChanges();
                        _logger.LogInformation($"✅ RETURN: Created new Payment record #{payment.PaymentId}");
                    }
                    else
                    {
                        _logger.LogWarning($"⚠️ RETURN: Cannot find or create Payment record for Order #{finalOrderId}");
                    }

                    // 3. Clear session
                    HttpContext.Session.Remove("OrderId");
                    HttpContext.Session.Remove("PaymentId");
                    HttpContext.Session.Remove("MoMoOrderId");

                    // 4. Set ViewBag
                    ViewBag.Success = true;
                    ViewBag.Message = "Thanh toán thành công!";
                    ViewBag.OrderId = finalOrderId;
                    ViewBag.Amount = amount;
                    ViewBag.TransId = transId;
                    ViewBag.PaymentMethod = "MoMo";

                    _logger.LogInformation("=== PAYMENT COMPLETED SUCCESSFULLY ===");
                }
                // ========== XỬ LÝ THANH TOÁN THẤT BẠI ==========
                else
                {
                    _logger.LogWarning($"❌ PAYMENT FAILED! ResultCode: {resultCode}, Message: {message}");

                    // Cập nhật hoặc tạo Payment record với status Failed
                    var payment = _context.Payments
                        .FirstOrDefault(p => p.OrderId == finalOrderId.Value && p.PaymentMethod == "MoMo");

                    if (payment != null)
                    {
                        payment.Status = "Failed";
                        payment.ResponseCode = resultCode.ToString();
                        payment.ResponseMessage = message;
                        payment.UpdatedAt = DateTime.Now;
                    }
                    else
                    {
                        var order = _context.Orders.Find(finalOrderId.Value);
                        if (order != null)
                        {
                            payment = new Payment
                            {
                                OrderId = finalOrderId.Value,
                                PaymentMethod = "MoMo",
                                Amount = order.TongTien,
                                Status = "Failed",
                                PaymentGatewayOrderId = orderId,
                                PaymentInfo = orderInfo,
                                ResponseCode = resultCode.ToString(),
                                ResponseMessage = message,
                                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                                CreatedAt = DateTime.Now,
                                UpdatedAt = DateTime.Now
                            };
                            _context.Payments.Add(payment);
                        }
                    }

                    _context.SaveChanges();

                    ViewBag.Success = false;
                    ViewBag.Message = GetMoMoErrorMessage(resultCode, message);
                    ViewBag.OrderId = finalOrderId;
                    ViewBag.ResultCode = resultCode;
                }

                return View("PaymentResult");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in MoMoReturn");
                ViewBag.Success = false;
                ViewBag.Message = "Có lỗi xảy ra khi xử lý thanh toán. Vui lòng liên hệ hỗ trợ.";
                return View("PaymentResult");
            }
        }

        // Helper method để get message lỗi từ MoMo
        private string GetMoMoErrorMessage(int resultCode, string defaultMessage)
        {
            return resultCode switch
            {
                9 => "Giao dịch bị từ chối bởi merchant",
                10 => "Không thể khởi tạo giao dịch",
                11 => "Truy cập bị từ chối",
                12 => "Số tiền không hợp lệ",
                13 => "Dữ liệu yêu cầu không hợp lệ",
                1000 => "Giao dịch đang được xử lý",
                1001 => "Giao dịch đã được hoàn tất",
                1004 => "Giao dịch đã hết hạn",
                1005 => "Giao dịch thất bại",
                1006 => "Người dùng đã hủy giao dịch",
                9000 => "Giao dịch bị từ chối",
                _ => $"Thanh toán thất bại: {defaultMessage}"
            };
        }

        // IPN (Instant Payment Notification) - MoMo gọi về server
        [HttpPost]
        public IActionResult MoMoIPN([FromBody] MoMoExecuteResponse response)
        {
            _logger.LogInformation("=== MOMO IPN CALLBACK ===");
            _logger.LogInformation($"MoMo IPN: {JsonConvert.SerializeObject(response)}");

            try
            {
                // Validate signature
                var isValidSignature = _momoService.ValidateSignature(response);

                if (!isValidSignature)
                {
                    _logger.LogWarning("❌ Invalid MoMo IPN signature");
                    return Json(new { resultCode = 97, message = "Invalid signature" });
                }

                // Parse OrderId từ MoMo (format: ORDER_{orderId}_{timestamp})
                int? realOrderId = null;
                try
                {
                    var parts = response.orderId.Split('_');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int parsedOrderId))
                    {
                        realOrderId = parsedOrderId;
                        _logger.LogInformation($"✅ Parsed OrderId: {realOrderId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing orderId in IPN");
                }

                if (!realOrderId.HasValue)
                {
                    _logger.LogError("❌ Cannot parse OrderId from IPN");
                    return Json(new { resultCode = 99, message = "Invalid orderId" });
                }

                if (response.resultCode == 0)
                {
                    _logger.LogInformation("✅ IPN: PAYMENT SUCCESS!");

                    // 1. Cập nhật Order
                    var order = _context.Orders.Find(realOrderId.Value);
                    if (order != null && order.TrangThai != "Đã thanh toán")
                    {
                        order.TrangThai = "Đã thanh toán";
                        _context.SaveChanges();
                        _logger.LogInformation($"✅ IPN: Updated Order #{order.OrderId} to 'Đã thanh toán'");
                    }

                    // 2. Cập nhật Payment record
                    var payment = _context.Payments
                        .FirstOrDefault(p => p.OrderId == realOrderId.Value && p.PaymentMethod == "MoMo");

                    if (payment != null)
                    {
                        payment.Status = "Success";
                        payment.TransactionId = response.transId.ToString();
                        payment.ResponseCode = response.resultCode.ToString();
                        payment.ResponseMessage = response.message;
                        payment.PaymentType = response.payType;
                        payment.PaymentDate = DateTime.Now;
                        payment.UpdatedAt = DateTime.Now;
                        _context.SaveChanges();
                        _logger.LogInformation($"✅ IPN: Updated Payment #{payment.PaymentId} to Success");
                    }
                    else
                    {
                        // Tạo payment record mới nếu chưa có
                        if (order != null)
                        {
                            payment = new Payment
                            {
                                OrderId = realOrderId.Value,
                                PaymentMethod = "MoMo",
                                Amount = order.TongTien,
                                Status = "Success",
                                TransactionId = response.transId.ToString(),
                                PaymentGatewayOrderId = response.orderId,
                                PaymentInfo = response.orderInfo,
                                ResponseCode = response.resultCode.ToString(),
                                ResponseMessage = response.message,
                                PaymentType = response.payType,
                                PaymentDate = DateTime.Now,
                                CreatedAt = DateTime.Now,
                                UpdatedAt = DateTime.Now
                            };
                            _context.Payments.Add(payment);
                            _context.SaveChanges();
                            _logger.LogInformation($"✅ IPN: Created new Payment record #{payment.PaymentId}");
                        }
                    }
                }
                else
                {
                    _logger.LogWarning($"❌ IPN: PAYMENT FAILED! ResultCode: {response.resultCode}");

                    // Cập nhật Payment record với status Failed
                    var payment = _context.Payments
                        .FirstOrDefault(p => p.OrderId == realOrderId.Value && p.PaymentMethod == "MoMo");

                    if (payment != null)
                    {
                        payment.Status = "Failed";
                        payment.ResponseCode = response.resultCode.ToString();
                        payment.ResponseMessage = response.message;
                        payment.UpdatedAt = DateTime.Now;
                        _context.SaveChanges();
                        _logger.LogInformation($"✅ IPN: Updated Payment #{payment.PaymentId} to Failed");
                    }
                }

                return Json(new { resultCode = 0, message = "Success" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing MoMo IPN");
                return Json(new { resultCode = 99, message = "Error" });
            }
        }
    }
}

