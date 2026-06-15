USE [SmartSociety]
GO

-- 1. Create UtilityTypes Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UtilityTypes]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UtilityTypes](
        [UtilityTypeId] INT IDENTITY(1,1) PRIMARY KEY,
        [Name] NVARCHAR(100) NOT NULL, -- e.g. 'Piped Gas', 'Backup Electricity'
        [RatePerUnit] DECIMAL(18,2) NOT NULL,
        [MeasurementUnit] NVARCHAR(50) NOT NULL, -- e.g. 'm³', 'kWh', 'Ltr'
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME DEFAULT GETDATE()
    )
END
GO

-- 2. Create UtilityBills Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UtilityBills]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UtilityBills](
        [BillId] INT IDENTITY(1,1) PRIMARY KEY,
        [FlatId] INT NOT NULL,
        [UtilityTypeId] INT NOT NULL,
        [BillMonth] INT NOT NULL,
        [BillYear] INT NOT NULL,
        [PreviousReading] DECIMAL(18,2) NOT NULL,
        [CurrentReading] DECIMAL(18,2) NOT NULL,
        [ConsumedUnits] DECIMAL(18,2) NOT NULL,
        [TotalAmount] DECIMAL(18,2) NOT NULL,
        [AmountPaid] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [DueDate] DATETIME NOT NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Unpaid', -- 'Unpaid', 'Partial', 'Paid'
        [CreatedAt] DATETIME DEFAULT GETDATE(),
        FOREIGN KEY (FlatId) REFERENCES Flats(FlatId),
        FOREIGN KEY (UtilityTypeId) REFERENCES UtilityTypes(UtilityTypeId)
    )
END
GO

-- 3. Create UtilityPayments Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UtilityPayments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UtilityPayments](
        [PaymentId] INT IDENTITY(1,1) PRIMARY KEY,
        [BillId] INT NOT NULL,
        [PaidAmount] DECIMAL(18,2) NOT NULL,
        [PaymentDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [PaymentMode] NVARCHAR(50) NOT NULL, -- Cash, Cheque, UPI, BankTransfer
        [TransactionId] NVARCHAR(100) NULL,
        [Remarks] NVARCHAR(255) NULL,
        [CreatedAt] DATETIME DEFAULT GETDATE(),
        FOREIGN KEY (BillId) REFERENCES UtilityBills(BillId)
    )
END
GO

-- 4. Stored Procedure: Save Utility Type
CREATE OR ALTER PROCEDURE sp_Utility_SaveType
    @UtilityTypeId INT,
    @Name NVARCHAR(100),
    @RatePerUnit DECIMAL(18,2),
    @MeasurementUnit NVARCHAR(50),
    @IsActive BIT
AS
BEGIN
    IF @UtilityTypeId = 0
    BEGIN
        INSERT INTO UtilityTypes (Name, RatePerUnit, MeasurementUnit, IsActive)
        VALUES (@Name, @RatePerUnit, @MeasurementUnit, @IsActive);
    END
    ELSE
    BEGIN
        UPDATE UtilityTypes
        SET Name = @Name,
            RatePerUnit = @RatePerUnit,
            MeasurementUnit = @MeasurementUnit,
            IsActive = @IsActive
        WHERE UtilityTypeId = @UtilityTypeId;
    END
END
GO

-- 5. Stored Procedure: Get Utility Types
CREATE OR ALTER PROCEDURE sp_Utility_GetTypes
AS
BEGIN
    SELECT * FROM UtilityTypes ORDER BY Name ASC;
END
GO

-- 6. Stored Procedure: Delete Utility Type
CREATE OR ALTER PROCEDURE sp_Utility_DeleteType
    @UtilityTypeId INT
AS
BEGIN
    -- Only allow if no bills exist for this type
    IF EXISTS (SELECT 1 FROM UtilityBills WHERE UtilityTypeId = @UtilityTypeId)
    BEGIN
        RAISERROR('Cannot delete because bills exist for this utility type.', 16, 1);
        RETURN;
    END
    DELETE FROM UtilityTypes WHERE UtilityTypeId = @UtilityTypeId;
END
GO

-- 7. Stored Procedure: Record New Reading & Generate Bill
CREATE OR ALTER PROCEDURE sp_Utility_RecordReading
    @FlatId INT,
    @UtilityTypeId INT,
    @BillMonth INT,
    @BillYear INT,
    @CurrentReading DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Check if a bill already exists for this flat, utility type, month, year
    IF EXISTS (SELECT 1 FROM UtilityBills WHERE FlatId = @FlatId AND UtilityTypeId = @UtilityTypeId AND BillMonth = @BillMonth AND BillYear = @BillYear)
    BEGIN
        RAISERROR('A bill for this utility already exists for the selected month and year.', 16, 1);
        RETURN;
    END

    -- 2. Fetch the Previous Reading (from the most recent bill for this flat and utility)
    DECLARE @PreviousReading DECIMAL(18,2) = 0;
    SELECT TOP 1 @PreviousReading = CurrentReading 
    FROM UtilityBills 
    WHERE FlatId = @FlatId AND UtilityTypeId = @UtilityTypeId 
    ORDER BY BillYear DESC, BillMonth DESC;

    -- 3. Calculate Consumption
    IF @CurrentReading < @PreviousReading
    BEGIN
        -- Handle meter reset or overflow scenario (simplified for V1: fail)
        RAISERROR('Current reading cannot be less than previous reading.', 16, 1);
        RETURN;
    END

    DECLARE @ConsumedUnits DECIMAL(18,2) = @CurrentReading - @PreviousReading;

    -- 4. Get Rate
    DECLARE @RatePerUnit DECIMAL(18,2) = 0;
    SELECT @RatePerUnit = RatePerUnit FROM UtilityTypes WHERE UtilityTypeId = @UtilityTypeId;

    DECLARE @TotalAmount DECIMAL(18,2) = @ConsumedUnits * @RatePerUnit;
    
    -- 5. Calculate Due Date (Let's say 15 days from now)
    DECLARE @DueDate DATETIME = DATEADD(day, 15, GETDATE());

    -- 6. Insert Bill
    INSERT INTO UtilityBills (FlatId, UtilityTypeId, BillMonth, BillYear, PreviousReading, CurrentReading, ConsumedUnits, TotalAmount, DueDate, Status)
    VALUES (@FlatId, @UtilityTypeId, @BillMonth, @BillYear, @PreviousReading, @CurrentReading, @ConsumedUnits, @TotalAmount, @DueDate, 'Unpaid');
END
GO

-- 8. Stored Procedure: Get Utility Bills
CREATE OR ALTER PROCEDURE sp_Utility_GetBills
    @Month INT = NULL,
    @Year INT = NULL
AS
BEGIN
    SELECT 
        B.BillId, B.FlatId, B.UtilityTypeId, B.BillMonth, B.BillYear, 
        B.PreviousReading, B.CurrentReading, B.ConsumedUnits, 
        B.TotalAmount, B.AmountPaid, B.DueDate, B.Status, B.CreatedAt,
        F.FlatNumber, B1.BlockName, 
        O.FullName AS OwnerName, 
        T.FullName AS TenantName,
        U.Name AS UtilityName, U.RatePerUnit, U.MeasurementUnit
    FROM UtilityBills B
    INNER JOIN Flats F ON B.FlatId = F.FlatId
    INNER JOIN Blocks B1 ON F.BlockId = B1.BlockId
    INNER JOIN UtilityTypes U ON B.UtilityTypeId = U.UtilityTypeId
    LEFT JOIN Users O ON F.OwnerId = O.UserId
    LEFT JOIN Users T ON F.TenantId = T.UserId
    WHERE (@Month IS NULL OR B.BillMonth = @Month)
      AND (@Year IS NULL OR B.BillYear = @Year)
    ORDER BY B.BillYear DESC, B.BillMonth DESC, B.BillId DESC;
END
GO

-- 9. Stored Procedure: Record Utility Payment
CREATE OR ALTER PROCEDURE sp_Utility_RecordPayment
    @BillId INT,
    @PaidAmount DECIMAL(18,2),
    @PaymentMode NVARCHAR(50),
    @TransactionId NVARCHAR(100),
    @Remarks NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO UtilityPayments (BillId, PaidAmount, PaymentMode, TransactionId, Remarks)
    VALUES (@BillId, @PaidAmount, @PaymentMode, @TransactionId, @Remarks);

    DECLARE @TotalAmount DECIMAL(18,2);
    DECLARE @NewAmountPaid DECIMAL(18,2);
    
    SELECT @TotalAmount = TotalAmount, @NewAmountPaid = AmountPaid + @PaidAmount
    FROM UtilityBills WHERE BillId = @BillId;

    DECLARE @NewStatus NVARCHAR(50) = 'Partial';
    IF @NewAmountPaid >= @TotalAmount
    BEGIN
        SET @NewStatus = 'Paid';
    END

    UPDATE UtilityBills
    SET AmountPaid = @NewAmountPaid,
        Status = @NewStatus
    WHERE BillId = @BillId;
END
GO

-- 10. Stored Procedure: Utility Dashboard Stats
CREATE OR ALTER PROCEDURE sp_Utility_GetDashboardStats
AS
BEGIN
    DECLARE @CurrentMonth INT = MONTH(GETDATE());
    DECLARE @CurrentYear INT = YEAR(GETDATE());

    SELECT 
        ISNULL(SUM(TotalAmount), 0) AS TotalBilledThisMonth,
        ISNULL(SUM(AmountPaid), 0) AS TotalCollectedThisMonth,
        ISNULL(SUM(TotalAmount - AmountPaid), 0) AS TotalPendingOverall,
        (SELECT COUNT(*) FROM UtilityBills WHERE Status != 'Paid' AND DueDate < GETDATE()) AS DefaultersCount
    FROM UtilityBills
    WHERE BillMonth = @CurrentMonth AND BillYear = @CurrentYear;
END
GO
