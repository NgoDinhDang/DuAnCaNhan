using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STOREBOOKS.Models
{
    public class YeuThich
    {
        [Key]
        public int Id { get; set; }

        public int MaNguoiDung { get; set; }

        public int MaSach { get; set; }

        [ForeignKey("MaNguoiDung")]
        public NguoiDung NguoiDung { get; set; }

        [ForeignKey("MaSach")]
        public Sach Sach { get; set; }
    }
}
