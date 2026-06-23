USE [SmartSociety];
GO

-- 1. Alter Vendors Table
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'ContractDocumentPath' AND Object_ID = Object_ID(N'dbo.Vendors'))
BEGIN
    ALTER TABLE dbo.Vendors ADD ContractDocumentPath NVARCHAR(255) NULL;
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'Rating' AND Object_ID = Object_ID(N'dbo.Vendors'))
BEGIN
    ALTER TABLE dbo.Vendors ADD Rating INT NULL;
END
GO

-- 2. Alter Assets Table
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'InvoiceDocumentPath' AND Object_ID = Object_ID(N'dbo.Assets'))
BEGIN
    ALTER TABLE dbo.Assets ADD InvoiceDocumentPath NVARCHAR(255) NULL;
END
GO

-- 3. Create AssetServiceLogs Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AssetServiceLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AssetServiceLogs](
        [LogId] [int] IDENTITY(1,1) NOT NULL,
        [AssetId] [int] NOT NULL,
        [VendorId] [int] NULL,
        [ServiceDate] [date] NOT NULL,
        [Description] [nvarchar](max) NOT NULL,
        [Cost] [decimal](18, 2) NULL,
        [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE(),
     CONSTRAINT [PK_AssetServiceLogs] PRIMARY KEY CLUSTERED ([LogId] ASC),
     CONSTRAINT [FK_AssetServiceLogs_Assets] FOREIGN KEY([AssetId]) REFERENCES [dbo].[Assets] ([AssetId]) ON DELETE CASCADE,
     CONSTRAINT [FK_AssetServiceLogs_Vendors] FOREIGN KEY([VendorId]) REFERENCES [dbo].[Vendors] ([VendorId]) ON DELETE SET NULL
    )
END
GO

-- 4. Update sp_Vendors_GetAll
CREATE OR ALTER PROCEDURE sp_Vendors_GetAll
    @Status NVARCHAR(50) = NULL
AS
BEGIN
    SELECT 
        VendorId, VendorName, ServiceCategory, ContactPerson, PhoneNumber, Email, 
        ContractStartDate, ContractEndDate, ContractCost, Status, CreatedAt,
        ContractDocumentPath, Rating
    FROM Vendors
    WHERE (@Status IS NULL OR Status = @Status)
    ORDER BY ContractEndDate ASC;
END
GO

-- 5. Update sp_Vendors_GetById
CREATE OR ALTER PROCEDURE sp_Vendors_GetById
    @VendorId INT
AS
BEGIN
    SELECT * FROM Vendors WHERE VendorId = @VendorId;
END
GO

-- 6. Update sp_Vendors_Upsert
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
    @Status NVARCHAR(50),
    @ContractDocumentPath NVARCHAR(255) = NULL,
    @Rating INT = NULL
AS
BEGIN
    IF @VendorId = 0
    BEGIN
        INSERT INTO Vendors (VendorName, ServiceCategory, ContactPerson, PhoneNumber, Email, ContractStartDate, ContractEndDate, ContractCost, Status, ContractDocumentPath, Rating)
        VALUES (@VendorName, @ServiceCategory, @ContactPerson, @PhoneNumber, @Email, @ContractStartDate, @ContractEndDate, @ContractCost, @Status, @ContractDocumentPath, @Rating);
        
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
            Status = @Status,
            ContractDocumentPath = CASE WHEN @ContractDocumentPath = 'CLEAR' THEN NULL WHEN @ContractDocumentPath IS NOT NULL THEN @ContractDocumentPath ELSE ContractDocumentPath END,
            Rating = @Rating
        WHERE VendorId = @VendorId;
        
        SELECT @VendorId;
    END
END
GO

-- 7. Update sp_Assets_GetAll
CREATE OR ALTER PROCEDURE sp_Assets_GetAll
    @Status NVARCHAR(50) = NULL
AS
BEGIN
    SELECT 
        a.AssetId, a.AssetName, a.AssetType, a.Location, a.PurchaseDate, a.PurchaseCost, 
        a.VendorId, v.VendorName, a.AmcExpiryDate, a.Status, a.CreatedAt, a.InvoiceDocumentPath
    FROM Assets a
    LEFT JOIN Vendors v ON a.VendorId = v.VendorId
    WHERE (@Status IS NULL OR a.Status = @Status)
    ORDER BY a.AssetName ASC;
END
GO

-- 8. Update sp_Assets_GetById
CREATE OR ALTER PROCEDURE sp_Assets_GetById
    @AssetId INT
AS
BEGIN
    SELECT 
        a.AssetId, a.AssetName, a.AssetType, a.Location, a.PurchaseDate, a.PurchaseCost, 
        a.VendorId, v.VendorName, a.AmcExpiryDate, a.Status, a.CreatedAt, a.InvoiceDocumentPath
    FROM Assets a
    LEFT JOIN Vendors v ON a.VendorId = v.VendorId
    WHERE a.AssetId = @AssetId;
END
GO

-- 9. Update sp_Assets_Upsert
CREATE OR ALTER PROCEDURE sp_Assets_Upsert
    @AssetId INT = 0,
    @AssetName NVARCHAR(150),
    @AssetType NVARCHAR(100),
    @Location NVARCHAR(150),
    @PurchaseDate DATE,
    @PurchaseCost DECIMAL(18,2) = NULL,
    @VendorId INT = NULL,
    @AmcExpiryDate DATE = NULL,
    @Status NVARCHAR(50),
    @InvoiceDocumentPath NVARCHAR(255) = NULL
AS
BEGIN
    IF @VendorId = 0 SET @VendorId = NULL;

    IF @AssetId = 0
    BEGIN
        INSERT INTO Assets (AssetName, AssetType, Location, PurchaseDate, PurchaseCost, VendorId, AmcExpiryDate, Status, InvoiceDocumentPath)
        VALUES (@AssetName, @AssetType, @Location, @PurchaseDate, @PurchaseCost, @VendorId, @AmcExpiryDate, @Status, @InvoiceDocumentPath);
        
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
            Status = @Status,
            InvoiceDocumentPath = CASE WHEN @InvoiceDocumentPath = 'CLEAR' THEN NULL WHEN @InvoiceDocumentPath IS NOT NULL THEN @InvoiceDocumentPath ELSE InvoiceDocumentPath END
        WHERE AssetId = @AssetId;
        
        SELECT @AssetId;
    END
END
GO

-- 10. Service Logs Stored Procedures
CREATE OR ALTER PROCEDURE sp_AssetServiceLogs_GetByAssetId
    @AssetId INT
AS
BEGIN
    SELECT 
        s.LogId, s.AssetId, s.VendorId, v.VendorName, s.ServiceDate, s.Description, s.Cost, s.CreatedAt
    FROM AssetServiceLogs s
    LEFT JOIN Vendors v ON s.VendorId = v.VendorId
    WHERE s.AssetId = @AssetId
    ORDER BY s.ServiceDate DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_AssetServiceLogs_Upsert
    @LogId INT = 0,
    @AssetId INT,
    @VendorId INT = NULL,
    @ServiceDate DATE,
    @Description NVARCHAR(MAX),
    @Cost DECIMAL(18,2) = NULL
AS
BEGIN
    IF @VendorId = 0 SET @VendorId = NULL;

    IF @LogId = 0
    BEGIN
        INSERT INTO AssetServiceLogs (AssetId, VendorId, ServiceDate, Description, Cost)
        VALUES (@AssetId, @VendorId, @ServiceDate, @Description, @Cost);
        
        SELECT SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE AssetServiceLogs SET 
            AssetId = @AssetId,
            VendorId = @VendorId,
            ServiceDate = @ServiceDate,
            Description = @Description,
            Cost = @Cost
        WHERE LogId = @LogId;
        
        SELECT @LogId;
    END
END
GO

CREATE OR ALTER PROCEDURE sp_AssetServiceLogs_Delete
    @LogId INT
AS
BEGIN
    DELETE FROM AssetServiceLogs WHERE LogId = @LogId;
END
GO
