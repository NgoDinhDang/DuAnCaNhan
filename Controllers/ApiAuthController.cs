using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Data;
using STOREBOOKS.Models;
using STOREBOOKS.Services;
using System;

namespace STOREBOOKS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;
        private readonly ILogger<ApiAuthController> _logger;

        public ApiAuthController(
            ApplicationDbContext context,
            JwtService jwtService,
            ILogger<ApiAuthController> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        /// <summary>
        /// Đăng nhập và nhận JWT token
        /// </summary>
        /// <param name="request">Thông tin đăng nhập (username và password)</param>
        /// <returns>JWT token và thông tin user nếu đăng nhập thành công</returns>
        /// <response code="200">Đăng nhập thành công, trả về token</response>
        /// <response code="400">Thông tin đăng nhập không hợp lệ</response>
        /// <response code="401">Tên đăng nhập hoặc mật khẩu không đúng</response>
        /// <response code="403">Tài khoản bị chặn</response>
        [HttpPost("Login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Tên đăng nhập và mật khẩu không được để trống"
                    });
                }

                var taiKhoan = await _context.TaiKhoan
                    .Include(tk => tk.NguoiDung)
                    .FirstOrDefaultAsync(u => u.TenDangNhap == request.Username && u.MatKhau == request.Password);

                if (taiKhoan == null)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Tên đăng nhập hoặc mật khẩu không đúng"
                    });
                }

                if (taiKhoan.BiChan)
                {
                    return Forbid();
                }

                // Tạo JWT token
                var token = _jwtService.GenerateToken(taiKhoan);

                _logger.LogInformation($"User {taiKhoan.TenDangNhap} đăng nhập thành công qua API");

                return Ok(new
                {
                    success = true,
                    message = "Đăng nhập thành công",
                    token = token,
                    user = new
                    {
                        maTaiKhoan = taiKhoan.MaTaiKhoan,
                        maNguoiDung = taiKhoan.NguoiDung?.MaNguoiDung,
                        tenDangNhap = taiKhoan.TenDangNhap,
                        email = taiKhoan.Email,
                        vaiTro = taiKhoan.VaiTro
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng nhập");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi đăng nhập"
                });
            }
        }

        /// <summary>
        /// Đăng ký tài khoản mới
        /// </summary>
        /// <param name="request">Thông tin đăng ký (username, password, email)</param>
        /// <returns>JWT token và thông tin user mới tạo</returns>
        /// <response code="200">Đăng ký thành công, trả về token</response>
        /// <response code="400">Thông tin đăng ký không hợp lệ</response>
        /// <response code="409">Username hoặc email đã tồn tại</response>
        [HttpPost("Register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Username))
                {
                    return BadRequest(new { success = false, message = "Tên đăng nhập không được để trống" });
                }

                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new { success = false, message = "Mật khẩu không được để trống" });
                }

                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new { success = false, message = "Email không được để trống" });
                }

                if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.Email))
                {
                    return BadRequest(new { success = false, message = "Email không hợp lệ" });
                }

                // Kiểm tra username đã tồn tại
                if (await _context.TaiKhoan.AnyAsync(u => u.TenDangNhap == request.Username))
                {
                    return Conflict(new { success = false, message = "Tên đăng nhập đã tồn tại" });
                }

                // Kiểm tra email đã tồn tại
                if (await _context.TaiKhoan.AnyAsync(u => u.Email == request.Email))
                {
                    return Conflict(new { success = false, message = "Email đã được sử dụng" });
                }

                // Tạo tài khoản mới
                var newTaiKhoan = new TaiKhoan
                {
                    TenDangNhap = request.Username,
                    MatKhau = request.Password,
                    Email = request.Email,
                    VaiTro = "User",
                    BiChan = false,
                    NgayTao = DateTime.Now
                };

                _context.TaiKhoan.Add(newTaiKhoan);
                await _context.SaveChangesAsync();

                // Tạo thông tin người dùng mặc định
                var newNguoiDung = new NguoiDung
                {
                    MaTaiKhoan = newTaiKhoan.MaTaiKhoan,
                    NgayTao = DateTime.Now
                };

                _context.NguoiDung.Add(newNguoiDung);
                await _context.SaveChangesAsync();

                // Load lại với navigation property
                newTaiKhoan = await _context.TaiKhoan
                    .Include(tk => tk.NguoiDung)
                    .FirstOrDefaultAsync(tk => tk.MaTaiKhoan == newTaiKhoan.MaTaiKhoan);

                // Tạo token ngay sau khi đăng ký
                var token = _jwtService.GenerateToken(newTaiKhoan);

                _logger.LogInformation($"User {newTaiKhoan.TenDangNhap} đăng ký thành công qua API");

                return Ok(new
                {
                    success = true,
                    message = "Đăng ký thành công",
                    token = token,
                    user = new
                    {
                        maTaiKhoan = newTaiKhoan.MaTaiKhoan,
                        maNguoiDung = newTaiKhoan.NguoiDung?.MaNguoiDung,
                        tenDangNhap = newTaiKhoan.TenDangNhap,
                        email = newTaiKhoan.Email,
                        vaiTro = newTaiKhoan.VaiTro
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng ký");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi đăng ký"
                });
            }
        }

        /// <summary>
        /// Lấy thông tin user hiện tại từ JWT token
        /// </summary>
        /// <remarks>
        /// Yêu cầu header: Authorization: Bearer {token}
        /// </remarks>
        /// <returns>Thông tin user hiện tại</returns>
        /// <response code="200">Trả về thông tin user</response>
        /// <response code="401">Token không hợp lệ hoặc hết hạn</response>
        /// <response code="404">Không tìm thấy user</response>
        [HttpGet("Me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var taiKhoanIdClaim = User.FindFirst("MaTaiKhoan")?.Value;
                if (string.IsNullOrEmpty(taiKhoanIdClaim) || !int.TryParse(taiKhoanIdClaim, out int taiKhoanId))
                {
                    return Unauthorized(new { success = false, message = "Token không hợp lệ" });
                }

                var taiKhoan = await _context.TaiKhoan
                    .Include(tk => tk.NguoiDung)
                    .FirstOrDefaultAsync(tk => tk.MaTaiKhoan == taiKhoanId);
                
                if (taiKhoan == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy tài khoản" });
                }

                return Ok(new
                {
                    success = true,
                    user = new
                    {
                        maTaiKhoan = taiKhoan.MaTaiKhoan,
                        maNguoiDung = taiKhoan.NguoiDung?.MaNguoiDung,
                        tenDangNhap = taiKhoan.TenDangNhap,
                        email = taiKhoan.Email,
                        vaiTro = taiKhoan.VaiTro,
                        biChan = taiKhoan.BiChan
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin user");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra"
                });
            }
        }

        /// <summary>
        /// Refresh token - Tạo JWT token mới từ token hiện tại
        /// </summary>
        /// <remarks>
        /// Yêu cầu header: Authorization: Bearer {token}
        /// </remarks>
        /// <returns>JWT token mới</returns>
        /// <response code="200">Trả về token mới</response>
        /// <response code="401">Token không hợp lệ hoặc user bị chặn</response>
        [HttpPost("Refresh")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var taiKhoanIdClaim = User.FindFirst("MaTaiKhoan")?.Value;
                if (string.IsNullOrEmpty(taiKhoanIdClaim) || !int.TryParse(taiKhoanIdClaim, out int taiKhoanId))
                {
                    return Unauthorized(new { success = false, message = "Token không hợp lệ" });
                }

                var taiKhoan = await _context.TaiKhoan
                    .Include(tk => tk.NguoiDung)
                    .FirstOrDefaultAsync(tk => tk.MaTaiKhoan == taiKhoanId);
                
                if (taiKhoan == null || taiKhoan.BiChan)
                {
                    return Unauthorized(new { success = false, message = "Tài khoản không hợp lệ hoặc đã bị chặn" });
                }

                // Tạo token mới
                var newToken = _jwtService.GenerateToken(taiKhoan);

                return Ok(new
                {
                    success = true,
                    message = "Token đã được làm mới",
                    token = newToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi refresh token");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra"
                });
            }
        }
    }

    // DTOs
    /// <summary>
    /// Request model cho đăng nhập
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// Tên đăng nhập
        /// </summary>
        public string Username { get; set; } = string.Empty;
        
        /// <summary>
        /// Mật khẩu
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model cho đăng ký
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// Tên đăng nhập (phải duy nhất)
        /// </summary>
        public string Username { get; set; } = string.Empty;
        
        /// <summary>
        /// Mật khẩu
        /// </summary>
        public string Password { get; set; } = string.Empty;
        
        /// <summary>
        /// Email (phải hợp lệ và duy nhất)
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }
}

