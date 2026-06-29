USE [SmartSociety];
GO

-- 1. Create Notifications Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Notifications](
        [NotificationId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NOT NULL FOREIGN KEY REFERENCES Users(UserId) ON DELETE CASCADE,
        [Title] NVARCHAR(150) NOT NULL,
        [Message] NVARCHAR(MAX) NOT NULL,
        [Category] NVARCHAR(50) NOT NULL, -- Maintenance, Visitor, Complaint, Notice, General
        [IsRead] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 2. Create FamilyMembers Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FamilyMembers]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[FamilyMembers](
        [FamilyMemberId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NOT NULL FOREIGN KEY REFERENCES Users(UserId) ON DELETE CASCADE,
        [FullName] NVARCHAR(100) NOT NULL,
        [Relation] NVARCHAR(50) NOT NULL, -- Spouse, Child, Parent, Sibling, Other
        [PhoneNumber] NVARCHAR(20) NULL,
        [Email] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 3. Stored Procedures for Notifications
CREATE OR ALTER PROCEDURE sp_Notifications_GetByUserId
    @UserId INT
AS
BEGIN
    SELECT * FROM Notifications
    WHERE UserId = @UserId
    ORDER BY CreatedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_Notifications_Insert
    @UserId INT,
    @Title NVARCHAR(150),
    @Message NVARCHAR(MAX),
    @Category NVARCHAR(50)
AS
BEGIN
    INSERT INTO Notifications (UserId, Title, Message, Category, IsRead, CreatedAt)
    VALUES (@UserId, @Title, @Message, @Category, 0, GETDATE());
    
    SELECT SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE sp_Notifications_MarkAsRead
    @NotificationId INT
AS
BEGIN
    UPDATE Notifications
    SET IsRead = 1
    WHERE NotificationId = @NotificationId;
END
GO

CREATE OR ALTER PROCEDURE sp_Notifications_MarkAllAsRead
    @UserId INT
AS
BEGIN
    UPDATE Notifications
    SET IsRead = 1
    WHERE UserId = @UserId;
END
GO

CREATE OR ALTER PROCEDURE sp_Notifications_Delete
    @NotificationId INT
AS
BEGIN
    DELETE FROM Notifications
    WHERE NotificationId = @NotificationId;
END
GO

-- 4. Stored Procedures for Family Members
CREATE OR ALTER PROCEDURE sp_FamilyMembers_GetByUserId
    @UserId INT
AS
BEGIN
    SELECT * FROM FamilyMembers
    WHERE UserId = @UserId
    ORDER BY CreatedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_FamilyMembers_Upsert
    @FamilyMemberId INT,
    @UserId INT,
    @FullName NVARCHAR(100),
    @Relation NVARCHAR(50),
    @PhoneNumber NVARCHAR(20) = NULL,
    @Email NVARCHAR(100) = NULL
AS
BEGIN
    IF @FamilyMemberId = 0 OR NOT EXISTS (SELECT 1 FROM FamilyMembers WHERE FamilyMemberId = @FamilyMemberId)
    BEGIN
        INSERT INTO FamilyMembers (UserId, FullName, Relation, PhoneNumber, Email, CreatedAt)
        VALUES (@UserId, @FullName, @Relation, @PhoneNumber, @Email, GETDATE());
        
        SELECT SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE FamilyMembers
        SET FullName = @FullName,
            Relation = @Relation,
            PhoneNumber = @PhoneNumber,
            Email = @Email
        WHERE FamilyMemberId = @FamilyMemberId;
        
        SELECT @FamilyMemberId;
    END
END
GO

CREATE OR ALTER PROCEDURE sp_FamilyMembers_Delete
    @FamilyMemberId INT
AS
BEGIN
    DELETE FROM FamilyMembers
    WHERE FamilyMemberId = @FamilyMemberId;
END
GO

-- 5. Seed Mock Data for User with UserId = 2 (Dhyey - Resident)
-- Delete first to ensure repeatability
DELETE FROM Notifications WHERE UserId = 2;
DELETE FROM FamilyMembers WHERE UserId = 2;

-- Insert Mock Family Members
INSERT INTO FamilyMembers (UserId, FullName, Relation, PhoneNumber, Email, CreatedAt)
VALUES 
(2, 'Karan Shah', 'Spouse', '9876543210', 'karan.shah@example.com', GETDATE()),
(2, 'Aarav Shah', 'Child', '9876543211', 'aarav.shah@example.com', GETDATE());

-- Insert Mock Notifications
INSERT INTO Notifications (UserId, Title, Message, Category, IsRead, CreatedAt)
VALUES
(2, 'Maintenance Bill Generated', 'Your maintenance bill for June 2026 of ₹2,500 has been generated. Please pay before 10th July.', 'Maintenance', 0, DATEADD(hour, -2, GETDATE())),
(2, 'Visitor Checked In', 'Visitor John Doe has entered the society for Flat A-102.', 'Visitor', 0, DATEADD(hour, -5, GETDATE())),
(2, 'Complaint In-Progress', 'Your complaint regarding water leakage (Ticket #1024) has been assigned to technician Ramesh.', 'Complaint', 0, DATEADD(day, -1, GETDATE())),
(2, 'New Society Circular', 'Annual General Meeting (AGM) is scheduled for 5th July 2026. Please check the notice board.', 'Notice', 1, DATEADD(day, -2, GETDATE())),
(2, 'Welcome to SmartSociety!', 'Welcome to the new premium resident portal. Complete your profile and family details.', 'General', 1, DATEADD(day, -5, GETDATE()));
GO
