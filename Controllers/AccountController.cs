using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Data;
using STOREBOOKS.Models;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System;
using STOREBOOKS.ViewModels;
using Microsoft.Extensions.Logging;

namespace STOREBOOKS.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }
        public IActionResult ThongTin()
        {
            var tenDangNhap = HttpContext.Session.GetString("TenDangNhap");

            if (string.IsNullOrEmpty(tenDangNhap))
            {
                return RedirectToAction("Login");
            }

            var taiKhoan = _context.TaiKhoan
                .Include(tk => tk.NguoiDung)
                .FirstOrDefault(x => x.TenDangNhap == tenDangNhap);

            if (taiKhoan == null)
            {
                return RedirectToAction("Login");
            }

            return View(taiKhoan);
        }
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            try
            {
                var taiKhoan = _context.TaiKhoan
                    .Include(tk => tk.NguoiDung)
                    .FirstOrDefault(u => u.TenDangNhap == username && u.MatKhau == password);

                if (taiKhoan != null && !taiKhoan.BiChan)
                {
                    HttpContext.Session.SetString("TenDangNhap", taiKhoan.TenDangNhap);
                    HttpContext.Session.SetString("VaiTro", taiKhoan.VaiTro ?? "User");
                    HttpContext.Session.SetString("Email", taiKhoan.Email ?? "");
                    HttpContext.Session.SetInt32("MaTaiKhoan", taiKhoan.MaTaiKhoan);

                    // Lưu MaNguoiDung nếu có
                    if (taiKhoan.NguoiDung != null)
                    {
                        HttpContext.Session.SetInt32("MaNguoiDung", taiKhoan.NguoiDung.MaNguoiDung);
                    }

                    return RedirectToAction("Index", "Books");
                }

                ViewBag.Error = "Tên người dùng hoặc mật khẩu không đúng, hoặc tài khoản đã bị chặn.";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Đăng nhập thất bại cho người dùng {Username}", username);
                ViewBag.Error = "Hệ thống gặp lỗi khi đăng nhập. Vui lòng liên hệ quản trị.";
                ViewBag.ServerException = ex.Message;
                Response.StatusCode = 500;
                return View();
            }
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string username, string password, string email)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                ViewBag.Error = "Tên đăng nhập không được để trống.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Mật khẩu không được để trống.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Email không được để trống.";
                return View();
            }

            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            {
                ViewBag.Error = "Email không hợp lệ.";
                return View();
            }

            if (_context.TaiKhoan.Any(u => u.TenDangNhap == username))
            {
                ViewBag.Error = "Tên người dùng đã tồn tại.";
                return View();
            }

            if (_context.TaiKhoan.Any(u => u.Email == email))
            {
                ViewBag.Error = "Email đã được sử dụng.";
                return View();
            }

            // Tạo tài khoản mới
            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = username,
                MatKhau = password,
                Email = email,
                VaiTro = "User",
                NgayTao = DateTime.Now
            };

            _context.Add(taiKhoan);
            _context.SaveChanges();

            // Tạo thông tin người dùng mặc định (có thể để trống, người dùng sẽ cập nhật sau)
            var nguoiDung = new NguoiDung
            {
                MaTaiKhoan = taiKhoan.MaTaiKhoan,
                NgayTao = DateTime.Now
            };

            _context.Add(nguoiDung);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            var taiKhoan = _context.TaiKhoan.FirstOrDefault(u => u.Email == email);
            if (taiKhoan == null)
            {
                ViewBag.Message = "Không tìm thấy người dùng với email này.";
                return View();
            }

            string token = Guid.NewGuid().ToString();
            HttpContext.Session.SetString("ResetToken", token);
            HttpContext.Session.SetString("ResetEmail", email);

            string resetLink = Url.Action("ResetPassword", "Account", new { token = token }, Request.Scheme);
            ViewBag.Message = $"Đã gửi liên kết đặt lại mật khẩu đến email: {email}<br><a href='{resetLink}'>[Bấm vào đây để test link]</a>";

            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            if (token != HttpContext.Session.GetString("ResetToken"))
                return BadRequest("Token không hợp lệ hoặc đã hết hạn.");

            var model = new ResetPasswordViewModel { Token = token };
            return View(model);
        }

        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (model.Password != model.ConfirmPassword)
            {
                ViewBag.Message = "Mật khẩu xác nhận không khớp.";
                return View(model);
            }

            if (model.Token != HttpContext.Session.GetString("ResetToken"))
                return BadRequest("Token không hợp lệ hoặc đã hết hạn.");

            string email = HttpContext.Session.GetString("ResetEmail");
            var taiKhoan = _context.TaiKhoan.FirstOrDefault(u => u.Email == email);
            if (taiKhoan == null) return NotFound();

            taiKhoan.MatKhau = model.Password;
            _context.SaveChanges();

            HttpContext.Session.Remove("ResetToken");
            HttpContext.Session.Remove("ResetEmail");

            TempData["ResetSuccess"] = "Đổi mật khẩu thành công! Bạn có thể đăng nhập lại.";
            return RedirectToAction("Login");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Books");
        }

        [HttpGet]
        public IActionResult DoiMatKhau()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DoiMatKhau(DoiMatKhau model)
        {
            var maTaiKhoan = HttpContext.Session.GetInt32("MaTaiKhoan");
            if (maTaiKhoan == null)
                return RedirectToAction("Login", "Account");

            var taiKhoan = await _context.TaiKhoan.FindAsync(maTaiKhoan.Value);
            if (taiKhoan == null)
                return RedirectToAction("Login", "Account");

            if (taiKhoan.MatKhau != model.MatKhauCu)
            {
                ViewBag.ThongBao = "Mật khẩu cũ không đúng.";
                return View();
            }

            taiKhoan.MatKhau = model.MatKhauMoi;
            _context.Update(taiKhoan);
            await _context.SaveChangesAsync();

            HttpContext.Session.Clear();
            TempData["ThongBao"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
            return RedirectToAction("Login", "Account");
        }
    }
}
