USE SmartSociety;
GO

-- 1. Create Complaints Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Complaints')
BEGIN
    CREATE TABLE Complaints (
        ComplaintId INT IDENTITY(1,1) PRIMARY KEY,
        FlatId INT NOT NULL,
        RaisedBy INT NOT NULL, -- User who raised it (Resident)
        Category VARCHAR(50) NOT NULL, -- Plumbing, Electrical, Cleaning, Security, Lift, Other
        Title VARCHAR(100) NOT NULL,
        Description NVARCHAR(MAX) NOT NULL,
        Priority VARCHAR(20) NOT NULL, -- Low, Medium, High, Emergency
        Status VARCHAR(20) NOT NULL DEFAULT 'Open', -- Open, InProgress, Resolved
        AssignedTo INT NULL, -- Technician or Admin
        AdminRemarks NVARCHAR(MAX) NULL,
        PhotoUrl VARCHAR(255) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        ResolvedAt DATETIME NULL,
        
        FOREIGN KEY (FlatId) REFERENCES Flats(FlatId),
        FOREIGN KEY (RaisedBy) REFERENCES Users(UserId),
        FOREIGN KEY (AssignedTo) REFERENCES Users(UserId)
    );
END
GO

-- 2. sp_Complaint_GetAll
CREATE OR ALTER PROCEDURE sp_Complaint_GetAll
    @Status VARCHAR(20) = NULL
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

-- 3. sp_Complaint_GetById
CREATE OR ALTER PROCEDURE sp_Complaint_GetById
    @ComplaintId INT
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
    WHERE c.ComplaintId = @ComplaintId;
END
GO

-- 4. sp_Complaint_Create
CREATE OR ALTER PROCEDURE sp_Complaint_Create
    @FlatId INT,
    @RaisedBy INT,
    @Category VARCHAR(50),
    @Title VARCHAR(100),
    @Description NVARCHAR(MAX),
    @Priority VARCHAR(20),
    @PhotoUrl VARCHAR(255) = NULL,
    @ComplaintId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO Complaints (FlatId, RaisedBy, Category, Title, Description, Priority, PhotoUrl)
    VALUES (@FlatId, @RaisedBy, @Category, @Title, @Description, @Priority, @PhotoUrl);
    
    SET @ComplaintId = SCOPE_IDENTITY();
END
GO

-- 5. sp_Complaint_UpdateStatus
CREATE OR ALTER PROCEDURE sp_Complaint_UpdateStatus
    @ComplaintId INT,
    @Status VARCHAR(20),
    @AdminRemarks NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE Complaints
    SET Status = @Status,
        AdminRemarks = ISNULL(@AdminRemarks, AdminRemarks),
        UpdatedAt = GETDATE(),
        ResolvedAt = CASE WHEN @Status = 'Resolved' THEN GETDATE() ELSE ResolvedAt END
    WHERE ComplaintId = @ComplaintId;
END
GO

-- 6. sp_Complaint_Assign
CREATE OR ALTER PROCEDURE sp_Complaint_Assign
    @ComplaintId INT,
    @AssignedTo INT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE Complaints
    SET AssignedTo = @AssignedTo,
        Status = CASE WHEN Status = 'Open' THEN 'InProgress' ELSE Status END,
        UpdatedAt = GETDATE()
    WHERE ComplaintId = @ComplaintId;
END
GO

-- 7. sp_Complaint_GetDashboardStats
CREATE OR ALTER PROCEDURE sp_Complaint_GetDashboardStats
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT
        (SELECT COUNT(*) FROM Complaints) AS TotalComplaints,
        (SELECT COUNT(*) FROM Complaints WHERE Status = 'Open') AS OpenComplaints,
        (SELECT COUNT(*) FROM Complaints WHERE Status = 'InProgress') AS InProgressComplaints,
        (SELECT COUNT(*) FROM Complaints WHERE Status = 'Resolved') AS ResolvedComplaints,
        (SELECT COUNT(*) FROM Complaints WHERE Priority IN ('Emergency', 'High') AND Status != 'Resolved') AS HighPriorityPending;
END
GO
