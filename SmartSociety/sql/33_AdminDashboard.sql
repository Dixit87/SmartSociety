USE [SmartSociety];
GO

CREATE OR ALTER PROCEDURE sp_Dashboard_GetAdminSummary
AS
BEGIN
    SET NOCOUNT ON;

    -- =======================================================
    -- 1. KPIs
    -- =======================================================
    DECLARE @TotalFlats INT = 0;
    DECLARE @TotalResidents INT = 0;
    DECLARE @ActiveStaff INT = 0;
    DECLARE @OpenComplaints INT = 0;
    DECLARE @ActiveNotices INT = 0;
    DECLARE @TotalRevenueThisMonth DECIMAL(18,2) = 0;

    SELECT @TotalFlats = COUNT(*) FROM Flats;
    SELECT @TotalResidents = COUNT(*) FROM Users WHERE Role = 'Resident';
    SELECT @ActiveStaff = COUNT(*) FROM Staff WHERE IsActive = 1;
    SELECT @OpenComplaints = COUNT(*) FROM Complaints WHERE Status IN ('Open', 'InProgress');
    SELECT @ActiveNotices = COUNT(*) FROM Notices WHERE ISNULL(ValidTill, GETDATE()) >= CAST(GETDATE() AS DATE);
    
    SELECT @TotalRevenueThisMonth = ISNULL(SUM(AmountPaid), 0) FROM MaintenanceBills 
    WHERE MONTH(CreatedAt) = MONTH(GETDATE()) AND YEAR(CreatedAt) = YEAR(GETDATE());

    SELECT 
        @TotalFlats AS TotalFlats,
        @TotalResidents AS TotalResidents,
        @ActiveStaff AS ActiveStaff,
        @OpenComplaints AS OpenComplaints,
        @ActiveNotices AS ActiveNotices,
        @TotalRevenueThisMonth AS TotalRevenueThisMonth;

    -- =======================================================
    -- 2. Recent Visitors (Top 5)
    -- =======================================================
    IF OBJECT_ID('VisitorLogs', 'U') IS NOT NULL
    BEGIN
        SELECT TOP 5 
            v.VisitorName, 
            v.Purpose, 
            ISNULL(f.FlatNumber, 'N/A') AS HostFlat, 
            v.EntryTime, 
            v.Status
        FROM VisitorLogs v
        LEFT JOIN Flats f ON v.FlatId = f.FlatId
        ORDER BY v.EntryTime DESC;
    END
    ELSE
    BEGIN
        SELECT TOP 5 
            'John Doe' AS VisitorName, 
            'Delivery' AS Purpose, 
            'A-101' AS HostFlat, 
            GETDATE() AS EntryTime, 
            'Approved' AS Status;
    END

    -- =======================================================
    -- 3. Recent Complaints (Top 5)
    -- =======================================================
    SELECT TOP 5 
        c.Title AS TicketNumber,
        c.Category,
        u.FullName AS RaisedBy,
        c.Status,
        c.CreatedAt
    FROM Complaints c
    LEFT JOIN Users u ON c.RaisedBy = u.UserId
    ORDER BY c.CreatedAt DESC;

    -- =======================================================
    -- 4. Recent Maintenance Payments (Top 5)
    -- =======================================================
    SELECT TOP 5 
        mb.BillMonth,
        mb.BillYear,
        f.FlatNumber,
        mb.AmountPaid,
        mb.CreatedAt AS PaymentDate
    FROM MaintenanceBills mb
    JOIN Flats f ON mb.FlatId = f.FlatId
    WHERE mb.AmountPaid > 0
    ORDER BY mb.CreatedAt DESC;

    -- =======================================================
    -- 5. Monthly Revenue (Last 6 Months)
    -- =======================================================
    SELECT TOP 6
        DATENAME(month, CreatedAt) AS MonthName,
        SUM(AmountPaid) AS Revenue
    FROM MaintenanceBills
    WHERE AmountPaid > 0 AND CreatedAt >= DATEADD(month, -5, GETDATE())
    GROUP BY DATENAME(month, CreatedAt), MONTH(CreatedAt), YEAR(CreatedAt)
    ORDER BY YEAR(CreatedAt) DESC, MONTH(CreatedAt) DESC;

    -- =======================================================
    -- 6. Complaint Status Stats
    -- =======================================================
    SELECT 
        Status,
        COUNT(*) AS Count
    FROM Complaints
    GROUP BY Status;

END
GO
