using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STOREBOOKS.Models
{
    public class DanhGia
    {
        [Key]
        public int Id { get; set; }

        [Range(1, 5)]
        public int SoSao { get; set; }

        [StringLength(500)]
        public string? BinhLuan { get; set; }

        public DateTime NgayDanhGia { get; set; } = DateTime.Now;

        [ForeignKey("NguoiDung")]
        public int MaNguoiDung { get; set; }
        public NguoiDung NguoiDung { get; set; }

        [ForeignKey("Sach")]
        public int MaSach { get; set; }
        public Sach Sach { get; set; }

        // ➕ Thêm để hỗ trợ duyệt đánh giá
        public bool DaDuyet { get; set; } = true;
    }
}
