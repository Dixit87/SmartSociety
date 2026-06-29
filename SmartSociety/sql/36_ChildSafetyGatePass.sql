USE [SmartSociety];
GO

-- 1. Create ChildExitRequests Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ChildExitRequests]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ChildExitRequests](
        [RequestId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [FlatId] INT NOT NULL,
        [FamilyMemberId] INT NOT NULL,
        [Status] NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Approved, Rejected
        [GuardRemarks] NVARCHAR(255) NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [ActionedAt] DATETIME NULL,
        CONSTRAINT [FK_ChildExitRequests_Flats] FOREIGN KEY([FlatId]) REFERENCES [dbo].[Flats] ([FlatId]) ON DELETE CASCADE,
        CONSTRAINT [FK_ChildExitRequests_FamilyMembers] FOREIGN KEY([FamilyMemberId]) REFERENCES [dbo].[FamilyMembers] ([FamilyMemberId]) ON DELETE CASCADE
    );
END
GO
