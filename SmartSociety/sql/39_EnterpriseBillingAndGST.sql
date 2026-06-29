-- SQL Migration: Enterprise Billing, GST & Sinking Funds (Enterprise Billing & GST)

-- 1. Alter MaintenanceSettings table to add GST configuration
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('MaintenanceSettings') AND name = 'GstEnabled')
BEGIN
    ALTER TABLE MaintenanceSettings ADD 
        GstEnabled BIT NOT NULL DEFAULT 0,
        GstRate DECIMAL(5,2) NOT NULL DEFAULT 18.00,
        GstThreshold DECIMAL(18,2) NOT NULL DEFAULT 7500.00,
        Gstin NVARCHAR(50) NULL,
        SocietyAnnualTurnover DECIMAL(18,2) NOT NULL DEFAULT 1500000.00;
END
GO

-- 2. Alter MaintenanceBills table to add GST tracking columns
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('MaintenanceBills') AND name = 'TaxAmount')
BEGIN
    ALTER TABLE MaintenanceBills ADD 
        TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        CGST DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        SGST DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        GstRate DECIMAL(5,2) NOT NULL DEFAULT 0.00;
END
GO

-- 3. Create FixedDeposits Table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FixedDeposits')
BEGIN
    CREATE TABLE FixedDeposits (
        FdId INT IDENTITY(1,1) PRIMARY KEY,
        FdNumber NVARCHAR(100) UNIQUE NOT NULL,
        BankName NVARCHAR(150) NOT NULL,
        PrincipalAmount DECIMAL(18,2) NOT NULL,
        InterestRate DECIMAL(5,2) NOT NULL,
        MaturityDate DATETIME NOT NULL,
        MaturityAmount DECIMAL(18,2) NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Active', -- 'Active', 'Matured', 'Liquidated'
        DateInvested DATETIME NOT NULL DEFAULT GETDATE(),
        Notes NVARCHAR(255) NULL
    );
END
GO

-- 4. Create SinkingFundTransactions Table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SinkingFundTransactions')
BEGIN
    CREATE TABLE SinkingFundTransactions (
        TransactionId INT IDENTITY(1,1) PRIMARY KEY,
        Type NVARCHAR(50) NOT NULL, -- 'Contribution', 'Withdrawal'
        Amount DECIMAL(18,2) NOT NULL,
        TransactionDate DATETIME NOT NULL DEFAULT GETDATE(),
        Purpose NVARCHAR(255) NOT NULL,
        ReferenceId NVARCHAR(100) NULL
    );
END
GO

-- --- Stored Procedures Updates ---

-- A. Update sp_Maintenance_GetSettings to return GST fields
CREATE OR ALTER PROCEDURE sp_Maintenance_GetSettings
AS
BEGIN
    SELECT TOP 1 
        SettingId, BillingType, Rate, PenaltyAmount, DueDays, UpdatedAt,
        GstEnabled, GstRate, GstThreshold, Gstin, SocietyAnnualTurnover
    FROM MaintenanceSettings 
    ORDER BY SettingId DESC;
END
GO

-- B. Update sp_Maintenance_UpdateSettings to support GST fields
CREATE OR ALTER PROCEDURE sp_Maintenance_UpdateSettings
    @BillingType NVARCHAR(50),
    @Rate DECIMAL(18,2),
    @PenaltyAmount DECIMAL(18,2),
    @DueDays INT,
    @GstEnabled BIT,
    @GstRate DECIMAL(5,2),
    @GstThreshold DECIMAL(18,2),
    @Gstin NVARCHAR(50),
    @SocietyAnnualTurnover DECIMAL(18,2)
AS
BEGIN
    UPDATE MaintenanceSettings 
    SET BillingType = @BillingType, 
        Rate = @Rate, 
        PenaltyAmount = @PenaltyAmount, 
        DueDays = @DueDays,
        GstEnabled = @GstEnabled,
        GstRate = @GstRate,
        GstThreshold = @GstThreshold,
        Gstin = @Gstin,
        SocietyAnnualTurnover = @SocietyAnnualTurnover,
        UpdatedAt = GETDATE();

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO MaintenanceSettings (
            BillingType, Rate, PenaltyAmount, DueDays, 
            GstEnabled, GstRate, GstThreshold, Gstin, SocietyAnnualTurnover
        )
        VALUES (
            @BillingType, @Rate, @PenaltyAmount, @DueDays, 
            @GstEnabled, @GstRate, @GstThreshold, @Gstin, @SocietyAnnualTurnover
        );
    END
END
GO

-- C. Update sp_Maintenance_GenerateBulkBills with GST Calculation
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
    DECLARE @GstEnabled BIT;
    DECLARE @GstRate DECIMAL(5,2);
    DECLARE @GstThreshold DECIMAL(18,2);
    DECLARE @SocietyAnnualTurnover DECIMAL(18,2);

    SELECT TOP 1 
        @BillingType = BillingType, 
        @Rate = Rate, 
        @DueDays = DueDays,
        @GstEnabled = GstEnabled,
        @GstRate = GstRate,
        @GstThreshold = GstThreshold,
        @SocietyAnnualTurnover = SocietyAnnualTurnover
    FROM MaintenanceSettings;

    -- Calculate Base & GST and Insert
    ;WITH FlatBilling AS (
        SELECT 
            FlatId,
            CASE 
                WHEN @BillingType = 'PerSqFt' THEN AreaSqFt * @Rate
                ELSE @Rate
            END AS BaseAmount
        FROM Flats
        WHERE IsActive = 1
    ),
    BillingTotals AS (
        SELECT 
            FlatId,
            BaseAmount,
            @ExtraCharges AS ExtraCharges,
            BaseAmount + @ExtraCharges AS RawSubtotal
        FROM FlatBilling
    ),
    BillingTax AS (
        SELECT 
            FlatId,
            BaseAmount,
            ExtraCharges,
            RawSubtotal,
            CASE 
                WHEN @GstEnabled = 1 AND (@SocietyAnnualTurnover >= 2000000.00 OR @GstThreshold = 0.00) AND (RawSubtotal > @GstThreshold) 
                THEN @GstRate 
                ELSE 0.00 
            END AS AppliedGstRate
        FROM BillingTotals
    ),
    BillingCalculations AS (
        SELECT 
            FlatId,
            BaseAmount,
            ExtraCharges,
            RawSubtotal,
            AppliedGstRate,
            CAST(RawSubtotal * (AppliedGstRate / 2.0 / 100.0) AS DECIMAL(18,2)) AS CGST,
            CAST(RawSubtotal * (AppliedGstRate / 2.0 / 100.0) AS DECIMAL(18,2)) AS SGST
        FROM BillingTax
    )
    INSERT INTO MaintenanceBills (
        FlatId, BillMonth, BillYear, BaseAmount, ExtraCharges, ExtraChargeRemarks, 
        TaxAmount, CGST, SGST, GstRate, TotalAmount, DueDate, Status
    )
    SELECT 
        C.FlatId,
        @BillMonth,
        @BillYear,
        C.BaseAmount,
        C.ExtraCharges,
        @ExtraChargeRemarks,
        C.CGST + C.SGST AS TaxAmount,
        C.CGST,
        C.SGST,
        C.AppliedGstRate,
        C.RawSubtotal + (C.CGST + C.SGST) AS TotalAmount,
        DATEADD(day, @DueDays, GETDATE()) AS DueDate,
        'Unpaid'
    FROM BillingCalculations C
    WHERE NOT EXISTS (
        SELECT 1 FROM MaintenanceBills MB 
        WHERE MB.FlatId = C.FlatId 
          AND MB.BillMonth = @BillMonth 
          AND MB.BillYear = @BillYear
    );
END
GO

-- D. Update sp_Maintenance_GetBills to fetch GST details
CREATE OR ALTER PROCEDURE sp_Maintenance_GetBills
    @Month INT = NULL,
    @Year INT = NULL
AS
BEGIN
    SELECT 
        B.BillId, B.FlatId, B.BillMonth, B.BillYear, 
        B.BaseAmount, B.ExtraCharges, B.ExtraChargeRemarks, B.PenaltyAmount, 
        B.TaxAmount, B.CGST, B.SGST, B.GstRate,
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

-- E. Update sp_Maintenance_GetBillReceipt to fetch GST details
CREATE OR ALTER PROCEDURE sp_Maintenance_GetBillReceipt
    @BillId INT
AS
BEGIN
    -- 1. Get Bill Info
    SELECT 
        B.BillId, B.FlatId, B.BillMonth, B.BillYear, 
        B.BaseAmount, B.ExtraCharges, B.ExtraChargeRemarks, B.PenaltyAmount, 
        B.TaxAmount, B.CGST, B.SGST, B.GstRate,
        B.TotalAmount, B.AmountPaid, B.DueDate, B.Status, B.CreatedAt,
        F.FlatNumber, B1.BlockName, 
        O.FullName AS OwnerName, 
        T.FullName AS TenantName
    FROM MaintenanceBills B
    INNER JOIN Flats F ON B.FlatId = F.FlatId
    INNER JOIN Blocks B1 ON F.BlockId = B1.BlockId
    LEFT JOIN Users O ON F.OwnerId = O.UserId
    LEFT JOIN Users T ON F.TenantId = T.UserId
    WHERE B.BillId = @BillId;

    -- 2. Get Payment Logs
    SELECT 
        PaymentId, BillId, PaidAmount, PaymentDate, PaymentMode, TransactionId, Remarks, CreatedAt
    FROM BillPayments
    WHERE BillId = @BillId
    ORDER BY PaymentDate DESC;
END
GO

-- F. Update sp_Maintenance_UpdateBill with GST support
CREATE OR ALTER PROCEDURE sp_Maintenance_UpdateBill
    @BillId INT,
    @BaseAmount DECIMAL(18,2),
    @ExtraCharges DECIMAL(18,2),
    @ExtraChargeRemarks NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AmountPaid DECIMAL(18,2);
    DECLARE @PenaltyAmount DECIMAL(18,2);
    
    SELECT @AmountPaid = AmountPaid, @PenaltyAmount = PenaltyAmount
    FROM MaintenanceBills WHERE BillId = @BillId;

    DECLARE @GstEnabled BIT;
    DECLARE @GstRate DECIMAL(5,2);
    DECLARE @GstThreshold DECIMAL(18,2);
    DECLARE @SocietyAnnualTurnover DECIMAL(18,2);

    SELECT TOP 1 
        @GstEnabled = GstEnabled,
        @GstRate = GstRate,
        @GstThreshold = GstThreshold,
        @SocietyAnnualTurnover = SocietyAnnualTurnover
    FROM MaintenanceSettings;

    DECLARE @RawSubtotal DECIMAL(18,2) = @BaseAmount + @ExtraCharges;
    DECLARE @AppliedGstRate DECIMAL(5,2) = 0.00;
    
    IF @GstEnabled = 1 AND (@SocietyAnnualTurnover >= 2000000.00 OR @GstThreshold = 0.00) AND (@RawSubtotal > @GstThreshold)
    BEGIN
        SET @AppliedGstRate = @GstRate;
    END

    DECLARE @CGST DECIMAL(18,2) = CAST(@RawSubtotal * (@AppliedGstRate / 2.0 / 100.0) AS DECIMAL(18,2));
    DECLARE @SGST DECIMAL(18,2) = CAST(@RawSubtotal * (@AppliedGstRate / 2.0 / 100.0) AS DECIMAL(18,2));
    DECLARE @TaxAmount DECIMAL(18,2) = @CGST + @SGST;

    DECLARE @NewTotal DECIMAL(18,2) = @RawSubtotal + @PenaltyAmount + @TaxAmount;
    
    DECLARE @NewStatus NVARCHAR(50) = 'Unpaid';
    IF @AmountPaid >= @NewTotal AND @NewTotal > 0
        SET @NewStatus = 'Paid';
    ELSE IF @AmountPaid > 0
        SET @NewStatus = 'Partial';

    UPDATE MaintenanceBills
    SET BaseAmount = @BaseAmount,
        ExtraCharges = @ExtraCharges,
        ExtraChargeRemarks = @ExtraChargeRemarks,
        TaxAmount = @TaxAmount,
        CGST = @CGST,
        SGST = @SGST,
        GstRate = @AppliedGstRate,
        TotalAmount = @NewTotal,
        Status = @NewStatus
    WHERE BillId = @BillId;
END
GO

-- G. Update sp_Maintenance_RecordPayment to auto-credit Sinking Fund (10% allocation)
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

    -- Automatically log 10% Contribution to Sinking Fund
    DECLARE @SinkingContribution DECIMAL(18,2) = CAST(@PaidAmount * 0.10 AS DECIMAL(18,2));
    IF @SinkingContribution > 0
    BEGIN
        INSERT INTO SinkingFundTransactions (Type, Amount, TransactionDate, Purpose, ReferenceId)
        VALUES ('Contribution', @SinkingContribution, GETDATE(), 
                '10% automatic Sinking Fund contribution from Maintenance Bill #' + CAST(@BillId AS VARCHAR), 
                @TransactionId);
    END
END
GO

-- H. Create Stored Procedures for Fixed Deposits & Sinking Fund
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_FixedDeposits_GetAll')
    DROP PROCEDURE sp_FixedDeposits_GetAll;
GO
CREATE PROCEDURE sp_FixedDeposits_GetAll
AS
BEGIN
    SELECT * FROM FixedDeposits ORDER BY MaturityDate ASC;
END
GO

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_FixedDeposits_Create')
    DROP PROCEDURE sp_FixedDeposits_Create;
GO
CREATE PROCEDURE sp_FixedDeposits_Create
    @FdNumber NVARCHAR(100),
    @BankName NVARCHAR(150),
    @PrincipalAmount DECIMAL(18,2),
    @InterestRate DECIMAL(5,2),
    @MaturityDate DATETIME,
    @MaturityAmount DECIMAL(18,2),
    @Status NVARCHAR(50),
    @DateInvested DATETIME,
    @Notes NVARCHAR(255),
    @NewFdId INT OUTPUT
AS
BEGIN
    INSERT INTO FixedDeposits (FdNumber, BankName, PrincipalAmount, InterestRate, MaturityDate, MaturityAmount, Status, DateInvested, Notes)
    VALUES (@FdNumber, @BankName, @PrincipalAmount, @InterestRate, @MaturityDate, @MaturityAmount, @Status, @DateInvested, @Notes);
    SET @NewFdId = SCOPE_IDENTITY();
END
GO

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_FixedDeposits_UpdateStatus')
    DROP PROCEDURE sp_FixedDeposits_UpdateStatus;
GO
CREATE PROCEDURE sp_FixedDeposits_UpdateStatus
    @FdId INT,
    @Status NVARCHAR(50)
AS
BEGIN
    UPDATE FixedDeposits SET Status = @Status WHERE FdId = @FdId;
END
GO

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_SinkingFund_GetAll')
    DROP PROCEDURE sp_SinkingFund_GetAll;
GO
CREATE PROCEDURE sp_SinkingFund_GetAll
AS
BEGIN
    SELECT * FROM SinkingFundTransactions ORDER BY TransactionDate DESC;
END
GO

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_SinkingFund_Create')
    DROP PROCEDURE sp_SinkingFund_Create;
GO
CREATE PROCEDURE sp_SinkingFund_Create
    @Type NVARCHAR(50),
    @Amount DECIMAL(18,2),
    @Purpose NVARCHAR(255),
    @ReferenceId NVARCHAR(100),
    @NewTransactionId INT OUTPUT
AS
BEGIN
    INSERT INTO SinkingFundTransactions (Type, Amount, TransactionDate, Purpose, ReferenceId)
    VALUES (@Type, @Amount, GETDATE(), @Purpose, @ReferenceId);
    SET @NewTransactionId = SCOPE_IDENTITY();
END
GO

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_SinkingFund_GetBalance')
    DROP PROCEDURE sp_SinkingFund_GetBalance;
GO
CREATE PROCEDURE sp_SinkingFund_GetBalance
AS
BEGIN
    SELECT 
        ISNULL(SUM(CASE WHEN Type = 'Contribution' THEN Amount ELSE -Amount END), 0) AS Balance
    FROM SinkingFundTransactions;
END
GO
