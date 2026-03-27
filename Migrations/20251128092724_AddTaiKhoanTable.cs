using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STOREBOOKS.Migrations
{
    /// <inheritdoc />
    public partial class AddTaiKhoanTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaiKhoan",
                columns: table => new
                {
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDangNhap = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MatKhau = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VaiTro = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BiChan = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoan", x => x.MaTaiKhoan);
                });

            // Chuyển dữ liệu đăng nhập hiện có sang bảng TaiKhoan mới
            migrationBuilder.Sql(@"
                INSERT INTO TaiKhoan (TenDangNhap, MatKhau, Email, VaiTro, BiChan)
                SELECT TenDangNhap, MatKhau, Email, VaiTro, BiChan
                FROM NguoiDung
            ");

            migrationBuilder.AddColumn<int>(
                name: "MaTaiKhoan",
                table: "NguoiDung",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE nd
                SET MaTaiKhoan = tk.MaTaiKhoan
                FROM NguoiDung nd
                INNER JOIN TaiKhoan tk ON nd.TenDangNhap = tk.TenDangNhap
            ");

            migrationBuilder.AlterColumn<int>(
                name: "MaTaiKhoan",
                table: "NguoiDung",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnhDaiDien",
                table: "NguoiDung",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiaChi",
                table: "NguoiDung",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GioiTinh",
                table: "NguoiDung",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HoTen",
                table: "NguoiDung",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoDienThoai",
                table: "NguoiDung",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayCapNhat",
                table: "NguoiDung",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySinh",
                table: "NguoiDung",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTao",
                table: "NguoiDung",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "ChatMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                table: "ChatMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_MaTaiKhoan",
                table: "NguoiDung",
                column: "MaTaiKhoan",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_NguoiDung_TaiKhoan_MaTaiKhoan",
                table: "NguoiDung",
                column: "MaTaiKhoan",
                principalTable: "TaiKhoan",
                principalColumn: "MaTaiKhoan",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropColumn(
                name: "BiChan",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "MatKhau",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "TenDangNhap",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "VaiTro",
                table: "NguoiDung");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BiChan",
                table: "NguoiDung",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "NguoiDung",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MatKhau",
                table: "NguoiDung",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenDangNhap",
                table: "NguoiDung",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VaiTro",
                table: "NguoiDung",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE nd
                SET TenDangNhap = tk.TenDangNhap,
                    MatKhau = tk.MatKhau,
                    Email = tk.Email,
                    VaiTro = tk.VaiTro,
                    BiChan = tk.BiChan
                FROM NguoiDung nd
                INNER JOIN TaiKhoan tk ON nd.MaTaiKhoan = tk.MaTaiKhoan
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_NguoiDung_TaiKhoan_MaTaiKhoan",
                table: "NguoiDung");

            migrationBuilder.DropIndex(
                name: "IX_NguoiDung_MaTaiKhoan",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "AnhDaiDien",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "DiaChi",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "GioiTinh",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "HoTen",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "SoDienThoai",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "NgayCapNhat",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "NgaySinh",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "NgayTao",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "MaTaiKhoan",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "ChatMessages");

            migrationBuilder.DropTable(
                name: "TaiKhoan");
        }
    }
}
