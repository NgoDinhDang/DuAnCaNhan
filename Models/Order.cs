using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STOREBOOKS.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        public string TenKhachHang { get; set; }

        [Required]
        public string Email { get; set; }

        public string? SoDienThoai { get; set; }

        public string? DiaChiGiaoHang { get; set; }

        public DateTime NgayDat { get; set; }

        public decimal TongTien { get; set; }
        [Required]
        public string TrangThai { get; set; } = "Chờ xác nhận";
        // Trạng thái đơn hàng: "Chờ xác nhận", "Đang giao", "Hoàn tất", "Đã huỷ"


        // Đổi thành nullable nếu không bắt buộc
        public int? MaNguoiDung { get; set; }

        public NguoiDung NguoiDung { get; set; }

        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

        /// <summary>
        /// Tính tổng tiền thực tế từ OrderDetails (SoLuong * Gia)
        /// </summary>
        [NotMapped]
        public decimal TongTienThucTe
        {
            get
            {
                return OrderDetails?.Sum(od => od.SoLuong * od.Gia) ?? 0;
            }
        }

        /// <summary>
        /// Kiểm tra xem TongTien có khớp với tổng OrderDetails không
        /// </summary>
        [NotMapped]
        public bool IsTongTienValid
        {
            get
            {
                return Math.Abs(TongTien - TongTienThucTe) < 0.01m; // Cho phép sai số nhỏ do làm tròn
            }
        }

        /// <summary>
        /// Cập nhật TongTien từ OrderDetails
        /// </summary>
        public void RecalculateTongTien()
        {
            TongTien = TongTienThucTe;
        }
    }
}
