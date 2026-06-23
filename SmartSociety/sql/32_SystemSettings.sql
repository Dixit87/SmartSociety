USE [SmartSociety];
GO

-- 1. Create SystemSettings Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SystemSettings]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SystemSettings](
        [SettingId] [int] IDENTITY(1,1) NOT NULL,
        [SocietyName] [nvarchar](200) NOT NULL,
        [RegistrationNo] [nvarchar](100) NULL,
        [Address] [nvarchar](500) NULL,
        [ContactEmail] [nvarchar](100) NULL,
        [ContactPhone] [nvarchar](50) NULL,
        [CurrencySymbol] [nvarchar](10) NOT NULL DEFAULT '₹',
        [DefaultPenaltyPercentage] [decimal](5,2) NOT NULL DEFAULT 5.0,
     CONSTRAINT [PK_SystemSettings] PRIMARY KEY CLUSTERED ([SettingId] ASC)
    )

    -- Insert default row if empty
    INSERT INTO SystemSettings (SocietyName, RegistrationNo, Address, ContactEmail, ContactPhone, CurrencySymbol, DefaultPenaltyPercentage)
    VALUES ('Smart Society Heights', 'REG-12345-2023', '123 Main Street, Smart City, SC 400001', 'admin@smartsociety.com', '+91 9876543210', '₹', 5.0);
END
GO

-- 2. Create DatabaseBackups Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DatabaseBackups]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DatabaseBackups](
        [BackupId] [int] IDENTITY(1,1) NOT NULL,
        [FileName] [nvarchar](200) NOT NULL,
        [FilePath] [nvarchar](500) NOT NULL,
        [SizeMB] [decimal](10,2) NOT NULL,
        [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE(),
     CONSTRAINT [PK_DatabaseBackups] PRIMARY KEY CLUSTERED ([BackupId] ASC)
    )
END
GO

-- 3. Stored Procedure: Get Settings
CREATE OR ALTER PROCEDURE sp_Settings_Get
AS
BEGIN
    SELECT TOP 1 * FROM SystemSettings ORDER BY SettingId ASC;
END
GO

-- 4. Stored Procedure: Update Settings
CREATE OR ALTER PROCEDURE sp_Settings_Update
    @SocietyName NVARCHAR(200),
    @RegistrationNo NVARCHAR(100),
    @Address NVARCHAR(500),
    @ContactEmail NVARCHAR(100),
    @ContactPhone NVARCHAR(50),
    @CurrencySymbol NVARCHAR(10),
    @DefaultPenaltyPercentage DECIMAL(5,2)
AS
BEGIN
    UPDATE SystemSettings
    SET 
        SocietyName = @SocietyName,
        RegistrationNo = @RegistrationNo,
        Address = @Address,
        ContactEmail = @ContactEmail,
        ContactPhone = @ContactPhone,
        CurrencySymbol = @CurrencySymbol,
        DefaultPenaltyPercentage = @DefaultPenaltyPercentage
    WHERE SettingId = (SELECT TOP 1 SettingId FROM SystemSettings ORDER BY SettingId ASC);
END
GO

-- 5. Stored Procedure: Insert Backup Record
CREATE OR ALTER PROCEDURE sp_Backups_Insert
    @FileName NVARCHAR(200),
    @FilePath NVARCHAR(500),
    @SizeMB DECIMAL(10,2)
AS
BEGIN
    INSERT INTO DatabaseBackups (FileName, FilePath, SizeMB, CreatedAt)
    VALUES (@FileName, @FilePath, @SizeMB, GETDATE());
END
GO

-- 6. Stored Procedure: Get All Backups
CREATE OR ALTER PROCEDURE sp_Backups_GetAll
AS
BEGIN
    SELECT * FROM DatabaseBackups ORDER BY CreatedAt DESC;
END
GO
