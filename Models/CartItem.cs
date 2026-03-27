using STOREBOOKS.Models;

namespace STOREBOOKS.Models
{
    public class CartItem
    {
        public int MaSach { get; set; }
        public string TenSach { get; set; }
        public string TacGia { get; set; }
        public int SoLuong { get; set; }
        public decimal Gia { get; set; }
        public string? HinhAnh { get; set; }
    }
}
