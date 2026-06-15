USE [SmartSociety]
GO

-- 1. Apply Penalties
CREATE OR ALTER PROCEDURE sp_Maintenance_ApplyPenalties
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @PenaltyAmount DECIMAL(18,2);
    SELECT TOP 1 @PenaltyAmount = PenaltyAmount FROM MaintenanceSettings;

    IF @PenaltyAmount > 0
    BEGIN
        -- Find bills that are overdue, unpaid/partial, and have NO penalty applied yet
        UPDATE MaintenanceBills
        SET PenaltyAmount = @PenaltyAmount,
            TotalAmount = BaseAmount + ExtraCharges + @PenaltyAmount
        WHERE DueDate < GETDATE() 
          AND Status != 'Paid'
          AND PenaltyAmount = 0;
          
        SELECT @@ROWCOUNT AS AppliedCount;
    END
    ELSE
    BEGIN
        SELECT 0 AS AppliedCount;
    END
END
GO

-- 2. Update Bill
CREATE OR ALTER PROCEDURE sp_Maintenance_UpdateBill
    @BillId INT,
    @BaseAmount DECIMAL(18,2),
    @ExtraCharges DECIMAL(18,2),
    @ExtraChargeRemarks NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    -- Update only if the bill has 0 AmountPaid (optional strict rule, but let's allow it anyway, just recalculate Total)
    -- Actually, to be safe, we allow edit anytime, but it recalculates TotalAmount and Balance correctly
    DECLARE @OldTotal DECIMAL(18,2);
    DECLARE @AmountPaid DECIMAL(18,2);
    DECLARE @PenaltyAmount DECIMAL(18,2);
    
    SELECT @OldTotal = TotalAmount, @AmountPaid = AmountPaid, @PenaltyAmount = PenaltyAmount
    FROM MaintenanceBills WHERE BillId = @BillId;

    DECLARE @NewTotal DECIMAL(18,2) = @BaseAmount + @ExtraCharges + @PenaltyAmount;
    
    DECLARE @NewStatus NVARCHAR(50) = 'Unpaid';
    IF @AmountPaid >= @NewTotal AND @NewTotal > 0
        SET @NewStatus = 'Paid';
    ELSE IF @AmountPaid > 0
        SET @NewStatus = 'Partial';

    UPDATE MaintenanceBills
    SET BaseAmount = @BaseAmount,
        ExtraCharges = @ExtraCharges,
        ExtraChargeRemarks = @ExtraChargeRemarks,
        TotalAmount = @NewTotal,
        Status = @NewStatus
    WHERE BillId = @BillId;
END
GO

-- 3. Delete Bill
CREATE OR ALTER PROCEDURE sp_Maintenance_DeleteBill
    @BillId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Check if payments exist
    IF EXISTS (SELECT 1 FROM BillPayments WHERE BillId = @BillId)
    BEGIN
        RAISERROR('Cannot delete bill because payments have been recorded against it.', 16, 1);
        RETURN;
    END

    DELETE FROM MaintenanceBills WHERE BillId = @BillId;
END
GO
