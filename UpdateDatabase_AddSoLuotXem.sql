-- =============================================
-- Script: Thêm cột SoLuotXem vào bảng Sach
-- Database: STOREBOOKS
-- =============================================

USE STOREBOOKS;
GO

-- Kiểm tra và thêm cột SoLuotXem nếu chưa tồn tại
IF NOT EXISTS (
    SELECT * 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Sach' 
    AND COLUMN_NAME = 'SoLuotXem'
)
BEGIN
    ALTER TABLE [dbo].[Sach]
    ADD [SoLuotXem] INT NOT NULL DEFAULT 0;
    
    PRINT N'✅ Đã thêm cột SoLuotXem vào bảng Sach thành công!';
END
ELSE
BEGIN
    PRINT N'ℹ️ Cột SoLuotXem đã tồn tại trong bảng Sach.';
END
GO

-- Đảm bảo tất cả bản ghi hiện có có giá trị SoLuotXem = 0
UPDATE [dbo].[Sach]
SET [SoLuotXem] = 0
WHERE [SoLuotXem] IS NULL;
GO

PRINT N'✅ Hoàn thành cập nhật database!';
GO

