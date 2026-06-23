USE [SmartSociety];
GO

CREATE OR ALTER PROCEDURE sp_Documents_Update
    @DocumentId INT,
    @Title NVARCHAR(200),
    @Category NVARCHAR(100),
    @FilePath NVARCHAR(500) = NULL,
    @IsVisibleToResidents BIT
AS
BEGIN
    IF @FilePath IS NOT NULL
    BEGIN
        UPDATE SocietyDocuments 
        SET 
            Title = @Title,
            Category = @Category,
            FilePath = @FilePath,
            IsVisibleToResidents = @IsVisibleToResidents
        WHERE DocumentId = @DocumentId;
    END
    ELSE
    BEGIN
        UPDATE SocietyDocuments 
        SET 
            Title = @Title,
            Category = @Category,
            IsVisibleToResidents = @IsVisibleToResidents
        WHERE DocumentId = @DocumentId;
    END
END
GO
