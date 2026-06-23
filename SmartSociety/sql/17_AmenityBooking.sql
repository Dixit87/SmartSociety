USE SmartSociety;
GO

-- 1. Create Amenities Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Amenities')
BEGIN
    CREATE TABLE Amenities (
        AmenityId INT IDENTITY(1,1) PRIMARY KEY,
        Name VARCHAR(100) NOT NULL,
        Description NVARCHAR(500),
        Capacity INT NOT NULL DEFAULT 0,
        Timings VARCHAR(100), -- e.g., "06:00 AM - 10:00 PM"
        IsActive BIT DEFAULT 1,
        CreatedAt DATETIME DEFAULT GETDATE()
    );
END
GO

-- 2. Create AmenityBookings Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AmenityBookings')
BEGIN
    CREATE TABLE AmenityBookings (
        BookingId INT IDENTITY(1,1) PRIMARY KEY,
        AmenityId INT NOT NULL FOREIGN KEY REFERENCES Amenities(AmenityId),
        FlatId INT NOT NULL FOREIGN KEY REFERENCES Flats(FlatId),
        UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
        BookingDate DATE NOT NULL,
        StartTime TIME NOT NULL,
        EndTime TIME NOT NULL,
        Purpose NVARCHAR(255),
        Status VARCHAR(20) DEFAULT 'Pending', -- Pending, Approved, Rejected, Cancelled
        Remarks NVARCHAR(500), -- For admin rejection reasons etc.
        CreatedAt DATETIME DEFAULT GETDATE()
    );
END
GO

-- 3. Stored Procedures for Amenities
CREATE OR ALTER PROCEDURE sp_Amenity_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        AmenityId, Name, Description, Capacity, Timings, IsActive, CreatedAt 
    FROM Amenities 
    ORDER BY Name ASC;
END
GO

CREATE OR ALTER PROCEDURE sp_Amenity_Create
    @Name VARCHAR(100),
    @Description NVARCHAR(500),
    @Capacity INT,
    @Timings VARCHAR(100),
    @IsActive BIT,
    @AmenityId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Amenities (Name, Description, Capacity, Timings, IsActive)
    VALUES (@Name, @Description, @Capacity, @Timings, @IsActive);
    
    SET @AmenityId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE sp_Amenity_Update
    @AmenityId INT,
    @Name VARCHAR(100),
    @Description NVARCHAR(500),
    @Capacity INT,
    @Timings VARCHAR(100),
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Amenities
    SET Name = @Name,
        Description = @Description,
        Capacity = @Capacity,
        Timings = @Timings,
        IsActive = @IsActive
    WHERE AmenityId = @AmenityId;
END
GO

CREATE OR ALTER PROCEDURE sp_Amenity_Delete
    @AmenityId INT
AS
BEGIN
    SET NOCOUNT ON;
    -- Note: Ideally soft delete or check if bookings exist. For now, simple delete if no relations or cascade.
    -- If there are bookings, this will fail due to FK. It's safer to just set IsActive = 0.
    UPDATE Amenities SET IsActive = 0 WHERE AmenityId = @AmenityId;
END
GO

-- 4. Stored Procedures for AmenityBookings
CREATE OR ALTER PROCEDURE sp_Booking_GetAll
    @Status VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        b.BookingId, b.AmenityId, b.FlatId, b.UserId, b.BookingDate, 
        b.StartTime, b.EndTime, b.Purpose, b.Status, b.Remarks, b.CreatedAt,
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
    @Status VARCHAR(20) = 'Pending',
    @BookingId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO AmenityBookings (AmenityId, FlatId, UserId, BookingDate, StartTime, EndTime, Purpose, Status)
    VALUES (@AmenityId, @FlatId, @UserId, @BookingDate, @StartTime, @EndTime, @Purpose, @Status);
    
    SET @BookingId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE sp_Booking_UpdateStatus
    @BookingId INT,
    @Status VARCHAR(20),
    @Remarks NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE AmenityBookings
    SET Status = @Status,
        Remarks = ISNULL(@Remarks, Remarks)
    WHERE BookingId = @BookingId;
END
GO
