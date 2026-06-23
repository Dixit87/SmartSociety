USE SmartSociety;
GO

-- 1. Alter Amenities Table to replace Timings string with TIME columns
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Amenities') AND name = 'Timings')
BEGIN
    ALTER TABLE Amenities ADD OpenTime TIME NULL;
    ALTER TABLE Amenities ADD CloseTime TIME NULL;
    
    -- Default open and close times
    EXEC('UPDATE Amenities SET OpenTime = ''06:00:00'', CloseTime = ''22:00:00''');
    
    ALTER TABLE Amenities ALTER COLUMN OpenTime TIME NOT NULL;
    ALTER TABLE Amenities ALTER COLUMN CloseTime TIME NOT NULL;

    ALTER TABLE Amenities DROP COLUMN Timings;
END
GO

-- 2. Update Amenity SPs
CREATE OR ALTER PROCEDURE sp_Amenity_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        AmenityId, Name, Description, Capacity, OpenTime, CloseTime, IsActive, CreatedAt 
    FROM Amenities 
    ORDER BY Name ASC;
END
GO

CREATE OR ALTER PROCEDURE sp_Amenity_Create
    @Name VARCHAR(100),
    @Description NVARCHAR(500),
    @Capacity INT,
    @OpenTime TIME,
    @CloseTime TIME,
    @IsActive BIT,
    @AmenityId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Amenities (Name, Description, Capacity, OpenTime, CloseTime, IsActive)
    VALUES (@Name, @Description, @Capacity, @OpenTime, @CloseTime, @IsActive);
    
    SET @AmenityId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE sp_Amenity_Update
    @AmenityId INT,
    @Name VARCHAR(100),
    @Description NVARCHAR(500),
    @Capacity INT,
    @OpenTime TIME,
    @CloseTime TIME,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Amenities
    SET Name = @Name,
        Description = @Description,
        Capacity = @Capacity,
        OpenTime = @OpenTime,
        CloseTime = @CloseTime,
        IsActive = @IsActive
    WHERE AmenityId = @AmenityId;
END
GO

-- 3. Update sp_Booking_Create with Validation Logic
CREATE OR ALTER PROCEDURE sp_Booking_Create
    @AmenityId INT,
    @FlatId INT,
    @UserId INT,
    @BookingDate DATE,
    @StartTime TIME,
    @EndTime TIME,
    @Purpose NVARCHAR(255),
    @Status VARCHAR(20) = 'Pending',
    @BookingId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- VALIDATION 1: EndTime must be > StartTime
    IF @EndTime <= @StartTime
    BEGIN
        RAISERROR('End Time must be greater than Start Time.', 16, 1);
        RETURN;
    END
    
    -- VALIDATION 2: Check if facility is open during requested hours
    DECLARE @FacilityOpen TIME, @FacilityClose TIME;
    SELECT @FacilityOpen = OpenTime, @FacilityClose = CloseTime FROM Amenities WHERE AmenityId = @AmenityId;
    
    IF @StartTime < @FacilityOpen OR @EndTime > @FacilityClose
    BEGIN
        RAISERROR('Booking time is outside of facility operating hours.', 16, 1);
        RETURN;
    END

    -- VALIDATION 3: Check for Overlapping Bookings (Pending or Approved)
    IF EXISTS (
        SELECT 1 FROM AmenityBookings 
        WHERE AmenityId = @AmenityId 
        AND BookingDate = @BookingDate
        AND Status IN ('Pending', 'Approved')
        AND (
            (@StartTime >= StartTime AND @StartTime < EndTime) OR 
            (@EndTime > StartTime AND @EndTime <= EndTime) OR
            (@StartTime <= StartTime AND @EndTime >= EndTime)
        )
    )
    BEGIN
        RAISERROR('The facility is already booked for the selected time slot.', 16, 1);
        RETURN;
    END

    INSERT INTO AmenityBookings (AmenityId, FlatId, UserId, BookingDate, StartTime, EndTime, Purpose, Status)
    VALUES (@AmenityId, @FlatId, @UserId, @BookingDate, @StartTime, @EndTime, @Purpose, @Status);
    
    SET @BookingId = SCOPE_IDENTITY();
END
GO
