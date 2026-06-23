USE [SmartSociety]
GO

-- 1. Create StaffFlats Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StaffFlats' and xtype='U')
BEGIN
    CREATE TABLE [dbo].[StaffFlats](
        [StaffId] [int] NOT NULL,
        [FlatId] [int] NOT NULL,
        PRIMARY KEY (StaffId, FlatId),
        FOREIGN KEY (StaffId) REFERENCES [dbo].[Staff](StaffId) ON DELETE CASCADE,
        FOREIGN KEY (FlatId) REFERENCES [dbo].[Flats](FlatId) ON DELETE CASCADE
    )
END
GO

-- 2. Create StaffAttendance Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StaffAttendance' and xtype='U')
BEGIN
    CREATE TABLE [dbo].[StaffAttendance](
        [AttendanceId] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [StaffId] [int] NOT NULL,
        [Date] [date] NOT NULL,
        [CheckInTime] [time](7) NULL,
        [CheckOutTime] [time](7) NULL,
        FOREIGN KEY (StaffId) REFERENCES [dbo].[Staff](StaffId) ON DELETE CASCADE
    )
END
GO

-- 3. Stored Procedures for Flats
CREATE OR ALTER PROCEDURE [dbo].[sp_Staff_AssignFlats]
    @StaffId INT,
    @FlatIds NVARCHAR(MAX) -- Comma-separated Flat IDs
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Delete existing assignments
    DELETE FROM [dbo].[StaffFlats] WHERE StaffId = @StaffId;

    -- Insert new assignments if provided
    IF @FlatIds IS NOT NULL AND LEN(@FlatIds) > 0
    BEGIN
        INSERT INTO [dbo].[StaffFlats] (StaffId, FlatId)
        SELECT @StaffId, CAST(value AS INT)
        FROM STRING_SPLIT(@FlatIds, ',');
    END
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Staff_GetAssignedFlats]
    @StaffId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT FlatId FROM [dbo].[StaffFlats] WHERE StaffId = @StaffId;
END
GO

-- 4. Stored Procedures for Attendance
CREATE OR ALTER PROCEDURE [dbo].[sp_Staff_LogAttendance]
    @StaffId INT,
    @LogType NVARCHAR(10) -- 'IN' or 'OUT'
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Today DATE = CAST(GETDATE() AS DATE);
    DECLARE @Now TIME = CAST(GETDATE() AS TIME);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[StaffAttendance] WHERE StaffId = @StaffId AND Date = @Today)
    BEGIN
        -- Insert new record for today
        IF @LogType = 'IN'
            INSERT INTO [dbo].[StaffAttendance] (StaffId, Date, CheckInTime) VALUES (@StaffId, @Today, @Now);
        ELSE
            INSERT INTO [dbo].[StaffAttendance] (StaffId, Date, CheckOutTime) VALUES (@StaffId, @Today, @Now);
    END
    ELSE
    BEGIN
        -- Update existing record
        IF @LogType = 'IN'
            UPDATE [dbo].[StaffAttendance] SET CheckInTime = @Now WHERE StaffId = @StaffId AND Date = @Today AND CheckInTime IS NULL;
        ELSE
            UPDATE [dbo].[StaffAttendance] SET CheckOutTime = @Now WHERE StaffId = @StaffId AND Date = @Today;
    END
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Staff_GetAttendance]
    @StaffId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT top 30 * FROM [dbo].[StaffAttendance] 
    WHERE StaffId = @StaffId 
    ORDER BY Date DESC;
END
GO
