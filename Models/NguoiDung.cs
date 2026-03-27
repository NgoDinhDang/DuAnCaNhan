using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STOREBOOKS.Models
{
    public class NguoiDung
    {
        [Key]
        public int MaNguoiDung { get; set; }

        // Khóa ngoại đến bảng TaiKhoan
        [Required]
        public int MaTaiKhoan { get; set; }

        [ForeignKey("MaTaiKhoan")]
        public TaiKhoan TaiKhoan { get; set; }

        // Thông tin khách hàng
        [StringLength(100)]
        public string? HoTen { get; set; }

        [StringLength(20)]
        public string? SoDienThoai { get; set; }

        [StringLength(500)]
        public string? DiaChi { get; set; }

        public DateTime? NgaySinh { get; set; }

        [StringLength(10)]
        public string? GioiTinh { get; set; } // Nam, Nữ, Khác

        public string? AnhDaiDien { get; set; } // Đường dẫn ảnh đại diện

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public DateTime? NgayCapNhat { get; set; }
    }
}
