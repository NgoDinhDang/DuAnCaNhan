using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using STOREBOOKS.Services;
using STOREBOOKS.Data;
using STOREBOOKS.Models;
using System;

namespace STOREBOOKS.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly ChatbotService _chatbot;
        private readonly ApplicationDbContext _context;

        public ChatHub(
            ILogger<ChatHub> logger,
            ChatbotService chatbot,
            ApplicationDbContext context)
        {
            _logger = logger;
            _chatbot = chatbot;
            _context = context;
        }

        // Khách hàng gửi tin nhắn - CHỈ admin mới thấy
        public async Task SendCustomerMessage(string userName, string message, string connectionId, int? maNguoiDung = null)
        {
            _logger.LogInformation($"📨 Nhận tin nhắn từ {userName} (ConnectionId: {connectionId}, MaNguoiDung: {maNguoiDung}): {message}");

            // Lưu tin nhắn khách hàng vào database
            try
            {
                var chatMessage = new ChatMessage
                {
                    SenderName = userName,
                    Message = message,
                    SentAt = DateTime.Now,
                    ConnectionId = connectionId,
                    IsAdminMessage = false,
                    MessageType = "text",
                    MaNguoiDung = maNguoiDung
                };
                _context.ChatMessages.Add(chatMessage);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"✅ Đã lưu tin nhắn khách hàng vào database. ID: {chatMessage.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Lỗi lưu tin nhắn khách hàng: {ex.Message}");
            }

            // Gửi tin nhắn đến admin
            await Clients.Group("Admins").SendAsync("ReceiveCustomerMessage",
                userName, message, DateTime.Now.ToString("HH:mm"), connectionId);

            // Gửi lại cho chính khách
            await Clients.Caller.SendAsync("ReceiveOwnMessage",
                userName, message, DateTime.Now.ToString("HH:mm"));

            // ========== TRẢ LỜI TỰ ĐỘNG SAU MỖI TIN NHẮN ==========
            try
            {
                // Chờ 1 giây trước khi trả lời
                await Task.Delay(1000);
                
                // Gửi tin nhắn tự động
                var autoReply = _chatbot.GetAutoReply();
                
                // Lưu tin nhắn bot vào database
                try
                {
                    var botMessage = new ChatMessage
                    {
                        SenderName = "STOREBOOKS Bot",
                        Message = autoReply,
                        SentAt = DateTime.Now,
                        ConnectionId = connectionId,
                        IsAdminMessage = true,
                        MessageType = "text",
                        MaNguoiDung = maNguoiDung
                    };
                    _context.ChatMessages.Add(botMessage);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"✅ Đã lưu tin nhắn bot vào database. ID: {botMessage.Id}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Lỗi lưu tin nhắn bot: {ex.Message}");
                }
                
                await Clients.Caller.SendAsync("ReceiveAdminMessage", 
                    "STOREBOOKS Bot", autoReply, DateTime.Now.ToString("HH:mm"));
                
                _logger.LogInformation($"🤖 Đã gửi tin nhắn tự động đến {connectionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Lỗi gửi tin nhắn tự động: {ex.Message}");
            }
        }

        // Admin trả lời tin nhắn
        public async Task SendAdminReply(string clientConnectionId, string adminName, string message)
        {
            // Lưu tin nhắn admin vào database
            try
            {
                var chatMessage = new ChatMessage
                {
                    SenderName = adminName,
                    Message = message,
                    SentAt = DateTime.Now,
                    ConnectionId = clientConnectionId,
                    IsAdminMessage = true,
                    MessageType = "text"
                };
                _context.ChatMessages.Add(chatMessage);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"✅ Đã lưu tin nhắn admin vào database. ID: {chatMessage.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Lỗi lưu tin nhắn admin: {ex.Message}");
            }

            await Clients.Client(clientConnectionId).SendAsync("ReceiveAdminMessage",
                adminName, message, DateTime.Now.ToString("HH:mm"));

            await Clients.Caller.SendAsync("ReceiveAdminOwnMessage",
                adminName, message, DateTime.Now.ToString("HH:mm"), clientConnectionId);
        }

        // Khách hàng gửi hình
        public async Task SendCustomerImage(string userName, string imageUrl, string connectionId)
        {
            _logger.LogInformation($"📸 Nhận hình từ {userName}: {imageUrl}");

            // Lưu hình ảnh vào database (đã được lưu trong ChatController.UploadChatImage)
            // Chỉ cần gửi qua SignalR ở đây

            await Clients.Group("Admins").SendAsync("ReceiveCustomerImage",
                userName, imageUrl, DateTime.Now.ToString("HH:mm"), connectionId);

            await Clients.Caller.SendAsync("ReceiveOwnImage",
                userName, imageUrl, DateTime.Now.ToString("HH:mm"));
        }

        // Admin gửi hình
        public async Task SendAdminImage(string clientConnectionId, string adminName, string imageUrl)
        {
            // Lưu hình ảnh vào database (đã được lưu trong ChatController.UploadChatImage)
            // Chỉ cần gửi qua SignalR ở đây

            await Clients.Client(clientConnectionId).SendAsync("ReceiveAdminImage",
                adminName, imageUrl, DateTime.Now.ToString("HH:mm"));

            await Clients.Caller.SendAsync("ReceiveAdminOwnImage",
                adminName, imageUrl, DateTime.Now.ToString("HH:mm"), clientConnectionId);
        }

        // Admin broadcast
        public async Task SendAdminBroadcast(string adminName, string message)
        {
            await Clients.AllExcept(Context.ConnectionId).SendAsync("ReceiveAdminMessage",
                adminName + " (Broadcast)", message, DateTime.Now.ToString("HH:mm"));
        }

        // Thêm admin vào group
        public async Task JoinAdminGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            _logger.LogInformation($"👤 Admin đã join group: {Context.ConnectionId}");
        }

        // Xóa admin khỏi group
        public async Task LeaveAdminGroup()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
        }

        // Notify user connect
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"🔌 User kết nối: {Context.ConnectionId}");

            await Clients.Group("Admins").SendAsync("UserConnected", Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        // Notify user disconnect
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"❌ User thoát: {Context.ConnectionId}");

            await Clients.Group("Admins").SendAsync("UserDisconnected", Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }
    }
}
