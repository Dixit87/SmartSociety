USE [SmartSociety];
GO

-- 1. Create Polls Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Polls]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Polls](
        [PollId] [int] IDENTITY(1,1) NOT NULL,
        [Question] [nvarchar](500) NOT NULL,
        [Description] [nvarchar](max) NULL,
        [StartDate] [datetime] NOT NULL DEFAULT GETDATE(),
        [EndDate] [datetime] NOT NULL,
        [Status] [nvarchar](50) NOT NULL DEFAULT 'Active', -- Active, Closed
        [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE(),
     CONSTRAINT [PK_Polls] PRIMARY KEY CLUSTERED ([PollId] ASC)
    )
END
GO

-- 2. Create PollOptions Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PollOptions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PollOptions](
        [OptionId] [int] IDENTITY(1,1) NOT NULL,
        [PollId] [int] NOT NULL,
        [OptionText] [nvarchar](250) NOT NULL,
     CONSTRAINT [PK_PollOptions] PRIMARY KEY CLUSTERED ([OptionId] ASC),
     CONSTRAINT [FK_PollOptions_Polls] FOREIGN KEY([PollId]) REFERENCES [dbo].[Polls] ([PollId]) ON DELETE CASCADE
    )
END
GO

-- 3. Create PollVotes Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PollVotes]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PollVotes](
        [VoteId] [int] IDENTITY(1,1) NOT NULL,
        [PollId] [int] NOT NULL,
        [OptionId] [int] NOT NULL,
        [UserId] [int] NOT NULL, -- To prevent multiple votes from same user
        [VotedAt] [datetime] NOT NULL DEFAULT GETDATE(),
     CONSTRAINT [PK_PollVotes] PRIMARY KEY CLUSTERED ([VoteId] ASC),
     CONSTRAINT [FK_PollVotes_Polls] FOREIGN KEY([PollId]) REFERENCES [dbo].[Polls] ([PollId]) ON DELETE NO ACTION, -- Cascade is on Options, we prevent multiple cascade paths
     CONSTRAINT [FK_PollVotes_Options] FOREIGN KEY([OptionId]) REFERENCES [dbo].[PollOptions] ([OptionId]) ON DELETE CASCADE
    )
END
GO

-- 4. Stored Procedure to Update Poll Status (Auto-close expired polls)
CREATE OR ALTER PROCEDURE sp_Polls_AutoClose
AS
BEGIN
    UPDATE Polls
    SET Status = 'Closed'
    WHERE EndDate < GETDATE() AND Status = 'Active';
END
GO

-- 5. Stored Procedure: GetAll
CREATE OR ALTER PROCEDURE sp_Polls_GetAll
AS
BEGIN
    -- First, close any expired polls
    EXEC sp_Polls_AutoClose;

    -- Return polls with total vote count
    SELECT 
        p.PollId, p.Question, p.Description, p.StartDate, p.EndDate, p.Status, p.CreatedAt,
        (SELECT COUNT(*) FROM PollVotes v WHERE v.PollId = p.PollId) AS TotalVotes
    FROM Polls p
    ORDER BY p.CreatedAt DESC;
END
GO

-- 6. Stored Procedure: GetById (Includes Options)
CREATE OR ALTER PROCEDURE sp_Polls_GetById
    @PollId INT
AS
BEGIN
    -- Poll Data
    SELECT * FROM Polls WHERE PollId = @PollId;
    
    -- Options Data with their individual vote counts
    SELECT 
        o.OptionId, o.PollId, o.OptionText,
        (SELECT COUNT(*) FROM PollVotes v WHERE v.OptionId = o.OptionId) AS VoteCount
    FROM PollOptions o
    WHERE o.PollId = @PollId;
END
GO

-- 7. Stored Procedure: Upsert Poll
CREATE OR ALTER PROCEDURE sp_Polls_Upsert
    @PollId INT = 0,
    @Question NVARCHAR(500),
    @Description NVARCHAR(MAX) = NULL,
    @EndDate DATETIME
AS
BEGIN
    IF @PollId = 0
    BEGIN
        INSERT INTO Polls (Question, Description, StartDate, EndDate, Status)
        VALUES (@Question, @Description, GETDATE(), @EndDate, 'Active');
        
        SELECT SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        -- Admin usually shouldn't edit question/options after people voted, but we allow basic edits
        UPDATE Polls SET 
            Question = @Question,
            Description = @Description,
            EndDate = @EndDate
        WHERE PollId = @PollId;
        
        SELECT @PollId;
    END
END
GO

-- 8. Stored Procedure: Insert Option
CREATE OR ALTER PROCEDURE sp_PollOptions_Insert
    @PollId INT,
    @OptionText NVARCHAR(250)
AS
BEGIN
    INSERT INTO PollOptions (PollId, OptionText)
    VALUES (@PollId, @OptionText);
END
GO

-- 9. Stored Procedure: Delete Poll
CREATE OR ALTER PROCEDURE sp_Polls_Delete
    @PollId INT
AS
BEGIN
    -- Options and Votes are CASCADE deleted
    DELETE FROM Polls WHERE PollId = @PollId;
END
GO

-- 10. Stored Procedure: Mock Vote
CREATE OR ALTER PROCEDURE sp_Polls_MockVote
    @PollId INT,
    @OptionId INT
AS
BEGIN
    -- Since it's a mock vote, we just generate a random UserId (from 1000 to 9999) to bypass any unique user constraints if added later
    DECLARE @RandomUserId INT = (SELECT CAST(RAND() * 9000 + 1000 AS INT));
    
    INSERT INTO PollVotes (PollId, OptionId, UserId, VotedAt)
    VALUES (@PollId, @OptionId, @RandomUserId, GETDATE());
END
GO
