using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STOREBOOKS.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        // Liên kết với đơn hàng
        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public Order Order { get; set; }

        // Thông tin thanh toán
        [Required]
        public string PaymentMethod { get; set; } = "COD"; // COD, MoMo, VNPay, etc.

        public decimal Amount { get; set; }

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Success, Failed, Cancelled

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        // Thông tin giao dịch từ cổng thanh toán (MoMo, VNPay, etc.)
        public string? TransactionId { get; set; } // Mã giao dịch từ MoMo
        public string? PaymentGatewayOrderId { get; set; } // OrderId gửi đến MoMo (ORDER_123_...)
        public string? PaymentInfo { get; set; } // Thông tin mô tả
        public string? ResponseCode { get; set; } // Mã phản hồi từ cổng thanh toán
        public string? ResponseMessage { get; set; } // Thông báo từ cổng thanh toán

        // Thông tin bổ sung
        public string? PaymentType { get; set; } // webApp, qr, etc.
        public string? IpAddress { get; set; } // IP người thanh toán
        
        // Timestamp
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}

