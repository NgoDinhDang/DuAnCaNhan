using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Models;

namespace STOREBOOKS.Data
{
    /// <summary>
    /// Generates lightweight sample data so that analytic pages (thống kê doanh thu) always have something to render.
    /// </summary>
    public static class SampleDataSeeder
    {
        private static readonly string[] SampleCustomers =
        {
            "Nguyễn Văn An", "Trần Thị Bình", "Lê Quốc Cường", "Phạm Minh Duy",
            "Võ Hoàng Em", "Bùi Hải Giang", "Đặng Nhật Hà", "Huỳnh Đức Khải",
            "Tạ Hữu Lộc", "Đoàn Thanh Mai", "Ngô Bảo Ngọc", "Trịnh Gia Phúc"
        };

        private static readonly string[] SampleEmails =
        {
            "an.nguyen@example.com", "binh.tran@example.com", "cuong.le@example.com",
            "duy.pham@example.com", "em.vo@example.com", "giang.bui@example.com",
            "ha.dang@example.com", "khai.huynh@example.com", "loc.ta@example.com",
            "mai.doan@example.com", "ngoc.ngo@example.com", "phuc.trinh@example.com"
        };

        /// <summary>
        /// Add 4 months of randomised order data when the Orders table is empty (typically on fresh databases).
        /// </summary>
        public static async Task SeedRevenueDataAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
        {
            // If the database already has completed/delivered orders we assume real data exists.
            var hasRevenueOrders = await context.Orders.AnyAsync(
                o => o.TrangThai == "Hoàn tất" || o.TrangThai == "Đã giao",
                cancellationToken);

            if (hasRevenueOrders)
            {
                return;
            }

            var random = new Random();
            var startDate = new DateTime(DateTime.Today.Year, Math.Max(1, DateTime.Today.Month - 3), 1);
            var orders = new List<Order>();

            // Generate roughly one order per day across ~120 days for nicer charts.
            for (int i = 0; i < 120; i++)
            {
                var date = startDate.AddDays(i);
                var customerIndex = random.Next(SampleCustomers.Length);
                var statusRoll = random.NextDouble();
                var status = statusRoll < 0.85 ? "Hoàn tất" : "Đã giao";

                orders.Add(new Order
                {
                    TenKhachHang = SampleCustomers[customerIndex],
                    Email = SampleEmails[customerIndex],
                    SoDienThoai = $"09{random.Next(10000000, 99999999)}",
                    DiaChiGiaoHang = $"Số {random.Next(10, 250)}, Q.{random.Next(1, 12)}, TP.HCM",
                    NgayDat = date.AddHours(random.Next(8, 22)).AddMinutes(random.Next(0, 60)),
                    TongTien = Math.Round((decimal)(random.Next(2, 10) * 120000 + random.NextDouble() * 50000), 0),
                    TrangThai = status
                });
            }

            await context.Orders.AddRangeAsync(orders, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
