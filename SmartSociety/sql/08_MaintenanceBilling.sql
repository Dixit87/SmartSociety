USE [SmartSociety]
GO

-- 1. Create MaintenanceSettings Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MaintenanceSettings]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MaintenanceSettings](
        [SettingId] INT IDENTITY(1,1) PRIMARY KEY,
        [BillingType] NVARCHAR(50) NOT NULL DEFAULT 'Fixed', -- 'Fixed' or 'PerSqFt'
        [Rate] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [PenaltyAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [DueDays] INT NOT NULL DEFAULT 10,
        [UpdatedAt] DATETIME DEFAULT GETDATE()
    )

    -- Insert default row
    INSERT INTO [dbo].[MaintenanceSettings] ([BillingType], [Rate], [PenaltyAmount], [DueDays])
    VALUES ('Fixed', 2500, 150, 10)
END
GO

-- 2. Create MaintenanceBills Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MaintenanceBills]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MaintenanceBills](
        [BillId] INT IDENTITY(1,1) PRIMARY KEY,
        [FlatId] INT NOT NULL,
        [BillMonth] INT NOT NULL,
        [BillYear] INT NOT NULL,
        [BaseAmount] DECIMAL(18,2) NOT NULL,
        [ExtraCharges] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [ExtraChargeRemarks] NVARCHAR(255) NULL,
        [PenaltyAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TotalAmount] DECIMAL(18,2) NOT NULL,
        [AmountPaid] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [DueDate] DATETIME NOT NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Unpaid', -- 'Unpaid', 'Partial', 'Paid'
        [CreatedAt] DATETIME DEFAULT GETDATE(),
        FOREIGN KEY (FlatId) REFERENCES Flats(FlatId)
    )
END
GO

-- 3. Create BillPayments Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BillPayments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[BillPayments](
        [PaymentId] INT IDENTITY(1,1) PRIMARY KEY,
        [BillId] INT NOT NULL,
        [PaidAmount] DECIMAL(18,2) NOT NULL,
        [PaymentDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [PaymentMode] NVARCHAR(50) NOT NULL, -- Cash, Cheque, UPI, BankTransfer
        [TransactionId] NVARCHAR(100) NULL,
        [Remarks] NVARCHAR(255) NULL,
        [CreatedAt] DATETIME DEFAULT GETDATE(),
        FOREIGN KEY (BillId) REFERENCES MaintenanceBills(BillId)
    )
END
GO

-- 4. Stored Procedure: Get Settings
CREATE OR ALTER PROCEDURE sp_Maintenance_GetSettings
AS
BEGIN
    SELECT TOP 1 * FROM MaintenanceSettings ORDER BY SettingId DESC;
END
GO

-- 5. Stored Procedure: Update Settings
CREATE OR ALTER PROCEDURE sp_Maintenance_UpdateSettings
    @BillingType NVARCHAR(50),
    @Rate DECIMAL(18,2),
    @PenaltyAmount DECIMAL(18,2),
    @DueDays INT
AS
BEGIN
    UPDATE MaintenanceSettings 
    SET BillingType = @BillingType, 
        Rate = @Rate, 
        PenaltyAmount = @PenaltyAmount, 
        DueDays = @DueDays, 
        UpdatedAt = GETDATE();

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO MaintenanceSettings (BillingType, Rate, PenaltyAmount, DueDays)
        VALUES (@BillingType, @Rate, @PenaltyAmount, @DueDays);
    END
END
GO

-- 6. Stored Procedure: Generate Bulk Bills
CREATE OR ALTER PROCEDURE sp_Maintenance_GenerateBulkBills
    @BillMonth INT,
    @BillYear INT,
    @ExtraCharges DECIMAL(18,2) = 0,
    @ExtraChargeRemarks NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @BillingType NVARCHAR(50);
    DECLARE @Rate DECIMAL(18,2);
    DECLARE @DueDays INT;

    SELECT TOP 1 
        @BillingType = BillingType, 
        @Rate = Rate, 
        @DueDays = DueDays 
    FROM MaintenanceSettings;

    -- Insert new bills for active flats that don't already have a bill for this month/year
    INSERT INTO MaintenanceBills (
        FlatId, BillMonth, BillYear, BaseAmount, ExtraCharges, ExtraChargeRemarks, 
        TotalAmount, DueDate, Status
    )
    SELECT 
        F.FlatId,
        @BillMonth,
        @BillYear,
        -- Calculate BaseAmount
        CASE 
            WHEN @BillingType = 'PerSqFt' THEN F.AreaSqFt * @Rate
            ELSE @Rate
        END AS BaseAmount,
        @ExtraCharges,
        @ExtraChargeRemarks,
        -- Calculate TotalAmount
        (CASE 
            WHEN @BillingType = 'PerSqFt' THEN F.AreaSqFt * @Rate
            ELSE @Rate
        END) + @ExtraCharges AS TotalAmount,
        -- Calculate Due Date
        DATEADD(day, @DueDays, GETDATE()) AS DueDate,
        'Unpaid'
    FROM Flats F
    WHERE F.IsActive = 1
    AND NOT EXISTS (
        SELECT 1 FROM MaintenanceBills MB 
        WHERE MB.FlatId = F.FlatId 
          AND MB.BillMonth = @BillMonth 
          AND MB.BillYear = @BillYear
    );
END
GO

-- 7. Stored Procedure: Get Bills
CREATE OR ALTER PROCEDURE sp_Maintenance_GetBills
    @Month INT = NULL,
    @Year INT = NULL
AS
BEGIN
    SELECT 
        B.BillId, B.FlatId, B.BillMonth, B.BillYear, 
        B.BaseAmount, B.ExtraCharges, B.ExtraChargeRemarks, B.PenaltyAmount, 
        B.TotalAmount, B.AmountPaid, B.DueDate, B.Status, B.CreatedAt,
        F.FlatNumber, B1.BlockName, 
        O.FullName AS OwnerName, 
        T.FullName AS TenantName
    FROM MaintenanceBills B
    INNER JOIN Flats F ON B.FlatId = F.FlatId
    INNER JOIN Blocks B1 ON F.BlockId = B1.BlockId
    LEFT JOIN Users O ON F.OwnerId = O.UserId
    LEFT JOIN Users T ON F.TenantId = T.UserId
    WHERE (@Month IS NULL OR B.BillMonth = @Month)
      AND (@Year IS NULL OR B.BillYear = @Year)
    ORDER BY B.BillYear DESC, B.BillMonth DESC, B1.BlockName ASC, F.FlatNumber ASC;
END
GO

-- 8. Stored Procedure: Record Payment
CREATE OR ALTER PROCEDURE sp_Maintenance_RecordPayment
    @BillId INT,
    @PaidAmount DECIMAL(18,2),
    @PaymentMode NVARCHAR(50),
    @TransactionId NVARCHAR(100),
    @Remarks NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Insert into BillPayments
    INSERT INTO BillPayments (BillId, PaidAmount, PaymentMode, TransactionId, Remarks)
    VALUES (@BillId, @PaidAmount, @PaymentMode, @TransactionId, @Remarks);

    -- Update MaintenanceBills
    DECLARE @TotalAmount DECIMAL(18,2);
    DECLARE @NewAmountPaid DECIMAL(18,2);
    
    SELECT @TotalAmount = TotalAmount, @NewAmountPaid = AmountPaid + @PaidAmount
    FROM MaintenanceBills WHERE BillId = @BillId;

    DECLARE @NewStatus NVARCHAR(50) = 'Partial';
    IF @NewAmountPaid >= @TotalAmount
    BEGIN
        SET @NewStatus = 'Paid';
    END

    UPDATE MaintenanceBills
    SET AmountPaid = @NewAmountPaid,
        Status = @NewStatus
    WHERE BillId = @BillId;
END
GO

-- 9. Stored Procedure: Dashboard Stats
CREATE OR ALTER PROCEDURE sp_Maintenance_GetDashboardStats
AS
BEGIN
    DECLARE @CurrentMonth INT = MONTH(GETDATE());
    DECLARE @CurrentYear INT = YEAR(GETDATE());

    SELECT 
        ISNULL(SUM(TotalAmount), 0) AS TotalExpectedThisMonth,
        ISNULL(SUM(AmountPaid), 0) AS TotalCollectedThisMonth,
        ISNULL(SUM(TotalAmount - AmountPaid), 0) AS TotalPendingOverall,
        (SELECT COUNT(*) FROM MaintenanceBills WHERE Status != 'Paid' AND DueDate < GETDATE()) AS TotalDefaulters
    FROM MaintenanceBills
    WHERE BillMonth = @CurrentMonth AND BillYear = @CurrentYear;
END
GO
