USE [SmartSociety]
GO

-- 1. Create Blocks Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Blocks')
BEGIN
    CREATE TABLE Blocks (
        BlockId INT IDENTITY(1,1) PRIMARY KEY,
        BlockName NVARCHAR(50) NOT NULL,
        TotalFloors INT NOT NULL,
        CreatedAt DATETIME DEFAULT GETDATE()
    );
END
GO

-- 2. Create Flats Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Flats')
BEGIN
    CREATE TABLE Flats (
        FlatId INT IDENTITY(1,1) PRIMARY KEY,
        BlockId INT NOT NULL FOREIGN KEY REFERENCES Blocks(BlockId),
        FlatNumber NVARCHAR(50) NOT NULL,
        FloorNumber INT NOT NULL,
        FlatType NVARCHAR(50) NOT NULL,
        AreaSqFt DECIMAL(10,2) NOT NULL,
        OwnerId INT NULL FOREIGN KEY REFERENCES Users(UserId),
        TenantId INT NULL FOREIGN KEY REFERENCES Users(UserId),
        IsActive BIT DEFAULT 1,
        CreatedAt DATETIME DEFAULT GETDATE()
    );
END
GO

-- 3. Stored Procedures for Blocks
CREATE OR ALTER PROCEDURE sp_Blocks_GetAll
AS
BEGIN
    SELECT * FROM Blocks ORDER BY BlockName;
END
GO

CREATE OR ALTER PROCEDURE sp_Blocks_Upsert
    @BlockId INT,
    @BlockName NVARCHAR(50),
    @TotalFloors INT
AS
BEGIN
    IF @BlockId = 0 OR NOT EXISTS (SELECT 1 FROM Blocks WHERE BlockId = @BlockId)
    BEGIN
        INSERT INTO Blocks (BlockName, TotalFloors, CreatedAt)
        VALUES (@BlockName, @TotalFloors, GETDATE());
        SELECT SCOPE_IDENTITY() AS BlockId;
    END
    ELSE
    BEGIN
        UPDATE Blocks
        SET BlockName = @BlockName,
            TotalFloors = @TotalFloors
        WHERE BlockId = @BlockId;
        SELECT @BlockId AS BlockId;
    END
END
GO

CREATE OR ALTER PROCEDURE sp_Blocks_Delete
    @BlockId INT
AS
BEGIN
    DELETE FROM Blocks WHERE BlockId = @BlockId;
END
GO

-- 4. Stored Procedures for Flats
CREATE OR ALTER PROCEDURE sp_Flats_GetAll
AS
BEGIN
    SELECT 
        f.*,
        b.BlockName,
        o.FullName AS OwnerName,
        t.FullName AS TenantName
    FROM Flats f
    INNER JOIN Blocks b ON f.BlockId = b.BlockId
    LEFT JOIN Users o ON f.OwnerId = o.UserId
    LEFT JOIN Users t ON f.TenantId = t.UserId
    ORDER BY b.BlockName, f.FloorNumber, f.FlatNumber;
END
GO

CREATE OR ALTER PROCEDURE sp_Flats_GetById
    @FlatId INT
AS
BEGIN
    SELECT * FROM Flats WHERE FlatId = @FlatId;
END
GO

CREATE OR ALTER PROCEDURE sp_Flats_Upsert
    @FlatId INT,
    @BlockId INT,
    @FlatNumber NVARCHAR(50),
    @FloorNumber INT,
    @FlatType NVARCHAR(50),
    @AreaSqFt DECIMAL(10,2),
    @OwnerId INT = NULL,
    @TenantId INT = NULL,
    @IsActive BIT
AS
BEGIN
    IF @FlatId = 0 OR NOT EXISTS (SELECT 1 FROM Flats WHERE FlatId = @FlatId)
    BEGIN
        INSERT INTO Flats (BlockId, FlatNumber, FloorNumber, FlatType, AreaSqFt, OwnerId, TenantId, IsActive, CreatedAt)
        VALUES (@BlockId, @FlatNumber, @FloorNumber, @FlatType, @AreaSqFt, @OwnerId, @TenantId, @IsActive, GETDATE());
        SELECT SCOPE_IDENTITY() AS FlatId;
    END
    ELSE
    BEGIN
        UPDATE Flats
        SET BlockId = @BlockId,
            FlatNumber = @FlatNumber,
            FloorNumber = @FloorNumber,
            FlatType = @FlatType,
            AreaSqFt = @AreaSqFt,
            OwnerId = @OwnerId,
            TenantId = @TenantId,
            IsActive = @IsActive
        WHERE FlatId = @FlatId;
        SELECT @FlatId AS FlatId;
    END
END
GO

CREATE OR ALTER PROCEDURE sp_Flats_Delete
    @FlatId INT
AS
BEGIN
    DELETE FROM Flats WHERE FlatId = @FlatId;
END
GO
