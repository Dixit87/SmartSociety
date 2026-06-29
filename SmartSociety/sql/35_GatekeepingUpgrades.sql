USE [SmartSociety];
GO

-- 1. Alter Visitors table to add InviteCode and ExpiryDate if they don't exist
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'InviteCode' AND Object_ID = Object_ID(N'dbo.Visitors'))
BEGIN
    ALTER TABLE dbo.Visitors ADD InviteCode NVARCHAR(20) NULL;
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'ExpiryDate' AND Object_ID = Object_ID(N'dbo.Visitors'))
BEGIN
    ALTER TABLE dbo.Visitors ADD ExpiryDate DATETIME NULL;
END
GO

-- 2. Create Deliveries table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Deliveries]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Deliveries](
        [DeliveryId] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [FlatId] [int] NOT NULL,
        [Company] [nvarchar](100) NOT NULL,
        [DeliveryAgentName] [nvarchar](100) NOT NULL,
        [DeliveryAgentPhone] [nvarchar](20) NOT NULL,
        [Status] [nvarchar](50) NOT NULL DEFAULT 'LoggedAtGate',
        [ReceiptPhoto] [nvarchar](255) NULL,
        [LoggedAt] [datetime] NOT NULL DEFAULT GETDATE(),
        [CollectedAt] [datetime] NULL,
        CONSTRAINT [FK_Deliveries_Flats] FOREIGN KEY([FlatId]) REFERENCES [dbo].[Flats] ([FlatId]) ON DELETE CASCADE
    );
END
GO
