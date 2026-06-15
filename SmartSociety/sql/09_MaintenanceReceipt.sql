USE [SmartSociety]
GO

CREATE OR ALTER PROCEDURE sp_Maintenance_GetBillReceipt
    @BillId INT
AS
BEGIN
    -- 1. Get Bill Info
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
    WHERE B.BillId = @BillId;

    -- 2. Get Payment Logs
    SELECT 
        PaymentId, BillId, PaidAmount, PaymentDate, PaymentMode, TransactionId, Remarks, CreatedAt
    FROM BillPayments
    WHERE BillId = @BillId
    ORDER BY PaymentDate DESC;
END
GO
