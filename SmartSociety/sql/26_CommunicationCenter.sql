USE [SmartSociety];
GO

-- 1. Create MessageLogs Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MessageLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MessageLogs](
        [MessageId] [int] IDENTITY(1,1) NOT NULL,
        [MessageType] [nvarchar](50) NOT NULL, -- Email, SMS, Emergency Alert
        [Audience] [nvarchar](100) NOT NULL, -- All Residents, Tower A, Committee, etc.
        [Subject] [nvarchar](250) NULL, -- Optional for SMS
        [Body] [nvarchar](max) NOT NULL,
        [SentAt] [datetime] NOT NULL DEFAULT GETDATE(),
        [Status] [nvarchar](50) NOT NULL DEFAULT 'Sent',
     CONSTRAINT [PK_MessageLogs] PRIMARY KEY CLUSTERED ([MessageId] ASC)
    )
END
GO

-- 2. Stored Procedure: GetAll
CREATE OR ALTER PROCEDURE sp_MessageLogs_GetAll
AS
BEGIN
    SELECT 
        MessageId, MessageType, Audience, Subject, Body, SentAt, Status
    FROM MessageLogs
    ORDER BY SentAt DESC;
END
GO

-- 3. Stored Procedure: Insert
CREATE OR ALTER PROCEDURE sp_MessageLogs_Insert
    @MessageType NVARCHAR(50),
    @Audience NVARCHAR(100),
    @Subject NVARCHAR(250) = NULL,
    @Body NVARCHAR(MAX),
    @Status NVARCHAR(50) = 'Sent'
AS
BEGIN
    INSERT INTO MessageLogs (MessageType, Audience, Subject, Body, SentAt, Status)
    VALUES (@MessageType, @Audience, @Subject, @Body, GETDATE(), @Status);
    
    SELECT SCOPE_IDENTITY();
END
GO
