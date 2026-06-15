USE SmartSociety;
GO

-- 1. Modify sp_Complaint_GetAll to include Month and Year filtering
CREATE OR ALTER PROCEDURE sp_Complaint_GetAll
    @Status VARCHAR(20) = NULL,
    @Month INT = NULL,
    @Year INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        c.ComplaintId,
        c.FlatId,
        b.BlockName,
        f.FlatNumber,
        c.RaisedBy,
        u1.FullName AS ResidentName,
        c.Category,
        c.Title,
        c.Description,
        c.Priority,
        c.Status,
        c.AssignedTo,
        u2.FullName AS TechnicianName,
        c.AdminRemarks,
        c.PhotoUrl,
        c.CreatedAt,
        c.UpdatedAt,
        c.ResolvedAt
    FROM Complaints c
    INNER JOIN Flats f ON c.FlatId = f.FlatId
    INNER JOIN Blocks b ON f.BlockId = b.BlockId
    INNER JOIN Users u1 ON c.RaisedBy = u1.UserId
    LEFT JOIN Users u2 ON c.AssignedTo = u2.UserId
    WHERE (@Status IS NULL OR c.Status = @Status)
      AND (@Month IS NULL OR MONTH(c.CreatedAt) = @Month)
      AND (@Year IS NULL OR YEAR(c.CreatedAt) = @Year)
    ORDER BY 
        CASE 
            WHEN c.Status = 'Open' THEN 1
            WHEN c.Status = 'InProgress' THEN 2
            WHEN c.Status = 'Resolved' THEN 3
            ELSE 4
        END,
        CASE c.Priority 
            WHEN 'Emergency' THEN 1 
            WHEN 'High' THEN 2 
            WHEN 'Medium' THEN 3 
            WHEN 'Low' THEN 4 
            ELSE 5 
        END,
        c.CreatedAt DESC;
END
GO

-- 2. Create sp_Complaint_Update
CREATE OR ALTER PROCEDURE sp_Complaint_Update
    @ComplaintId INT,
    @Category VARCHAR(50),
    @Title VARCHAR(100),
    @Description NVARCHAR(MAX),
    @Priority VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE Complaints
    SET Category = @Category,
        Title = @Title,
        Description = @Description,
        Priority = @Priority,
        UpdatedAt = GETDATE()
    WHERE ComplaintId = @ComplaintId;
END
GO

-- 3. Create sp_Complaint_Delete
CREATE OR ALTER PROCEDURE sp_Complaint_Delete
    @ComplaintId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE FROM Complaints WHERE ComplaintId = @ComplaintId;
END
GO
