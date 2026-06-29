-- CREATE TABLES

-- 1. Classifieds Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Classifieds')
BEGIN
    CREATE TABLE Classifieds (
        ClassifiedId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
        Title NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NOT NULL,
        Price DECIMAL(18,2) NOT NULL,
        AdCategory NVARCHAR(50) NOT NULL,
        AdType NVARCHAR(20) NOT NULL,
        ImagePath NVARCHAR(500) NULL,
        IsActive BIT DEFAULT 1 NOT NULL,
        CreatedAt DATETIME DEFAULT GETDATE() NOT NULL
    );
END;

-- 2. Forum Topics Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ForumTopics')
BEGIN
    CREATE TABLE ForumTopics (
        TopicId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
        Title NVARCHAR(200) NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        Category NVARCHAR(50) NOT NULL,
        IsPinned BIT DEFAULT 0 NOT NULL,
        CreatedAt DATETIME DEFAULT GETDATE() NOT NULL
    );
END;

-- 3. Forum Replies Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ForumReplies')
BEGIN
    CREATE TABLE ForumReplies (
        ReplyId INT IDENTITY(1,1) PRIMARY KEY,
        TopicId INT NOT NULL FOREIGN KEY REFERENCES ForumTopics(TopicId) ON DELETE CASCADE,
        UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
        Content NVARCHAR(MAX) NOT NULL,
        CreatedAt DATETIME DEFAULT GETDATE() NOT NULL
    );
END;

-- 4. Staff Ratings Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StaffRatings')
BEGIN
    CREATE TABLE StaffRatings (
        RatingId INT IDENTITY(1,1) PRIMARY KEY,
        StaffId INT NOT NULL FOREIGN KEY REFERENCES Staff(StaffId),
        UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
        Rating INT NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
        Review NVARCHAR(1000) NULL,
        CreatedAt DATETIME DEFAULT GETDATE() NOT NULL
    );
END;
GO

-- CREATE STORED PROCEDURES

-- CLASSIFIEDS PROCEDURES
CREATE OR ALTER PROCEDURE sp_Classifieds_GetAllActive
AS
BEGIN
    SELECT c.*, u.FullName AS OwnerName, u.PhoneNumber AS OwnerPhone, u.FlatNumber AS OwnerFlatNumber
    FROM Classifieds c
    INNER JOIN Users u ON c.UserId = u.UserId
    WHERE c.IsActive = 1
    ORDER BY c.CreatedAt DESC;
END;
GO

CREATE OR ALTER PROCEDURE sp_Classifieds_GetById
    @ClassifiedId INT
AS
BEGIN
    SELECT c.*, u.FullName AS OwnerName, u.PhoneNumber AS OwnerPhone, u.FlatNumber AS OwnerFlatNumber
    FROM Classifieds c
    INNER JOIN Users u ON c.UserId = u.UserId
    WHERE c.ClassifiedId = @ClassifiedId;
END;
GO

CREATE OR ALTER PROCEDURE sp_Classifieds_GetByUserId
    @UserId INT
AS
BEGIN
    SELECT c.*, u.FullName AS OwnerName, u.PhoneNumber AS OwnerPhone, u.FlatNumber AS OwnerFlatNumber
    FROM Classifieds c
    INNER JOIN Users u ON c.UserId = u.UserId
    WHERE c.UserId = @UserId
    ORDER BY c.CreatedAt DESC;
END;
GO

CREATE OR ALTER PROCEDURE sp_Classifieds_Upsert
    @ClassifiedId INT,
    @UserId INT,
    @Title NVARCHAR(200),
    @Description NVARCHAR(MAX),
    @Price DECIMAL(18,2),
    @AdCategory NVARCHAR(50),
    @AdType NVARCHAR(20),
    @ImagePath NVARCHAR(500),
    @IsActive BIT
AS
BEGIN
    IF EXISTS (SELECT 1 FROM Classifieds WHERE ClassifiedId = @ClassifiedId)
    BEGIN
        UPDATE Classifieds
        SET Title = @Title,
            Description = @Description,
            Price = @Price,
            AdCategory = @AdCategory,
            AdType = @AdType,
            ImagePath = COALESCE(@ImagePath, ImagePath),
            IsActive = @IsActive
        WHERE ClassifiedId = @ClassifiedId;
        
        SELECT @ClassifiedId;
    END
    ELSE
    BEGIN
        INSERT INTO Classifieds (UserId, Title, Description, Price, AdCategory, AdType, ImagePath, IsActive, CreatedAt)
        VALUES (@UserId, @Title, @Description, @Price, @AdCategory, @AdType, @ImagePath, @IsActive, GETDATE());
        
        SELECT SCOPE_IDENTITY();
    END
END;
GO

CREATE OR ALTER PROCEDURE sp_Classifieds_Delete
    @ClassifiedId INT
AS
BEGIN
    DELETE FROM Classifieds WHERE ClassifiedId = @ClassifiedId;
END;
GO

-- FORUM TOPICS PROCEDURES
CREATE OR ALTER PROCEDURE sp_ForumTopics_GetAll
AS
BEGIN
    SELECT t.*, u.FullName AS AuthorName, u.FlatNumber AS AuthorFlat,
           (SELECT COUNT(*) FROM ForumReplies r WHERE r.TopicId = t.TopicId) AS ReplyCount
    FROM ForumTopics t
    INNER JOIN Users u ON t.UserId = u.UserId
    ORDER BY t.IsPinned DESC, t.CreatedAt DESC;
END;
GO

CREATE OR ALTER PROCEDURE sp_ForumTopics_GetById
    @TopicId INT
AS
BEGIN
    SELECT t.*, u.FullName AS AuthorName, u.FlatNumber AS AuthorFlat,
           (SELECT COUNT(*) FROM ForumReplies r WHERE r.TopicId = t.TopicId) AS ReplyCount
    FROM ForumTopics t
    INNER JOIN Users u ON t.UserId = u.UserId
    WHERE t.TopicId = @TopicId;
END;
GO

CREATE OR ALTER PROCEDURE sp_ForumTopics_Upsert
    @TopicId INT,
    @UserId INT,
    @Title NVARCHAR(200),
    @Content NVARCHAR(MAX),
    @Category NVARCHAR(50),
    @IsPinned BIT
AS
BEGIN
    IF EXISTS (SELECT 1 FROM ForumTopics WHERE TopicId = @TopicId)
    BEGIN
        UPDATE ForumTopics
        SET Title = @Title,
            Content = @Content,
            Category = @Category,
            IsPinned = @IsPinned
        WHERE TopicId = @TopicId;
        
        SELECT @TopicId;
    END
    ELSE
    BEGIN
        INSERT INTO ForumTopics (UserId, Title, Content, Category, IsPinned, CreatedAt)
        VALUES (@UserId, @Title, @Content, @Category, @IsPinned, GETDATE());
        
        SELECT SCOPE_IDENTITY();
    END
END;
GO

CREATE OR ALTER PROCEDURE sp_ForumTopics_Delete
    @TopicId INT
AS
BEGIN
    DELETE FROM ForumTopics WHERE TopicId = @TopicId;
END;
GO

-- FORUM REPLIES PROCEDURES
CREATE OR ALTER PROCEDURE sp_ForumReplies_GetByTopicId
    @TopicId INT
AS
BEGIN
    SELECT r.*, u.FullName AS AuthorName, u.FlatNumber AS AuthorFlat
    FROM ForumReplies r
    INNER JOIN Users u ON r.UserId = u.UserId
    WHERE r.TopicId = @TopicId
    ORDER BY r.CreatedAt ASC;
END;
GO

CREATE OR ALTER PROCEDURE sp_ForumReplies_Insert
    @TopicId INT,
    @UserId INT,
    @Content NVARCHAR(MAX)
AS
BEGIN
    INSERT INTO ForumReplies (TopicId, UserId, Content, CreatedAt)
    VALUES (@TopicId, @UserId, @Content, GETDATE());
    
    SELECT SCOPE_IDENTITY();
END;
GO

CREATE OR ALTER PROCEDURE sp_ForumReplies_Delete
    @ReplyId INT
AS
BEGIN
    DELETE FROM ForumReplies WHERE ReplyId = @ReplyId;
END;
GO

-- STAFF RATINGS PROCEDURES
CREATE OR ALTER PROCEDURE sp_StaffRatings_GetByStaffId
    @StaffId INT
AS
BEGIN
    SELECT r.*, u.FullName AS ReviewerName, u.FlatNumber AS ReviewerFlat
    FROM StaffRatings r
    INNER JOIN Users u ON r.UserId = u.UserId
    WHERE r.StaffId = @StaffId
    ORDER BY r.CreatedAt DESC;
END;
GO

CREATE OR ALTER PROCEDURE sp_StaffRatings_Add
    @StaffId INT,
    @UserId INT,
    @Rating INT,
    @Review NVARCHAR(1000)
AS
BEGIN
    INSERT INTO StaffRatings (StaffId, UserId, Rating, Review, CreatedAt)
    VALUES (@StaffId, @UserId, @Rating, @Review, GETDATE());
    
    SELECT SCOPE_IDENTITY();
END;
GO

CREATE OR ALTER PROCEDURE sp_StaffRatings_GetAverageRating
    @StaffId INT
AS
BEGIN
    SELECT 
        COALESCE(AVG(CAST(Rating AS DECIMAL(3,2))), 0.0) AS AvgRating, 
        COUNT(*) AS RatingCount
    FROM StaffRatings
    WHERE StaffId = @StaffId;
END;
GO
