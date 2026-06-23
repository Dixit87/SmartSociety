USE [SmartSociety];
GO

-- 1. Vendors Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Vendors]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Vendors](
        [VendorId] [int] IDENTITY(1,1) NOT NULL,
        [VendorName] [nvarchar](150) NOT NULL,
        [ServiceCategory] [nvarchar](100) NOT NULL,
        [ContactPerson] [nvarchar](100) NOT NULL,
        [PhoneNumber] [nvarchar](20) NOT NULL,
        [Email] [nvarchar](100) NULL,
        [ContractStartDate] [date] NOT NULL,
        [ContractEndDate] [date] NOT NULL,
        [ContractCost] [decimal](18, 2) NULL,
        [Status] [nvarchar](50) NOT NULL DEFAULT 'Active',
        [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE(),
     CONSTRAINT [PK_Vendors] PRIMARY KEY CLUSTERED ([VendorId] ASC)
    )
END
GO

-- 2. Assets Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Assets]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Assets](
        [AssetId] [int] IDENTITY(1,1) NOT NULL,
        [AssetName] [nvarchar](150) NOT NULL,
        [AssetType] [nvarchar](100) NOT NULL,
        [Location] [nvarchar](150) NOT NULL,
        [PurchaseDate] [date] NOT NULL,
        [PurchaseCost] [decimal](18, 2) NULL,
        [VendorId] [int] NULL, -- Reference to AMC Vendor
        [AmcExpiryDate] [date] NULL,
        [Status] [nvarchar](50) NOT NULL DEFAULT 'Active', -- Active, Maintenance, Decommissioned
        [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE(),
     CONSTRAINT [PK_Assets] PRIMARY KEY CLUSTERED ([AssetId] ASC),
     CONSTRAINT [FK_Assets_Vendors] FOREIGN KEY([VendorId]) REFERENCES [dbo].[Vendors] ([VendorId]) ON DELETE SET NULL
    )
END
GO

-- 3. sp_Vendors_GetAll
CREATE OR ALTER PROCEDURE sp_Vendors_GetAll
    @Status NVARCHAR(50) = NULL
AS
BEGIN
    SELECT 
        VendorId, VendorName, ServiceCategory, ContactPerson, PhoneNumber, Email, 
        ContractStartDate, ContractEndDate, ContractCost, Status, CreatedAt
    FROM Vendors
    WHERE (@Status IS NULL OR Status = @Status)
    ORDER BY ContractEndDate ASC;
END
GO

-- 4. sp_Vendors_GetById
CREATE OR ALTER PROCEDURE sp_Vendors_GetById
    @VendorId INT
AS
BEGIN
    SELECT * FROM Vendors WHERE VendorId = @VendorId;
END
GO

-- 5. sp_Vendors_Upsert
CREATE OR ALTER PROCEDURE sp_Vendors_Upsert
    @VendorId INT = 0,
    @VendorName NVARCHAR(150),
    @ServiceCategory NVARCHAR(100),
    @ContactPerson NVARCHAR(100),
    @PhoneNumber NVARCHAR(20),
    @Email NVARCHAR(100) = NULL,
    @ContractStartDate DATE,
    @ContractEndDate DATE,
    @ContractCost DECIMAL(18,2) = NULL,
    @Status NVARCHAR(50)
AS
BEGIN
    IF @VendorId = 0
    BEGIN
        INSERT INTO Vendors (VendorName, ServiceCategory, ContactPerson, PhoneNumber, Email, ContractStartDate, ContractEndDate, ContractCost, Status)
        VALUES (@VendorName, @ServiceCategory, @ContactPerson, @PhoneNumber, @Email, @ContractStartDate, @ContractEndDate, @ContractCost, @Status);
        
        SELECT SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE Vendors SET 
            VendorName = @VendorName,
            ServiceCategory = @ServiceCategory,
            ContactPerson = @ContactPerson,
            PhoneNumber = @PhoneNumber,
            Email = @Email,
            ContractStartDate = @ContractStartDate,
            ContractEndDate = @ContractEndDate,
            ContractCost = @ContractCost,
            Status = @Status
        WHERE VendorId = @VendorId;
        
        SELECT @VendorId;
    END
END
GO

-- 6. sp_Vendors_Delete
CREATE OR ALTER PROCEDURE sp_Vendors_Delete
    @VendorId INT
AS
BEGIN
    -- Setting related assets AMC to NULL before deleting vendor is handled by FK ON DELETE SET NULL
    DELETE FROM Vendors WHERE VendorId = @VendorId;
END
GO

-- 7. sp_Assets_GetAll
CREATE OR ALTER PROCEDURE sp_Assets_GetAll
    @Status NVARCHAR(50) = NULL
AS
BEGIN
    SELECT 
        a.AssetId, a.AssetName, a.AssetType, a.Location, a.PurchaseDate, a.PurchaseCost, 
        a.VendorId, v.VendorName, a.AmcExpiryDate, a.Status, a.CreatedAt
    FROM Assets a
    LEFT JOIN Vendors v ON a.VendorId = v.VendorId
    WHERE (@Status IS NULL OR a.Status = @Status)
    ORDER BY a.AssetName ASC;
END
GO

-- 8. sp_Assets_GetById
CREATE OR ALTER PROCEDURE sp_Assets_GetById
    @AssetId INT
AS
BEGIN
    SELECT 
        a.AssetId, a.AssetName, a.AssetType, a.Location, a.PurchaseDate, a.PurchaseCost, 
        a.VendorId, v.VendorName, a.AmcExpiryDate, a.Status, a.CreatedAt
    FROM Assets a
    LEFT JOIN Vendors v ON a.VendorId = v.VendorId
    WHERE a.AssetId = @AssetId;
END
GO

-- 9. sp_Assets_Upsert
CREATE OR ALTER PROCEDURE sp_Assets_Upsert
    @AssetId INT = 0,
    @AssetName NVARCHAR(150),
    @AssetType NVARCHAR(100),
    @Location NVARCHAR(150),
    @PurchaseDate DATE,
    @PurchaseCost DECIMAL(18,2) = NULL,
    @VendorId INT = NULL,
    @AmcExpiryDate DATE = NULL,
    @Status NVARCHAR(50)
AS
BEGIN
    IF @VendorId = 0 SET @VendorId = NULL;

    IF @AssetId = 0
    BEGIN
        INSERT INTO Assets (AssetName, AssetType, Location, PurchaseDate, PurchaseCost, VendorId, AmcExpiryDate, Status)
        VALUES (@AssetName, @AssetType, @Location, @PurchaseDate, @PurchaseCost, @VendorId, @AmcExpiryDate, @Status);
        
        SELECT SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE Assets SET 
            AssetName = @AssetName,
            AssetType = @AssetType,
            Location = @Location,
            PurchaseDate = @PurchaseDate,
            PurchaseCost = @PurchaseCost,
            VendorId = @VendorId,
            AmcExpiryDate = @AmcExpiryDate,
            Status = @Status
        WHERE AssetId = @AssetId;
        
        SELECT @AssetId;
    END
END
GO

-- 10. sp_Assets_Delete
CREATE OR ALTER PROCEDURE sp_Assets_Delete
    @AssetId INT
AS
BEGIN
    DELETE FROM Assets WHERE AssetId = @AssetId;
END
GO
