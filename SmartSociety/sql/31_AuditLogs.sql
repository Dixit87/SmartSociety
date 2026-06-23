USE [SmartSociety];
GO

-- 1. Create AuditLogs Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AuditLogs](
        [LogId] [int] IDENTITY(1,1) NOT NULL,
        [Username] [nvarchar](100) NOT NULL,
        [ActionType] [nvarchar](50) NOT NULL, -- e.g., Create, Update, Delete, Login
        [ModuleName] [nvarchar](100) NOT NULL, -- e.g., Flats, Users, Documents
        [Description] [nvarchar](500) NOT NULL,
        [Timestamp] [datetime] NOT NULL DEFAULT GETDATE(),
        [IPAddress] [nvarchar](50) NULL,
     CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED ([LogId] ASC)
    )
END
GO

-- 2. Stored Procedure: Insert
CREATE OR ALTER PROCEDURE sp_AuditLogs_Insert
    @Username NVARCHAR(100),
    @ActionType NVARCHAR(50),
    @ModuleName NVARCHAR(100),
    @Description NVARCHAR(500),
    @IPAddress NVARCHAR(50) = NULL
AS
BEGIN
    INSERT INTO AuditLogs (Username, ActionType, ModuleName, Description, Timestamp, IPAddress)
    VALUES (@Username, @ActionType, @ModuleName, @Description, GETDATE(), @IPAddress);
END
GO

-- 3. Stored Procedure: GetAll (Latest First)
CREATE OR ALTER PROCEDURE sp_AuditLogs_GetAll
AS
BEGIN
    SELECT 
        LogId, Username, ActionType, ModuleName, Description, Timestamp, IPAddress
    FROM AuditLogs
    ORDER BY Timestamp DESC;
END
GO

-- 4. Insert Dummy Data for Demo Purposes
IF NOT EXISTS (SELECT 1 FROM AuditLogs)
BEGIN
    INSERT INTO AuditLogs (Username, ActionType, ModuleName, Description, Timestamp, IPAddress) VALUES
    ('Admin', 'Login', 'Authentication', 'Admin logged into the portal.', DATEADD(hour, -24, GETDATE()), '192.168.1.10'),
    ('Admin', 'Create', 'Document Management', 'Uploaded new audit report: Audit_2025.pdf', DATEADD(hour, -23, GETDATE()), '192.168.1.10'),
    ('John Doe', 'Update', 'Complaint Helpdesk', 'Updated complaint #102 status to In Progress', DATEADD(hour, -20, GETDATE()), '10.0.0.5'),
    ('Admin', 'Delete', 'Visitor Logs', 'Deleted spam visitor entry ID: 450', DATEADD(hour, -18, GETDATE()), '192.168.1.10'),
    ('Admin', 'Create', 'Polls & Voting', 'Created new poll: Society Color Scheme', DATEADD(hour, -10, GETDATE()), '192.168.1.10'),
    ('Jane Smith', 'Login', 'Authentication', 'Resident logged into the portal.', DATEADD(hour, -5, GETDATE()), '172.16.0.4'),
    ('Admin', 'Update', 'Reports & Analytics', 'Exported Financial Audit CSV', DATEADD(minute, -30, GETDATE()), '192.168.1.10');
END
GO
