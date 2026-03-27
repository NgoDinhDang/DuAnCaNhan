using System.ComponentModel.DataAnnotations;

namespace STOREBOOKS.Models
{
    public class TaiKhoan
    {
        [Key]
        public int MaTaiKhoan { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(50)]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100)]
        public string MatKhau { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(20)]
        public string? VaiTro { get; set; } // Admin, User,...

        public bool BiChan { get; set; } = false;

        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Quan hệ 1-1 với NguoiDung
        public NguoiDung? NguoiDung { get; set; }
    }
}

