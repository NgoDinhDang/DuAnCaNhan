using System;
using System.Collections.Generic;

namespace STOREBOOKS.ViewModels
{
    // Dùng để hiển thị doanh thu theo từng ngày
    public class ThongKeDoanhThuViewModel
    {
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public decimal TongDoanhThu { get; set; }

        public List<DoanhThuTheoNgay> DoanhThuTheoNgayList { get; set; } = new();
        public List<DoanhThuTheoThang> DoanhThuTheoThangList { get; set; } = new();
        public List<DoanhThuTheoNam> DoanhThuTheoNamList { get; set; } = new();
    }

    public class DoanhThuTheoNgay
    {
        public DateTime Ngay { get; set; }
        public decimal TongTien { get; set; }
    }

    public class DoanhThuTheoThang
    {
        public int Thang { get; set; }
        public int Nam { get; set; }
        public decimal TongTien { get; set; }
    }

    public class DoanhThuTheoNam
    {
        public int Nam { get; set; }
        public decimal TongTien { get; set; }
    }

}
