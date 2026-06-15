USE [SmartSociety]
GO

-- 1. Get Previous Reading for Auto-fetch
CREATE OR ALTER PROCEDURE sp_Utility_GetPreviousReading
    @FlatId INT,
    @UtilityTypeId INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @PreviousReading DECIMAL(18,2) = 0;
    
    SELECT TOP 1 @PreviousReading = CurrentReading 
    FROM UtilityBills 
    WHERE FlatId = @FlatId AND UtilityTypeId = @UtilityTypeId 
    ORDER BY BillYear DESC, BillMonth DESC;
    
    SELECT @PreviousReading AS PreviousReading;
END
GO

-- 2. Modify Record Reading to support OverridePreviousReading
CREATE OR ALTER PROCEDURE sp_Utility_RecordReading
    @FlatId INT,
    @UtilityTypeId INT,
    @BillMonth INT,
    @BillYear INT,
    @CurrentReading DECIMAL(18,2),
    @OverridePreviousReading DECIMAL(18,2) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Check if a bill already exists for this flat, utility type, month, year
    IF EXISTS (SELECT 1 FROM UtilityBills WHERE FlatId = @FlatId AND UtilityTypeId = @UtilityTypeId AND BillMonth = @BillMonth AND BillYear = @BillYear)
    BEGIN
        RAISERROR('A bill for this utility already exists for the selected month and year.', 16, 1);
        RETURN;
    END

    -- Fetch or use Override Previous Reading
    DECLARE @PreviousReading DECIMAL(18,2) = 0;
    
    IF @OverridePreviousReading IS NOT NULL
    BEGIN
        SET @PreviousReading = @OverridePreviousReading;
    END
    ELSE
    BEGIN
        SELECT TOP 1 @PreviousReading = CurrentReading 
        FROM UtilityBills 
        WHERE FlatId = @FlatId AND UtilityTypeId = @UtilityTypeId 
        ORDER BY BillYear DESC, BillMonth DESC;
    END

    -- Calculate Consumption
    IF @CurrentReading < @PreviousReading
    BEGIN
        RAISERROR('Current reading cannot be less than previous reading.', 16, 1);
        RETURN;
    END

    DECLARE @ConsumedUnits DECIMAL(18,2) = @CurrentReading - @PreviousReading;

    -- Get Rate
    DECLARE @RatePerUnit DECIMAL(18,2) = 0;
    SELECT @RatePerUnit = RatePerUnit FROM UtilityTypes WHERE UtilityTypeId = @UtilityTypeId;

    DECLARE @TotalAmount DECIMAL(18,2) = @ConsumedUnits * @RatePerUnit;
    
    -- Calculate Due Date (15 days from now)
    DECLARE @DueDate DATETIME = DATEADD(day, 15, GETDATE());

    -- Insert Bill
    INSERT INTO UtilityBills (FlatId, UtilityTypeId, BillMonth, BillYear, PreviousReading, CurrentReading, ConsumedUnits, TotalAmount, DueDate, Status)
    VALUES (@FlatId, @UtilityTypeId, @BillMonth, @BillYear, @PreviousReading, @CurrentReading, @ConsumedUnits, @TotalAmount, @DueDate, 'Unpaid');
END
GO

-- 3. Update Bill
CREATE OR ALTER PROCEDURE sp_Utility_UpdateBill
    @BillId INT,
    @PreviousReading DECIMAL(18,2),
    @CurrentReading DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @CurrentReading < @PreviousReading
    BEGIN
        RAISERROR('Current reading cannot be less than previous reading.', 16, 1);
        RETURN;
    END

    DECLARE @ConsumedUnits DECIMAL(18,2) = @CurrentReading - @PreviousReading;
    DECLARE @RatePerUnit DECIMAL(18,2) = 0;
    DECLARE @UtilityTypeId INT;
    DECLARE @AmountPaid DECIMAL(18,2);
    
    SELECT @UtilityTypeId = UtilityTypeId, @AmountPaid = AmountPaid FROM UtilityBills WHERE BillId = @BillId;
    SELECT @RatePerUnit = RatePerUnit FROM UtilityTypes WHERE UtilityTypeId = @UtilityTypeId;

    DECLARE @TotalAmount DECIMAL(18,2) = @ConsumedUnits * @RatePerUnit;
    
    DECLARE @NewStatus NVARCHAR(50) = 'Unpaid';
    IF @AmountPaid >= @TotalAmount AND @TotalAmount > 0
    BEGIN
        SET @NewStatus = 'Paid';
    END
    ELSE IF @AmountPaid > 0
    BEGIN
        SET @NewStatus = 'Partial';
    END

    UPDATE UtilityBills
    SET PreviousReading = @PreviousReading,
        CurrentReading = @CurrentReading,
        ConsumedUnits = @ConsumedUnits,
        TotalAmount = @TotalAmount,
        Status = @NewStatus
    WHERE BillId = @BillId;
END
GO

-- 4. Delete Bill
CREATE OR ALTER PROCEDURE sp_Utility_DeleteBill
    @BillId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @AmountPaid DECIMAL(18,2) = 0;
    SELECT @AmountPaid = AmountPaid FROM UtilityBills WHERE BillId = @BillId;

    IF @AmountPaid > 0
    BEGIN
        RAISERROR('Cannot delete a bill that has payments associated with it.', 16, 1);
        RETURN;
    END

    DELETE FROM UtilityBills WHERE BillId = @BillId;
END
GO

-- 5. Get Bill By Id (For Receipt)
CREATE OR ALTER PROCEDURE sp_Utility_GetBillById
    @BillId INT
AS
BEGIN
    SELECT 
        B.BillId, B.FlatId, B.UtilityTypeId, B.BillMonth, B.BillYear, 
        B.PreviousReading, B.CurrentReading, B.ConsumedUnits, 
        B.TotalAmount, B.AmountPaid, B.DueDate, B.Status, B.CreatedAt,
        F.FlatNumber, B1.BlockName, 
        O.FullName AS OwnerName, O.Email AS OwnerEmail, O.PhoneNumber AS OwnerPhone,
        T.FullName AS TenantName,
        U.Name AS UtilityName, U.RatePerUnit, U.MeasurementUnit
    FROM UtilityBills B
    INNER JOIN Flats F ON B.FlatId = F.FlatId
    INNER JOIN Blocks B1 ON F.BlockId = B1.BlockId
    INNER JOIN UtilityTypes U ON B.UtilityTypeId = U.UtilityTypeId
    LEFT JOIN Users O ON F.OwnerId = O.UserId
    LEFT JOIN Users T ON F.TenantId = T.UserId
    WHERE B.BillId = @BillId;
END
GO

-- 6. Get Payments By Bill (For Receipt)
CREATE OR ALTER PROCEDURE sp_Utility_GetPaymentsByBill
    @BillId INT
AS
BEGIN
    SELECT * FROM UtilityPayments WHERE BillId = @BillId ORDER BY PaymentDate DESC;
END
GO
