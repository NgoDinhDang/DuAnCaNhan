using STOREBOOKS.Models;

namespace STOREBOOKS.ViewModels
{
    public class SachChiTietViewModel
    {
        public Sach Sach { get; set; }
        public List<DanhGia> DanhGias { get; set; }
        public DanhGia DanhGiaMoi { get; set; }
        public bool IsYeuThich { get; set; }

        public List<Sach> SachLienQuan { get; set; } // Thêm dòng này
    }


}
