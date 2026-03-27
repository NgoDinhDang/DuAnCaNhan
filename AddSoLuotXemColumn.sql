-- Thêm cột SoLuotXem vào bảng Sach
-- Chạy script này trong SQL Server Management Studio hoặc công cụ quản lý database

-- Kiểm tra xem cột đã tồn tại chưa
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Sach]') 
    AND name = 'SoLuotXem'
)
BEGIN
    -- Thêm cột SoLuotXem với giá trị mặc định là 0
    ALTER TABLE [dbo].[Sach]
    ADD SoLuotXem INT NOT NULL DEFAULT 0;
    
    PRINT 'Đã thêm cột SoLuotXem vào bảng Sach thành công!';
END
ELSE
BEGIN
    PRINT 'Cột SoLuotXem đã tồn tại trong bảng Sach.';
END

-- Cập nhật giá trị mặc định cho các bản ghi hiện có (nếu cần)
UPDATE [dbo].[Sach]
SET SoLuotXem = 0
WHERE SoLuotXem IS NULL;

PRINT 'Hoàn thành!';

