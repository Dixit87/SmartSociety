-- SQL Migration: Maintenance Scheduling & Inventory Store

USE [SmartSociety];
GO

-- 1. Create MaintenanceSchedules Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MaintenanceSchedules]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MaintenanceSchedules](
        [ScheduleId] INT IDENTITY(1,1) PRIMARY KEY,
        [AssetId] INT NOT NULL,
        [TaskName] NVARCHAR(255) NOT NULL,
        [FrequencyMonths] INT NOT NULL,
        [LastServiceDate] DATE NULL,
        [NextDueDate] DATE NOT NULL,
        [Notes] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_MaintenanceSchedules_Assets] FOREIGN KEY([AssetId]) REFERENCES [dbo].[Assets] ([AssetId]) ON DELETE CASCADE
    );
END
GO

-- 2. Create InventoryItems Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InventoryItems]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[InventoryItems](
        [ItemId] INT IDENTITY(1,1) PRIMARY KEY,
        [ItemName] NVARCHAR(150) NOT NULL UNIQUE,
        [Quantity] INT NOT NULL DEFAULT 0,
        [Unit] NVARCHAR(50) NOT NULL DEFAULT 'pcs',
        [CostPerUnit] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        [MinStockLevel] INT NOT NULL DEFAULT 5,
        [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 3. Create ComplaintSpareParts Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ComplaintSpareParts]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ComplaintSpareParts](
        [ComplaintId] INT NOT NULL,
        [ItemId] INT NOT NULL,
        [QuantityUsed] INT NOT NULL,
        [CostPerUnit] DECIMAL(18,2) NOT NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_ComplaintSpareParts] PRIMARY KEY CLUSTERED ([ComplaintId] ASC, [ItemId] ASC),
        CONSTRAINT [FK_ComplaintSpareParts_Complaints] FOREIGN KEY([ComplaintId]) REFERENCES [dbo].[Complaints] ([ComplaintId]) ON DELETE CASCADE,
        CONSTRAINT [FK_ComplaintSpareParts_InventoryItems] FOREIGN KEY([ItemId]) REFERENCES [dbo].[InventoryItems] ([ItemId]) ON DELETE CASCADE
    );
END
GO

-- --- Stored Procedures for MaintenanceSchedules ---

CREATE OR ALTER PROCEDURE sp_MaintenanceSchedules_GetAll
AS
BEGIN
    SELECT 
        s.ScheduleId, s.AssetId, a.AssetName, s.TaskName, s.FrequencyMonths, 
        s.LastServiceDate, s.NextDueDate, s.Notes, s.IsActive, s.CreatedAt
    FROM MaintenanceSchedules s
    INNER JOIN Assets a ON s.AssetId = a.AssetId
    ORDER BY s.NextDueDate ASC;
END
GO

CREATE OR ALTER PROCEDURE sp_MaintenanceSchedules_GetById
    @ScheduleId INT
AS
BEGIN
    SELECT 
        s.ScheduleId, s.AssetId, a.AssetName, s.TaskName, s.FrequencyMonths, 
        s.LastServiceDate, s.NextDueDate, s.Notes, s.IsActive, s.CreatedAt
    FROM MaintenanceSchedules s
    INNER JOIN Assets a ON s.AssetId = a.AssetId
    WHERE s.ScheduleId = @ScheduleId;
END
GO

CREATE OR ALTER PROCEDURE sp_MaintenanceSchedules_Upsert
    @ScheduleId INT = 0,
    @AssetId INT,
    @TaskName NVARCHAR(255),
    @FrequencyMonths INT,
    @LastServiceDate DATE = NULL,
    @NextDueDate DATE,
    @Notes NVARCHAR(MAX) = NULL,
    @IsActive BIT
AS
BEGIN
    IF @ScheduleId = 0
    BEGIN
        INSERT INTO MaintenanceSchedules (AssetId, TaskName, FrequencyMonths, LastServiceDate, NextDueDate, Notes, IsActive)
        VALUES (@AssetId, @TaskName, @FrequencyMonths, @LastServiceDate, @NextDueDate, @Notes, @IsActive);
        SELECT SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE MaintenanceSchedules
        SET AssetId = @AssetId,
            TaskName = @TaskName,
            FrequencyMonths = @FrequencyMonths,
            LastServiceDate = @LastServiceDate,
            NextDueDate = @NextDueDate,
            Notes = @Notes,
            IsActive = @IsActive
        WHERE ScheduleId = @ScheduleId;
        SELECT @ScheduleId;
    END
END
GO

CREATE OR ALTER PROCEDURE sp_MaintenanceSchedules_Delete
    @ScheduleId INT
AS
BEGIN
    DELETE FROM MaintenanceSchedules WHERE ScheduleId = @ScheduleId;
END
GO

-- Process due maintenance schedules
CREATE OR ALTER PROCEDURE sp_MaintenanceSchedules_ProcessDue
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ScheduleId INT, @AssetId INT, @TaskName NVARCHAR(255), @FrequencyMonths INT, @NextDueDate DATE, @VendorId INT;
    DECLARE @JobCount INT = 0;

    DECLARE cur CURSOR FOR 
    SELECT ScheduleId, AssetId, TaskName, FrequencyMonths, NextDueDate
    FROM MaintenanceSchedules
    WHERE IsActive = 1 AND NextDueDate <= CAST(GETDATE() AS DATE);

    OPEN cur;
    FETCH NEXT FROM cur INTO @ScheduleId, @AssetId, @TaskName, @FrequencyMonths, @NextDueDate;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Get default VendorId from Asset if exists
        SELECT @VendorId = VendorId FROM Assets WHERE AssetId = @AssetId;

        -- Insert automatic service log with status/notes
        INSERT INTO AssetServiceLogs (AssetId, VendorId, ServiceDate, Description, Cost)
        VALUES (@AssetId, @VendorId, GETDATE(), 'Automatic Scheduled Service: ' + @TaskName, 0.00);

        -- Update Schedule dates (last service set to today, next due pushed forward)
        UPDATE MaintenanceSchedules
        SET LastServiceDate = CAST(GETDATE() AS DATE),
            NextDueDate = DATEADD(month, @FrequencyMonths, CAST(GETDATE() AS DATE))
        WHERE ScheduleId = @ScheduleId;

        SET @JobCount = @JobCount + 1;

        FETCH NEXT FROM cur INTO @ScheduleId, @AssetId, @TaskName, @FrequencyMonths, @NextDueDate;
    END

    CLOSE cur;
    DEALLOCATE cur;

    SELECT @JobCount AS ProcessedCount;
END
GO

-- --- Stored Procedures for Inventory ---

CREATE OR ALTER PROCEDURE sp_InventoryItems_GetAll
AS
BEGIN
    SELECT ItemId, ItemName, Quantity, Unit, CostPerUnit, MinStockLevel, UpdatedAt
    FROM InventoryItems
    ORDER BY ItemName ASC;
END
GO

CREATE OR ALTER PROCEDURE sp_InventoryItems_GetById
    @ItemId INT
AS
BEGIN
    SELECT ItemId, ItemName, Quantity, Unit, CostPerUnit, MinStockLevel, UpdatedAt
    FROM InventoryItems
    WHERE ItemId = @ItemId;
END
GO

CREATE OR ALTER PROCEDURE sp_InventoryItems_Upsert
    @ItemId INT = 0,
    @ItemName NVARCHAR(150),
    @Quantity INT,
    @Unit NVARCHAR(50),
    @CostPerUnit DECIMAL(18,2),
    @MinStockLevel INT
AS
BEGIN
    IF @ItemId = 0
    BEGIN
        INSERT INTO InventoryItems (ItemName, Quantity, Unit, CostPerUnit, MinStockLevel, UpdatedAt)
        VALUES (@ItemName, @Quantity, @Unit, @CostPerUnit, @MinStockLevel, GETDATE());
        SELECT SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE InventoryItems
        SET ItemName = @ItemName,
            Quantity = @Quantity,
            Unit = @Unit,
            CostPerUnit = @CostPerUnit,
            MinStockLevel = @MinStockLevel,
            UpdatedAt = GETDATE()
        WHERE ItemId = @ItemId;
        SELECT @ItemId;
    END
END
GO

CREATE OR ALTER PROCEDURE sp_InventoryItems_Delete
    @ItemId INT
AS
BEGIN
    DELETE FROM InventoryItems WHERE ItemId = @ItemId;
END
GO

CREATE OR ALTER PROCEDURE sp_InventoryItems_DeductStock
    @ItemId INT,
    @QuantityUsed INT
AS
BEGIN
    UPDATE InventoryItems
    SET Quantity = CASE WHEN Quantity - @QuantityUsed < 0 THEN 0 ELSE Quantity - @QuantityUsed END,
        UpdatedAt = GETDATE()
    WHERE ItemId = @ItemId;
END
GO

-- --- Stored Procedures for Complaint Spare Parts ---

CREATE OR ALTER PROCEDURE sp_ComplaintSpareParts_GetByComplaintId
    @ComplaintId INT
AS
BEGIN
    SELECT 
        csp.ComplaintId, csp.ItemId, ii.ItemName, csp.QuantityUsed, csp.CostPerUnit, csp.CreatedAt
    FROM ComplaintSpareParts csp
    INNER JOIN InventoryItems ii ON csp.ItemId = ii.ItemId
    WHERE csp.ComplaintId = @ComplaintId;
END
GO

CREATE OR ALTER PROCEDURE sp_ComplaintSpareParts_Create
    @ComplaintId INT,
    @ItemId INT,
    @QuantityUsed INT,
    @CostPerUnit DECIMAL(18,2)
AS
BEGIN
    -- Delete existing composite primary key if matching to support updates, then insert
    DELETE FROM ComplaintSpareParts WHERE ComplaintId = @ComplaintId AND ItemId = @ItemId;

    INSERT INTO ComplaintSpareParts (ComplaintId, ItemId, QuantityUsed, CostPerUnit)
    VALUES (@ComplaintId, @ItemId, @QuantityUsed, @CostPerUnit);
END
GO

-- --- Seed Initial Data ---

-- Seed Inventory Items
IF NOT EXISTS (SELECT 1 FROM InventoryItems)
BEGIN
    INSERT INTO InventoryItems (ItemName, Quantity, Unit, CostPerUnit, MinStockLevel)
    VALUES 
        ('LED Bulb 15W', 25, 'pcs', 120.00, 5),
        ('PVC Pipe 1 inch (3m)', 12, 'pcs', 180.00, 3),
        ('Water Gate Valve 1 inch', 4, 'pcs', 350.00, 2),
        ('Electric Switch 6A', 40, 'pcs', 45.00, 10),
        ('Teflon Thread Seal Tape', 15, 'rolls', 20.00, 5),
        ('MCB Double Pole 32A', 6, 'pcs', 280.00, 2),
        ('Flexible Waste Pipe (Kitchen)', 8, 'pcs', 75.00, 3);
END
GO

-- Seed Maintenance Schedules (Link to existing Lift and Generator assets if any)
-- Check if assets exist, and seed a test schedule
DECLARE @LiftId INT = NULL;
DECLARE @GenId INT = NULL;

SELECT TOP 1 @LiftId = AssetId FROM Assets WHERE AssetType LIKE '%Lift%' OR AssetType LIKE '%Elevator%';
SELECT TOP 1 @GenId = AssetId FROM Assets WHERE AssetType LIKE '%Generator%';

IF @LiftId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM MaintenanceSchedules WHERE AssetId = @LiftId)
    BEGIN
        INSERT INTO MaintenanceSchedules (AssetId, TaskName, FrequencyMonths, NextDueDate, Notes)
        VALUES (@LiftId, 'Quarterly Lift Elevator Service', 3, DATEADD(day, -1, GETDATE()), 'Verify steel rope tension and brake pads.');
    END
END

IF @GenId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM MaintenanceSchedules WHERE AssetId = @GenId)
    BEGIN
        INSERT INTO MaintenanceSchedules (AssetId, TaskName, FrequencyMonths, NextDueDate, Notes)
        VALUES (@GenId, 'Generator Oil & Filter Change', 6, DATEADD(month, 3, GETDATE()), 'Mobil Delvac oil and standard fuel filter replacements.');
    END
END
GO
