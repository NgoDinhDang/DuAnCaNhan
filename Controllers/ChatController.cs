using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Data;
using STOREBOOKS.Models;

namespace STOREBOOKS.Controllers
{
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ChatController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Trang chat cho admin
        [Route("admin/chat")]
        public IActionResult Admin()
        {
            var role = HttpContext.Session.GetString("VaiTro");
            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        // API lấy lịch sử chat - Lấy tất cả tin nhắn từ database
        [HttpGet]
        public async Task<IActionResult> GetChatHistory(string? connectionId = null, int? maNguoiDung = null, int limit = 100)
        {
            IQueryable<ChatMessage> query = _context.ChatMessages
                .OrderBy(m => m.SentAt); // Sắp xếp từ cũ đến mới

            // Ưu tiên lấy theo MaNguoiDung nếu có (cho user đã đăng nhập)
            if (maNguoiDung.HasValue && maNguoiDung.Value > 0)
            {
                query = query.Where(m => m.MaNguoiDung == maNguoiDung.Value);
            }
            // Nếu không có MaNguoiDung, lấy theo connectionId
            else if (!string.IsNullOrEmpty(connectionId))
            {
                query = query.Where(m => m.ConnectionId == connectionId);
            }

            var messages = await query.Take(limit).ToListAsync();
            
            return Json(new
            {
                success = true,
                messages = messages.Select(m => new
                {
                    id = m.Id,
                    senderName = m.SenderName,
                    message = m.Message,
                    messageType = m.MessageType,
                    imageUrl = m.ImageUrl,
                    sentAt = m.SentAt.ToString("HH:mm"),
                    fullDate = m.SentAt.ToString("dd/MM/yyyy HH:mm:ss"),
                    isAdminMessage = m.IsAdminMessage,
                    connectionId = m.ConnectionId
                })
            });
        }

        // API lấy danh sách các cuộc hội thoại (group by connectionId)
        [HttpGet]
        public async Task<IActionResult> GetConversations(bool onlyUnread = false)
        {
            var query = _context.ChatMessages.Where(m => m.ConnectionId != null);

            // Nếu chỉ lấy tin nhắn chưa đọc
            if (onlyUnread)
            {
                // Lấy các connectionId có tin nhắn chưa đọc từ khách hàng
                var unreadConnectionIds = await _context.ChatMessages
                    .Where(m => m.ConnectionId != null && !m.IsAdminMessage && !m.IsRead)
                    .Select(m => m.ConnectionId)
                    .Distinct()
                    .ToListAsync();

                query = query.Where(m => unreadConnectionIds.Contains(m.ConnectionId));
            }

            var conversations = await query
                .GroupBy(m => m.ConnectionId)
                .Select(g => new
                {
                    connectionId = g.Key,
                    userName = g.OrderByDescending(m => m.SentAt).First().SenderName,
                    lastMessage = g.OrderByDescending(m => m.SentAt).First().Message,
                    lastMessageTime = g.OrderByDescending(m => m.SentAt).First().SentAt,
                    messageCount = g.Count(),
                    unreadCount = g.Count(m => !m.IsAdminMessage && !m.IsRead)
                })
                .OrderByDescending(c => c.lastMessageTime)
                .ToListAsync();

            return Json(new
            {
                success = true,
                conversations = conversations.Select(c => new
                {
                    connectionId = c.connectionId,
                    userName = c.userName,
                    lastMessage = c.lastMessage.Length > 50 ? c.lastMessage.Substring(0, 50) + "..." : c.lastMessage,
                    lastMessageTime = c.lastMessageTime.ToString("HH:mm"),
                    messageCount = c.messageCount,
                    unreadCount = c.unreadCount
                })
            });
        }

        // Đếm số tin nhắn chưa đọc (theo connectionId, chỉ tin nhắn khách gửi)
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount(string? connectionId = null)
        {
            var query = _context.ChatMessages
                .Where(m => !m.IsAdminMessage && !m.IsRead);

            if (!string.IsNullOrEmpty(connectionId))
            {
                query = query.Where(m => m.ConnectionId == connectionId);
            }

            var count = await query.CountAsync();

            return Ok(new { success = true, unreadCount = count });
        }

        // Đánh dấu tin nhắn đã đọc
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var msg = await _context.ChatMessages.FindAsync(id);
            if (msg == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy tin nhắn" });
            }

            msg.IsRead = true;
            msg.ReadAt = DateTime.Now;
            _context.ChatMessages.Update(msg);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // Đánh dấu tất cả tin nhắn của một cuộc hội thoại đã đọc
        [HttpPost]
        public async Task<IActionResult> MarkConversationAsRead(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId))
            {
                return BadRequest(new { success = false, message = "ConnectionId không hợp lệ" });
            }

            var unreadMessages = await _context.ChatMessages
                .Where(m => m.ConnectionId == connectionId && !m.IsAdminMessage && !m.IsRead)
                .ToListAsync();

            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
                msg.ReadAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, markedCount = unreadMessages.Count });
        }

        // Xóa tin nhắn
        [HttpDelete]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var msg = await _context.ChatMessages.FindAsync(id);
            if (msg == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy tin nhắn" });
            }

            _context.ChatMessages.Remove(msg);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // Tìm kiếm tin nhắn theo từ khóa
        [HttpGet]
        public async Task<IActionResult> SearchMessages(string keyword, string? connectionId = null, int limit = 100)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest(new { success = false, message = "Keyword không được để trống" });
            }

            var baseQuery = _context.ChatMessages
                .Where(m => m.Message.Contains(keyword));

            if (!string.IsNullOrEmpty(connectionId))
            {
                baseQuery = baseQuery.Where(m => m.ConnectionId == connectionId);
            }

            var messages = await baseQuery
                .OrderByDescending(m => m.SentAt)
                .Take(limit)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                messages = messages.Select(m => new
                {
                    id = m.Id,
                    senderName = m.SenderName,
                    message = m.Message,
                    messageType = m.MessageType,
                    imageUrl = m.ImageUrl,
                    sentAt = m.SentAt.ToString("HH:mm"),
                    fullDate = m.SentAt.ToString("dd/MM/yyyy HH:mm:ss"),
                    isAdminMessage = m.IsAdminMessage,
                    connectionId = m.ConnectionId,
                    isRead = m.IsRead,
                    readAt = m.ReadAt
                })
            });
        }

        // API lưu tin nhắn
        [HttpPost]
        public async Task<IActionResult> SaveMessage([FromBody] ChatMessageDto dto)
        {
            if (string.IsNullOrEmpty(dto.Message))
            {
                return BadRequest("Tin nhắn không được để trống");
            }

            try
            {
                var chatMessage = new ChatMessage
                {
                    SenderName = dto.SenderName ?? "Khách",
                    Message = dto.Message,
                    SentAt = DateTime.Now,
                    ConnectionId = dto.ConnectionId,
                    IsAdminMessage = dto.IsAdminMessage,
                    MaNguoiDung = dto.MaNguoiDung,
                    MessageType = "text",  // ← FIX: Thêm MessageType
                    ImageUrl = null        // ← Explicitly set null cho image
                };

                _context.ChatMessages.Add(chatMessage);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    success = true,
                    id = chatMessage.Id, 
                    sentAt = chatMessage.SentAt 
                });
            }
            catch (Exception ex)
            {
                // Log lỗi chi tiết
                return BadRequest(new { 
                    success = false,
                    error = ex.Message,
                    innerError = ex.InnerException?.Message 
                });
            }
        }

        // API upload hình ảnh cho chat
        [HttpPost]
        public async Task<IActionResult> UploadChatImage(IFormFile file, [FromForm] string? senderName, [FromForm] string? connectionId, [FromForm] bool isAdminMessage, [FromForm] int? maNguoiDung)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, error = "Không có file được chọn" });
                }

                // Kiểm tra file có phải là hình ảnh không
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { success = false, error = "Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .gif, .webp)" });
                }

                // Giới hạn kích thước file (5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { success = false, error = "Kích thước file không được vượt quá 5MB" });
                }

                // Tạo tên file unique
                var fileName = $"chat_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}{fileExtension}";
                
                // Đường dẫn lưu file
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "chat");
                
                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, fileName);
                
                // Lưu file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // URL để truy cập hình ảnh
                var imageUrl = $"/uploads/chat/{fileName}";

                // Lưu vào database
                var chatMessage = new ChatMessage
                {
                    SenderName = senderName ?? "Khách",
                    Message = "Đã gửi một hình ảnh", // Placeholder text
                    SentAt = DateTime.Now,
                    ConnectionId = connectionId,
                    IsAdminMessage = isAdminMessage,
                    MaNguoiDung = maNguoiDung,
                    MessageType = "image",
                    ImageUrl = imageUrl
                };

                _context.ChatMessages.Add(chatMessage);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    id = chatMessage.Id,
                    imageUrl = imageUrl,
                    fileName = fileName,
                    sentAt = chatMessage.SentAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }
    }

    // DTO cho tin nhắn
    public class ChatMessageDto
    {
        public string? SenderName { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ConnectionId { get; set; }
        public bool IsAdminMessage { get; set; }
        public int? MaNguoiDung { get; set; }
    }
}

