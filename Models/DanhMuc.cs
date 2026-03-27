using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STOREBOOKS.Models
{
    public class DanhMuc
    {
        [Key]
        public int MaDanhMuc { get; set; }

        [Required(ErrorMessage = "Tên danh mục là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên danh mục không được dài quá 100 ký tự.")]
        public string TenDanhMuc { get; set; }

        // Thêm trường ParentId để hỗ trợ danh mục cha
        public int? ParentId { get; set; }

        // Tham chiếu đến danh mục cha
        [ForeignKey("ParentId")]
        public DanhMuc? ParentCategory { get; set; }

        // Danh sách danh mục con
        public List<DanhMuc>? SubCategories { get; set; }

        // Danh sách sách thuộc danh mục này
        public List<Sach>? Saches { get; set; }
    }
}