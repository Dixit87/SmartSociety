USE [SmartSociety];
GO

CREATE OR ALTER PROCEDURE sp_Analytics_GetReportMetrics
AS
BEGIN
    SET NOCOUNT ON;

    -- =======================================================
    -- 1. Summary KPIs
    -- =======================================================
    DECLARE @TotalCollected DECIMAL(18,2) = 0;
    DECLARE @TotalPending DECIMAL(18,2) = 0;
    DECLARE @OpenComplaints INT = 0;
    DECLARE @TotalBookings INT = 0;

    -- Maintenance Dues (Total amount of unpaid bills)
    SELECT @TotalPending = ISNULL(SUM(TotalAmount - AmountPaid), 0) FROM MaintenanceBills WHERE Status IN ('Unpaid', 'Partial');
    
    -- Collected Amount (Total of paid bills)
    SELECT @TotalCollected = ISNULL(SUM(AmountPaid), 0) FROM MaintenanceBills;

    -- Open Complaints
    SELECT @OpenComplaints = COUNT(*) FROM Complaints WHERE Status IN ('Open', 'In Progress');

    -- Total Amenity Bookings
    SELECT @TotalBookings = COUNT(*) FROM AmenityBookings;

    -- Return ResultSet 1: KPIs
    SELECT 
        @TotalCollected AS TotalCollected,
        @TotalPending AS TotalPending,
        @OpenComplaints AS OpenComplaints,
        @TotalBookings AS TotalBookings;

    -- =======================================================
    -- 2. Complaint Analysis (By Category)
    -- =======================================================
    -- Return ResultSet 2: Complaints by Category
    SELECT 
        Category,
        COUNT(*) AS Count
    FROM Complaints
    GROUP BY Category;

    -- =======================================================
    -- 3. Maintenance Collection Status (Paid vs Unpaid vs Overdue)
    -- =======================================================
    -- Return ResultSet 3: Bill Status
    SELECT 
        Status,
        SUM(TotalAmount) AS TotalAmount
    FROM MaintenanceBills
    GROUP BY Status;

    -- =======================================================
    -- 4. Recent High-Value Dues (Top 5 Defaulters)
    -- =======================================================
    -- Return ResultSet 4: Defaulters
    SELECT TOP 5
        u.FullName AS ResidentName,
        f.FlatNumber,
        (mb.TotalAmount - mb.AmountPaid) AS DueAmount,
        mb.DueDate
    FROM MaintenanceBills mb
    JOIN Flats f ON mb.FlatId = f.FlatId
    JOIN Users u ON ISNULL(f.TenantId, f.OwnerId) = u.UserId
    WHERE mb.Status IN ('Unpaid', 'Partial') AND mb.DueDate < GETDATE()
    ORDER BY DueAmount DESC;

END
GO
