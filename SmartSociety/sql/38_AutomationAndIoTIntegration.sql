-- SQL Migration: Smart RFID Parking Gate & Smart Meter Integration (Smart Society IoT)

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SmartMeters')
BEGIN
    CREATE TABLE SmartMeters (
        MeterId INT IDENTITY(1,1) PRIMARY KEY,
        FlatId INT NOT NULL FOREIGN KEY REFERENCES Flats(FlatId),
        MeterType VARCHAR(50) NOT NULL, -- 'Electricity' or 'Water'
        MeterNumber VARCHAR(100) UNIQUE NOT NULL,
        Balance DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
        CurrentReading DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
        LastSyncTime DATETIME NOT NULL DEFAULT GETDATE(),
        Status VARCHAR(50) NOT NULL DEFAULT 'Active', -- 'Active' or 'Suspended'
        IsActive BIT NOT NULL DEFAULT 1
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SmartMeterLogs')
BEGIN
    CREATE TABLE SmartMeterLogs (
        LogId INT IDENTITY(1,1) PRIMARY KEY,
        MeterId INT NOT NULL FOREIGN KEY REFERENCES SmartMeters(MeterId),
        UnitsConsumed DECIMAL(10, 2) NOT NULL,
        Cost DECIMAL(10, 2) NOT NULL,
        BalanceAfter DECIMAL(10, 2) NOT NULL,
        Timestamp DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SmartMeterRecharges')
BEGIN
    CREATE TABLE SmartMeterRecharges (
        RechargeId INT IDENTITY(1,1) PRIMARY KEY,
        MeterId INT NOT NULL FOREIGN KEY REFERENCES SmartMeters(MeterId),
        Amount DECIMAL(10, 2) NOT NULL,
        PaymentMethod VARCHAR(50) NOT NULL,
        TransactionId VARCHAR(100) NULL,
        RechargeTime DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RfidGateLogs')
BEGIN
    CREATE TABLE RfidGateLogs (
        LogId INT IDENTITY(1,1) PRIMARY KEY,
        SlotId INT NULL FOREIGN KEY REFERENCES ParkingSlots(SlotId) ON DELETE SET NULL,
        RfidTag VARCHAR(100) NOT NULL,
        VehicleNumber VARCHAR(50) NULL,
        Direction VARCHAR(10) NOT NULL, -- 'Entry' or 'Exit'
        GateName VARCHAR(100) NOT NULL,
        Timestamp DATETIME NOT NULL DEFAULT GETDATE(),
        Status VARCHAR(100) NOT NULL -- 'Authorized - Gate Opened', 'Unauthorized - Denied', etc.
    );
END
GO

-- Stored Procedures --

-- 1. Get smart meters by flat ID
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_SmartMeters_GetByFlatId')
    DROP PROCEDURE sp_SmartMeters_GetByFlatId;
GO
CREATE PROCEDURE sp_SmartMeters_GetByFlatId
    @FlatId INT
AS
BEGIN
    SELECT m.*, f.FlatNumber, b.BlockName, o.FullName AS OwnerName,
           COALESCE(t.RatePerUnit, 5.50) AS RatePerUnit, 
           COALESCE(t.MeasurementUnit, 'Unit') AS MeasurementUnit
    FROM SmartMeters m
    INNER JOIN Flats f ON m.FlatId = f.FlatId
    INNER JOIN Blocks b ON f.BlockId = b.BlockId
    LEFT JOIN Users o ON f.OwnerId = o.UserId
    LEFT JOIN UtilityTypes t ON m.MeterType = t.Name
    WHERE m.FlatId = @FlatId AND m.IsActive = 1;
END
GO

-- 2. Get all smart meters
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_SmartMeters_GetAll')
    DROP PROCEDURE sp_SmartMeters_GetAll;
GO
CREATE PROCEDURE sp_SmartMeters_GetAll
AS
BEGIN
    SELECT m.*, f.FlatNumber, b.BlockName, o.FullName AS OwnerName,
           COALESCE(t.RatePerUnit, 5.50) AS RatePerUnit, 
           COALESCE(t.MeasurementUnit, 'Unit') AS MeasurementUnit
    FROM SmartMeters m
    INNER JOIN Flats f ON m.FlatId = f.FlatId
    INNER JOIN Blocks b ON f.BlockId = b.BlockId
    LEFT JOIN Users o ON f.OwnerId = o.UserId
    LEFT JOIN UtilityTypes t ON m.MeterType = t.Name
    WHERE m.IsActive = 1
    ORDER BY b.BlockName, f.FlatNumber;
END
GO

-- 3. Get smart meter by ID
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_SmartMeters_GetById')
    DROP PROCEDURE sp_SmartMeters_GetById;
GO
CREATE PROCEDURE sp_SmartMeters_GetById
    @MeterId INT
AS
BEGIN
    SELECT m.*, f.FlatNumber, b.BlockName, o.FullName AS OwnerName,
           COALESCE(t.RatePerUnit, 5.50) AS RatePerUnit, 
           COALESCE(t.MeasurementUnit, 'Unit') AS MeasurementUnit
    FROM SmartMeters m
    INNER JOIN Flats f ON m.FlatId = f.FlatId
    INNER JOIN Blocks b ON f.BlockId = b.BlockId
    LEFT JOIN Users o ON f.OwnerId = o.UserId
    LEFT JOIN UtilityTypes t ON m.MeterType = t.Name
    WHERE m.MeterId = @MeterId;
END
GO

-- 4. Create or Update smart meter
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_SmartMeters_CreateOrUpdate')
    DROP PROCEDURE sp_SmartMeters_CreateOrUpdate;
GO
CREATE PROCEDURE sp_SmartMeters_CreateOrUpdate
    @MeterId INT,
    @FlatId INT,
    @MeterType VARCHAR(50),
    @MeterNumber VARCHAR(100),
    @Balance DECIMAL(10,2),
    @CurrentReading DECIMAL(10,2),
    @Status VARCHAR(50),
    @IsActive BIT,
    @NewMeterId INT OUTPUT
AS
BEGIN
    IF EXISTS (SELECT 1 FROM SmartMeters WHERE MeterId = @MeterId)
    BEGIN
        UPDATE SmartMeters
        SET FlatId = @FlatId,
            MeterType = @MeterType,
            MeterNumber = @MeterNumber,
            Balance = @Balance,
            CurrentReading = @CurrentReading,
            Status = @Status,
            IsActive = @IsActive
        WHERE MeterId = @MeterId;
        SET @NewMeterId = @MeterId;
    END
    ELSE
    BEGIN
        INSERT INTO SmartMeters (FlatId, MeterType, MeterNumber, Balance, CurrentReading, Status, LastSyncTime, IsActive)
        VALUES (@FlatId, @MeterType, @MeterNumber, @Balance, @CurrentReading, @Status, GETDATE(), @IsActive);
        SET @NewMeterId = SCOPE_IDENTITY();
    END
END
GO

-- 5. Consume Balance
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_SmartMeters_ConsumeBalance')
    DROP PROCEDURE sp_SmartMeters_ConsumeBalance;
GO
CREATE PROCEDURE sp_SmartMeters_ConsumeBalance
    @MeterId INT,
    @UnitsConsumed DECIMAL(10,2),
    @Cost DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @OldBalance DECIMAL(10,2);
        DECLARE @NewBalance DECIMAL(10,2);
        DECLARE @NewReading DECIMAL(10,2);

        SELECT @OldBalance = Balance, @NewReading = CurrentReading
        FROM SmartMeters
        WHERE MeterId = @MeterId;

        SET @NewBalance = @OldBalance - @Cost;
        SET @NewReading = @NewReading + @UnitsConsumed;

        DECLARE @Status VARCHAR(50) = 'Active';
        IF @NewBalance <= 0
        BEGIN
            SET @Status = 'Suspended';
        END

        UPDATE SmartMeters
        SET Balance = @NewBalance,
            CurrentReading = @NewReading,
            Status = @Status,
            LastSyncTime = GETDATE()
        WHERE MeterId = @MeterId;

        INSERT INTO SmartMeterLogs (MeterId, UnitsConsumed, Cost, BalanceAfter, Timestamp)
        VALUES (@MeterId, @UnitsConsumed, @Cost, @NewBalance, GETDATE());

        COMMIT TRANSACTION;
        SELECT @NewBalance AS Balance, @Status AS Status, @NewReading AS CurrentReading;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- 6. Recharge smart meter
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_SmartMeters_Recharge')
    DROP PROCEDURE sp_SmartMeters_Recharge;
GO
CREATE PROCEDURE sp_SmartMeters_Recharge
    @MeterId INT,
    @Amount DECIMAL(10,2),
    @PaymentMethod VARCHAR(50),
    @TransactionId VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @OldBalance DECIMAL(10,2);
        DECLARE @NewBalance DECIMAL(10,2);

        SELECT @OldBalance = Balance FROM SmartMeters WHERE MeterId = @MeterId;
        SET @NewBalance = @OldBalance + @Amount;

        UPDATE SmartMeters
        SET Balance = @NewBalance,
            Status = 'Active'
        WHERE MeterId = @MeterId;

        INSERT INTO SmartMeterRecharges (MeterId, Amount, PaymentMethod, TransactionId, RechargeTime)
        VALUES (@MeterId, @Amount, @PaymentMethod, @TransactionId, GETDATE());

        COMMIT TRANSACTION;
        SELECT @NewBalance AS Balance, 'Active' AS Status;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- 7. Insert RFID gate event log
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_RfidGateLogs_Insert')
    DROP PROCEDURE sp_RfidGateLogs_Insert;
GO
CREATE PROCEDURE sp_RfidGateLogs_Insert
    @RfidTag VARCHAR(100),
    @Direction VARCHAR(10),
    @GateName VARCHAR(100),
    @Status VARCHAR(100)
AS
BEGIN
    DECLARE @SlotId INT = NULL;
    DECLARE @VehicleNumber VARCHAR(50) = NULL;

    -- Look up registered tag in ParkingSlots
    SELECT @SlotId = SlotId, @VehicleNumber = VehicleNumber
    FROM ParkingSlots
    WHERE StickerNumber = @RfidTag AND IsActive = 1;

    INSERT INTO RfidGateLogs (SlotId, RfidTag, VehicleNumber, Direction, GateName, Timestamp, Status)
    VALUES (@SlotId, @RfidTag, @VehicleNumber, @Direction, @GateName, GETDATE(), @Status);

    -- Return the inserted log joined with slot info
    SELECT l.*, s.SlotNumber, f.FlatNumber, b.BlockName, o.FullName AS OwnerName
    FROM RfidGateLogs l
    LEFT JOIN ParkingSlots s ON l.SlotId = s.SlotId
    LEFT JOIN Flats f ON s.FlatId = f.FlatId
    LEFT JOIN Blocks b ON f.BlockId = b.BlockId
    LEFT JOIN Users o ON f.OwnerId = o.UserId
    WHERE l.LogId = SCOPE_IDENTITY();
END
GO

-- 8. Get all Rfid Gate Logs
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_RfidGateLogs_GetAll')
    DROP PROCEDURE sp_RfidGateLogs_GetAll;
GO
CREATE PROCEDURE sp_RfidGateLogs_GetAll
AS
BEGIN
    SELECT l.*, s.SlotNumber, f.FlatNumber, b.BlockName, o.FullName AS OwnerName
    FROM RfidGateLogs l
    LEFT JOIN ParkingSlots s ON l.SlotId = s.SlotId
    LEFT JOIN Flats f ON s.FlatId = f.FlatId
    LEFT JOIN Blocks b ON f.BlockId = b.BlockId
    LEFT JOIN Users o ON f.OwnerId = o.UserId
    ORDER BY l.Timestamp DESC;
END
GO

-- 9. Get Rfid Gate Logs by Flat ID
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_RfidGateLogs_GetByFlatId')
    DROP PROCEDURE sp_RfidGateLogs_GetByFlatId;
GO
CREATE PROCEDURE sp_RfidGateLogs_GetByFlatId
    @FlatId INT
AS
BEGIN
    SELECT l.*, s.SlotNumber, f.FlatNumber, b.BlockName, o.FullName AS OwnerName
    FROM RfidGateLogs l
    INNER JOIN ParkingSlots s ON l.SlotId = s.SlotId
    INNER JOIN Flats f ON s.FlatId = f.FlatId
    INNER JOIN Blocks b ON f.BlockId = b.BlockId
    LEFT JOIN Users o ON f.OwnerId = o.UserId
    WHERE f.FlatId = @FlatId
    ORDER BY l.Timestamp DESC;
END
GO

-- 10. Get Smart Meter Logs
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_SmartMeterLogs_GetByMeterId')
    DROP PROCEDURE sp_SmartMeterLogs_GetByMeterId;
GO
CREATE PROCEDURE sp_SmartMeterLogs_GetByMeterId
    @MeterId INT
AS
BEGIN
    SELECT * FROM SmartMeterLogs WHERE MeterId = @MeterId ORDER BY Timestamp DESC;
END
GO

-- 11. Get Smart Meter Recharges
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_SmartMeterRecharges_GetByMeterId')
    DROP PROCEDURE sp_SmartMeterRecharges_GetByMeterId;
GO
CREATE PROCEDURE sp_SmartMeterRecharges_GetByMeterId
    @MeterId INT
AS
BEGIN
    SELECT * FROM SmartMeterRecharges WHERE MeterId = @MeterId ORDER BY RechargeTime DESC;
END
GO
