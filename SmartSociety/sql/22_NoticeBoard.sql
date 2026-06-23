-- 22_NoticeBoard.sql

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Notices]') AND type in (N'U'))
BEGIN
CREATE TABLE Notices (
    NoticeId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    Category NVARCHAR(50) NOT NULL,
    TargetAudience NVARCHAR(100) NOT NULL,
    AttachmentPath NVARCHAR(500) NULL,
    IsPinned BIT NOT NULL DEFAULT 0,
    ValidFrom DATE NOT NULL,
    ValidTill DATE NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Active',
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);
END
GO

-- Stored Procedure: GetAll
CREATE OR ALTER PROCEDURE sp_Notices_GetAll
    @Status NVARCHAR(50) = NULL,
    @Category NVARCHAR(50) = NULL
AS
BEGIN
    -- Also auto-expire notices if ValidTill is passed
    UPDATE Notices 
    SET Status = 'Expired' 
    WHERE ValidTill IS NOT NULL AND ValidTill < CAST(GETDATE() AS DATE) AND Status = 'Active';

    SELECT * FROM Notices
    WHERE (@Status IS NULL OR Status = @Status)
      AND (@Category IS NULL OR Category = @Category)
      AND IsActive = 1
    ORDER BY IsPinned DESC, ValidFrom DESC, CreatedAt DESC;
END
GO

-- Stored Procedure: GetById
CREATE OR ALTER PROCEDURE sp_Notices_GetById
    @NoticeId INT
AS
BEGIN
    SELECT * FROM Notices WHERE NoticeId = @NoticeId;
END
GO

-- Stored Procedure: Upsert
CREATE OR ALTER PROCEDURE sp_Notices_Upsert
    @NoticeId INT,
    @Title NVARCHAR(200),
    @Description NVARCHAR(MAX),
    @Category NVARCHAR(50),
    @TargetAudience NVARCHAR(100),
    @AttachmentPath NVARCHAR(500),
    @IsPinned BIT,
    @ValidFrom DATE,
    @ValidTill DATE,
    @Status NVARCHAR(50),
    @CreatedBy INT
AS
BEGIN
    IF @NoticeId = 0
    BEGIN
        INSERT INTO Notices (Title, Description, Category, TargetAudience, AttachmentPath, IsPinned, ValidFrom, ValidTill, Status, IsActive, CreatedBy, CreatedAt)
        VALUES (@Title, @Description, @Category, @TargetAudience, @AttachmentPath, @IsPinned, @ValidFrom, @ValidTill, @Status, 1, @CreatedBy, GETDATE());
        
        SELECT SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE Notices
        SET Title = @Title,
            Description = @Description,
            Category = @Category,
            TargetAudience = @TargetAudience,
            AttachmentPath = CASE WHEN @AttachmentPath = 'CLEAR' THEN NULL 
                                  WHEN @AttachmentPath IS NOT NULL THEN @AttachmentPath 
                                  ELSE AttachmentPath END,
            IsPinned = @IsPinned,
            ValidFrom = @ValidFrom,
            ValidTill = @ValidTill,
            Status = @Status
        WHERE NoticeId = @NoticeId;

        SELECT @NoticeId;
    END
END
GO

-- Stored Procedure: Delete
CREATE OR ALTER PROCEDURE sp_Notices_Delete
    @NoticeId INT
AS
BEGIN
    UPDATE Notices SET IsActive = 0 WHERE NoticeId = @NoticeId;
END
GO

-- Stored Procedure: TogglePin
CREATE OR ALTER PROCEDURE sp_Notices_TogglePin
    @NoticeId INT,
    @IsPinned BIT
AS
BEGIN
    UPDATE Notices SET IsPinned = @IsPinned WHERE NoticeId = @NoticeId;
END
GO
