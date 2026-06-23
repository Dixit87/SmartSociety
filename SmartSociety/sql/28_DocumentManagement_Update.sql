USE [SmartSociety];
GO

-- Stored Procedure: Update Document Metadata
CREATE OR ALTER PROCEDURE sp_Documents_Update
    @DocumentId INT,
    @Title NVARCHAR(200),
    @Category NVARCHAR(100),
    @IsVisibleToResidents BIT
AS
BEGIN
    UPDATE SocietyDocuments 
    SET 
        Title = @Title,
        Category = @Category,
        IsVisibleToResidents = @IsVisibleToResidents
    WHERE DocumentId = @DocumentId;
END
GO
