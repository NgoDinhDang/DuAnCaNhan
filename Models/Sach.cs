using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STOREBOOKS.Models
{
    public class Sach
    {
        [Key]
        public int MaSach { get; set; }

        [NotMapped]
        public IFormFile? HinhAnhFile { get; set; }

        public string? HinhAnh { get; set; }

        [Required(ErrorMessage = "Tên sách là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên sách không được dài quá 100 ký tự.")]
        public string TenSach { get; set; }

        [Required(ErrorMessage = "Tác giả là bắt buộc.")]
        [StringLength(50, ErrorMessage = "Tên tác giả không được dài quá 50 ký tự.")]
        public string TacGia { get; set; }

        [Required(ErrorMessage = "Giá là bắt buộc.")]
        [Range(0, 10000000, ErrorMessage = "Giá phải từ 0 đến 10,000,000 VND.")]
        public decimal Gia { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả không được dài quá 500 ký tự.")]
        public string MoTa { get; set; }

        [Required(ErrorMessage = "Danh mục là bắt buộc.")]
        [ForeignKey("DanhMuc")]
        public int MaDanhMuc { get; set; }

        public DanhMuc? DanhMuc { get; set; }

        // ✅ Trường giảm giá (phần trăm), ví dụ 10 => giảm 10%
        [Range(0, 100, ErrorMessage = "Giảm giá phải từ 0% đến 100%.")]
        public double? GiamGia { get; set; }

        // ✅ Tính giá sau khi giảm
        [NotMapped]
        public decimal GiaSauGiam
        {
            get
            {
                if (GiamGia.HasValue && GiamGia > 0)
                {
                    return Gia * (decimal)(1 - GiamGia.Value / 100);
                }
                return Gia;
            }
        }

        // ✅ Đánh dấu sách nổi bật
        public bool IsNoiBat { get; set; } = false;
        // ✅ Admin có thể bật/tắt sách khuyến mãi theo ý muốn
        public bool IsKhuyenMai { get; set; } = false;

        // ✅ Số lượt xem sách
        public int SoLuotXem { get; set; } = 0;

    }
}
