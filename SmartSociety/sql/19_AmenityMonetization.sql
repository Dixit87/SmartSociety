USE SmartSociety;
GO

-- 1. Alter Amenities Table to add PricePerHour
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Amenities') AND name = 'PricePerHour')
BEGIN
    ALTER TABLE Amenities ADD PricePerHour DECIMAL(18,2) NOT NULL DEFAULT 0;
END
GO

-- 2. Alter AmenityBookings Table to add TotalAmount and PaymentStatus
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AmenityBookings') AND name = 'TotalAmount')
BEGIN
    ALTER TABLE AmenityBookings ADD TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0;
    ALTER TABLE AmenityBookings ADD PaymentStatus VARCHAR(20) NOT NULL DEFAULT 'Pending'; -- Pending, Paid, Refunded
END
GO

-- 3. Update Amenity SPs
CREATE OR ALTER PROCEDURE sp_Amenity_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        AmenityId, Name, Description, Capacity, OpenTime, CloseTime, PricePerHour, IsActive, CreatedAt 
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
    @PricePerHour DECIMAL(18,2),
    @IsActive BIT,
    @AmenityId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Amenities (Name, Description, Capacity, OpenTime, CloseTime, PricePerHour, IsActive)
    VALUES (@Name, @Description, @Capacity, @OpenTime, @CloseTime, @PricePerHour, @IsActive);
    
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
    @PricePerHour DECIMAL(18,2),
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
        PricePerHour = @PricePerHour,
        IsActive = @IsActive
    WHERE AmenityId = @AmenityId;
END
GO

-- 4. Update Booking SPs
CREATE OR ALTER PROCEDURE sp_Booking_GetAll
    @Status VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        b.BookingId, b.AmenityId, b.FlatId, b.UserId, b.BookingDate, 
        b.StartTime, b.EndTime, b.Purpose, b.Status, b.Remarks, b.TotalAmount, b.PaymentStatus, b.CreatedAt,
        a.Name AS AmenityName,
        f.FlatNumber AS FlatNo,
        u.FullName AS ResidentName,
        u.PhoneNumber AS ResidentPhone
    FROM AmenityBookings b
    INNER JOIN Amenities a ON b.AmenityId = a.AmenityId
    INNER JOIN Flats f ON b.FlatId = f.FlatId
    INNER JOIN Users u ON b.UserId = u.UserId
    WHERE (@Status IS NULL OR b.Status = @Status)
    ORDER BY b.BookingDate DESC, b.StartTime DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_Booking_Create
    @AmenityId INT,
    @FlatId INT,
    @UserId INT,
    @BookingDate DATE,
    @StartTime TIME,
    @EndTime TIME,
    @Purpose NVARCHAR(255),
    @TotalAmount DECIMAL(18,2),
    @PaymentStatus VARCHAR(20) = 'Pending',
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

    INSERT INTO AmenityBookings (AmenityId, FlatId, UserId, BookingDate, StartTime, EndTime, Purpose, TotalAmount, PaymentStatus, Status)
    VALUES (@AmenityId, @FlatId, @UserId, @BookingDate, @StartTime, @EndTime, @Purpose, @TotalAmount, @PaymentStatus, @Status);
    
    SET @BookingId = SCOPE_IDENTITY();
END
GO
