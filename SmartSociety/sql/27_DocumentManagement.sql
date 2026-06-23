USE [SmartSociety];
GO

-- 1. Create SocietyDocuments Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SocietyDocuments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SocietyDocuments](
        [DocumentId] [int] IDENTITY(1,1) NOT NULL,
        [Title] [nvarchar](200) NOT NULL,
        [Category] [nvarchar](100) NOT NULL, -- e.g., Audit Reports, Society Rules, Forms, Minutes of Meeting
        [FilePath] [nvarchar](500) NOT NULL,
        [UploadedAt] [datetime] NOT NULL DEFAULT GETDATE(),
        [IsVisibleToResidents] [bit] NOT NULL DEFAULT 0,
     CONSTRAINT [PK_SocietyDocuments] PRIMARY KEY CLUSTERED ([DocumentId] ASC)
    )
END
GO

-- 2. Stored Procedure: GetAll
CREATE OR ALTER PROCEDURE sp_Documents_GetAll
AS
BEGIN
    SELECT 
        DocumentId, Title, Category, FilePath, UploadedAt, IsVisibleToResidents
    FROM SocietyDocuments
    ORDER BY UploadedAt DESC;
END
GO

-- 3. Stored Procedure: Insert
CREATE OR ALTER PROCEDURE sp_Documents_Insert
    @Title NVARCHAR(200),
    @Category NVARCHAR(100),
    @FilePath NVARCHAR(500),
    @IsVisibleToResidents BIT
AS
BEGIN
    INSERT INTO SocietyDocuments (Title, Category, FilePath, UploadedAt, IsVisibleToResidents)
    VALUES (@Title, @Category, @FilePath, GETDATE(), @IsVisibleToResidents);
    
    SELECT SCOPE_IDENTITY();
END
GO

-- 4. Stored Procedure: Delete
CREATE OR ALTER PROCEDURE sp_Documents_Delete
    @DocumentId INT
AS
BEGIN
    DELETE FROM SocietyDocuments WHERE DocumentId = @DocumentId;
END
GO

-- 5. Stored Procedure: Get By Id (To get filepath for physical deletion)
CREATE OR ALTER PROCEDURE sp_Documents_GetById
    @DocumentId INT
AS
BEGIN
    SELECT * FROM SocietyDocuments WHERE DocumentId = @DocumentId;
END
GO
