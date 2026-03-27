using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STOREBOOKS.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SenderName { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.Now;

        public string? ConnectionId { get; set; }

        public bool IsAdminMessage { get; set; } = false;

        // Hỗ trợ upload hình ảnh
        public string? ImageUrl { get; set; } // Đường dẫn đến hình ảnh (nếu có)

        public string MessageType { get; set; } = "text"; // "text" hoặc "image"

        // Liên kết với người dùng (nếu đã đăng nhập)
        public int? MaNguoiDung { get; set; }

        [ForeignKey("MaNguoiDung")]
        public NguoiDung? NguoiDung { get; set; }

        // Trạng thái đã đọc (cho phía admin)
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
    }
}

