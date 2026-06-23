USE [SmartSociety]
GO

-- 1. Create Staff Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Staff' and xtype='U')
BEGIN
    CREATE TABLE [dbo].[Staff](
        [StaffId] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [FullName] [nvarchar](150) NOT NULL,
        [Role] [nvarchar](100) NOT NULL, -- e.g. Guard, Maid, Plumber
        [ContactNumber] [nvarchar](20) NOT NULL,
        [AadharNumber] [nvarchar](20) NULL,
        [PhotoPath] [nvarchar](255) NULL,
        [ShiftStart] [time](7) NULL,
        [ShiftEnd] [time](7) NULL,
        [IsVerified] [bit] NOT NULL DEFAULT 0,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE()
    )
END
GO

-- 2. Stored Procedures

-- Create or Update Staff
CREATE OR ALTER PROCEDURE [dbo].[sp_Staff_CreateOrUpdate]
    @StaffId INT,
    @FullName NVARCHAR(150),
    @Role NVARCHAR(100),
    @ContactNumber NVARCHAR(20),
    @AadharNumber NVARCHAR(20),
    @PhotoPath NVARCHAR(255),
    @ShiftStart TIME,
    @ShiftEnd TIME,
    @IsVerified BIT,
    @IsActive BIT,
    @NewStaffId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @StaffId = 0 OR @StaffId IS NULL
    BEGIN
        INSERT INTO [dbo].[Staff] (FullName, Role, ContactNumber, AadharNumber, PhotoPath, ShiftStart, ShiftEnd, IsVerified, IsActive)
        VALUES (@FullName, @Role, @ContactNumber, @AadharNumber, @PhotoPath, @ShiftStart, @ShiftEnd, @IsVerified, @IsActive);
        
        SET @NewStaffId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE [dbo].[Staff]
        SET FullName = @FullName,
            Role = @Role,
            ContactNumber = @ContactNumber,
            AadharNumber = @AadharNumber,
            PhotoPath = ISNULL(@PhotoPath, PhotoPath), -- Don't overwrite photo if null
            ShiftStart = @ShiftStart,
            ShiftEnd = @ShiftEnd,
            IsVerified = @IsVerified,
            IsActive = @IsActive
        WHERE StaffId = @StaffId;

        SET @NewStaffId = @StaffId;
    END
END
GO

-- Get All Staff
CREATE OR ALTER PROCEDURE [dbo].[sp_Staff_GetAll]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM [dbo].[Staff] ORDER BY IsActive DESC, CreatedAt DESC;
END
GO

-- Get Staff By Id
CREATE OR ALTER PROCEDURE [dbo].[sp_Staff_GetById]
    @StaffId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM [dbo].[Staff] WHERE StaffId = @StaffId;
END
GO

-- Toggle Staff Status (IsActive)
CREATE OR ALTER PROCEDURE [dbo].[sp_Staff_ToggleStatus]
    @StaffId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Staff]
    SET IsActive = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END
    WHERE StaffId = @StaffId;
END
GO
