using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STOREBOOKS.Models
{
    public class OrderDetail
    {
        [Key]
        public int OrderDetailId { get; set; }

        [ForeignKey("Order")]
        public int OrderId { get; set; }
        public Order Order { get; set; }

        [ForeignKey("Sach")]
        public int MaSach { get; set; }
        public Sach Sach { get; set; }

        public int SoLuong { get; set; }
        public string? TenSach { get; set; } // optional

        public decimal Gia { get; set; }
    }
}
