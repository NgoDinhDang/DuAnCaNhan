using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using STOREBOOKS.Data;
using STOREBOOKS.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STOREBOOKS.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CARTKEY = "cart";

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        List<CartItem> GetCartItems()
        {
            var session = HttpContext.Session;
            string jsonCart = session.GetString(CARTKEY);
            if (jsonCart != null)
            {
                return JsonConvert.DeserializeObject<List<CartItem>>(jsonCart);
            }
            return new List<CartItem>();
        }

        void SaveCartSession(List<CartItem> ls)
        {
            var session = HttpContext.Session;
            string jsonCart = JsonConvert.SerializeObject(ls);
            session.SetString(CARTKEY, jsonCart);
        }

        public IActionResult Index()
        {
            return View(GetCartItems());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int id)
        {
            var product = _context.Sach.FirstOrDefault(p => p.MaSach == id);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });
            }

            var cart = GetCartItems();
            var cartItem = cart.Find(p => p.MaSach == id);
            if (cartItem != null)
            {
                cartItem.SoLuong++;
            }
            else
            {
                cart.Add(new CartItem()
                {
                    MaSach = product.MaSach,
                    TenSach = product.TenSach,
                    Gia = product.Gia,
                    SoLuong = 1,
                    HinhAnh = product.HinhAnh
                });
            }
            SaveCartSession(cart);

            return Json(new { success = true, message = "Sách đã được thêm vào giỏ hàng." });
        }

        [HttpPost]
        public IActionResult BuyNow(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("TenDangNhap")))
            {
                return RedirectToAction("Login", "Account");
            }

            var product = _context.Sach.FirstOrDefault(p => p.MaSach == id);
            if (product == null)
                return NotFound();

            var tempCart = new List<CartItem>
            {
                new CartItem
                {
                    MaSach = product.MaSach,
                    TenSach = product.TenSach,
                    Gia = product.Gia,
                    SoLuong = 1,
                    HinhAnh = product.HinhAnh
                }
            };

            TempData["BuyNow"] = JsonConvert.SerializeObject(tempCart);
            return RedirectToAction("CheckoutBuyNow");
        }

        public IActionResult Remove(int id)
        {
            var cart = GetCartItems();
            var cartItem = cart.Find(p => p.MaSach == id);
            if (cartItem != null)
            {
                cart.Remove(cartItem);
            }
            SaveCartSession(cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Update(int id, int quantity)
        {
            var cart = GetCartItems();
            var cartItem = cart.Find(p => p.MaSach == id);
            if (cartItem != null)
            {
                cartItem.SoLuong = quantity;
            }
            SaveCartSession(cart);
            return RedirectToAction(nameof(Index));
        }

        // ✅ AJAX cập nhật số lượng (dùng cho view index)
        [HttpPost]
        public IActionResult CapNhatSoLuong(int id, int soLuong)
        {
            var cart = GetCartItems();
            var cartItem = cart.FirstOrDefault(p => p.MaSach == id);
            if (cartItem != null)
            {
                cartItem.SoLuong = soLuong;
            }

            SaveCartSession(cart);

            var thanhTien = cartItem.Gia * cartItem.SoLuong;
            var tongTien = cart.Sum(p => p.Gia * p.SoLuong);

            return Json(new
            {
                thanhTien = thanhTien,
                tongTien = tongTien
            });
        }

        public IActionResult Clear()
        {
            SaveCartSession(new List<CartItem>());
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Checkout()
        {
            var cart = GetCartItems();
            return View("Checkout", cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(string tenKhachHang, string email, string soDienThoai, string diaChiGiaoHang)
        {
            var cart = GetCartItems();

            if (cart == null || cart.Count == 0)
            {
                ModelState.AddModelError("", "Giỏ hàng đang trống.");
                return View("Checkout", cart);
            }

            if (!ModelState.IsValid)
                return View("Checkout", cart);

            var order = new Order
            {
                TenKhachHang = tenKhachHang,
                Email = email,
                SoDienThoai = soDienThoai,
                DiaChiGiaoHang = diaChiGiaoHang,
                NgayDat = DateTime.Now,
                TrangThai = "Chờ xác nhận",
                TongTien = cart.Sum(x => x.SoLuong * x.Gia),
                OrderDetails = cart.Select(item => new OrderDetail
                {
                    MaSach = item.MaSach,
                    SoLuong = item.SoLuong,
                    Gia = item.Gia
                }).ToList()
            };

            if (HttpContext.Session.GetInt32("MaNguoiDung") is int maNguoiDung)
            {
                order.MaNguoiDung = maNguoiDung;
            }

            try
            {
                _context.Orders.Add(order);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                var error = ex;
                while (error.InnerException != null)
                    error = error.InnerException;

                ModelState.AddModelError("", "Lỗi khi lưu đơn hàng: " + error.Message);
                return View("Checkout", cart);
            }

            TempData["MaDonHang"] = order.OrderId;

            SaveCartSession(new List<CartItem>());

            return RedirectToAction("OrderSuccess");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CheckoutBuyNow(string tenKhachHang, string email, string soDienThoai, string diaChiGiaoHang)
        {
            var buyNowData = TempData["BuyNow"] as string;
            if (string.IsNullOrEmpty(buyNowData))
            {
                ModelState.AddModelError("", "Không có sản phẩm để mua.");
                return View(new List<CartItem>());
            }

            var cart = JsonConvert.DeserializeObject<List<CartItem>>(buyNowData);

            if (!ModelState.IsValid)
                return View(cart);

            var order = new Order
            {
                TenKhachHang = tenKhachHang,
                Email = email,
                SoDienThoai = soDienThoai,
                DiaChiGiaoHang = diaChiGiaoHang,
                NgayDat = DateTime.Now,
                TrangThai = "Chờ xác nhận",
                TongTien = cart.Sum(x => x.SoLuong * x.Gia),
                OrderDetails = cart.Select(item => new OrderDetail
                {
                    MaSach = item.MaSach,
                    SoLuong = item.SoLuong,
                    Gia = item.Gia
                }).ToList()
            };

            if (HttpContext.Session.GetInt32("MaNguoiDung") is int maNguoiDung)
            {
                order.MaNguoiDung = maNguoiDung;
            }

            _context.Orders.Add(order);
            _context.SaveChanges();

            TempData["MaDonHang"] = order.OrderId;

            return RedirectToAction("OrderSuccess");
        }

        public IActionResult CheckoutBuyNow()
        {
            TempData.Keep("BuyNow");

            var buyNowData = TempData["BuyNow"] as string;

            if (string.IsNullOrEmpty(buyNowData))
            {
                ViewBag.Message = "Không có sản phẩm để thanh toán";
                return View(new List<CartItem>());
            }

            var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(buyNowData);

            return View(cartItems);
        }

        public IActionResult OrderSuccess()
        {
            return View();
        }
        [HttpGet]
        public IActionResult GetCartItemCount()
        {
            var cart = GetCartItems();
            int count = cart.Sum(p => p.SoLuong);
            return Json(new { count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutBuyNowWithMoMo(string tenKhachHang, string email, string soDienThoai, string diaChiGiaoHang)
        {
            var buyNowData = TempData["BuyNow"] as string;
            if (string.IsNullOrEmpty(buyNowData))
            {
                return Json(new { success = false, message = "Không có sản phẩm để mua." });
            }

            TempData.Keep("BuyNow");
            var cart = JsonConvert.DeserializeObject<List<CartItem>>(buyNowData);

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin." });
            }

            var order = new Order
            {
                TenKhachHang = tenKhachHang,
                Email = email,
                SoDienThoai = soDienThoai,
                DiaChiGiaoHang = diaChiGiaoHang,
                NgayDat = DateTime.Now,
                TrangThai = "Chờ thanh toán",
                TongTien = cart.Sum(x => x.SoLuong * x.Gia),
                OrderDetails = cart.Select(item => new OrderDetail
                {
                    MaSach = item.MaSach,
                    SoLuong = item.SoLuong,
                    Gia = item.Gia
                }).ToList()
            };

            if (HttpContext.Session.GetInt32("MaNguoiDung") is int maNguoiDung)
            {
                order.MaNguoiDung = maNguoiDung;
            }

            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                return Json(new { success = true, orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi tạo đơn hàng: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CheckoutWithMoMo(string tenKhachHang, string email, string soDienThoai, string diaChiGiaoHang)
        {
            var cart = GetCartItems();

            if (cart == null || cart.Count == 0)
            {
                return Json(new { success = false, message = "Giỏ hàng đang trống." });
            }

            if (string.IsNullOrEmpty(tenKhachHang) || string.IsNullOrEmpty(email) || 
                string.IsNullOrEmpty(soDienThoai) || string.IsNullOrEmpty(diaChiGiaoHang))
            {
                return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin." });
            }

            var order = new Order
            {
                TenKhachHang = tenKhachHang,
                Email = email,
                SoDienThoai = soDienThoai,
                DiaChiGiaoHang = diaChiGiaoHang,
                NgayDat = DateTime.Now,
                TrangThai = "Chờ thanh toán",
                TongTien = cart.Sum(x => x.SoLuong * x.Gia),
                OrderDetails = cart.Select(item => new OrderDetail
                {
                    MaSach = item.MaSach,
                    SoLuong = item.SoLuong,
                    Gia = item.Gia
                }).ToList()
            };

            if (HttpContext.Session.GetInt32("MaNguoiDung") is int maNguoiDung)
            {
                order.MaNguoiDung = maNguoiDung;
            }

            try
            {
                _context.Orders.Add(order);
                _context.SaveChanges();

                // Clear cart after creating order
                SaveCartSession(new List<CartItem>());

                return Json(new 
                { 
                    success = true, 
                    orderId = order.OrderId,
                    message = "Đơn hàng đã được tạo thành công"
                });
            }
            catch (Exception ex)
            {
                var error = ex;
                while (error.InnerException != null)
                    error = error.InnerException;

                return Json(new { success = false, message = "Lỗi khi lưu đơn hàng: " + error.Message });
            }
        }

    }
}
