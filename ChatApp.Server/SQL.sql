CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(100) NOT NULL UNIQUE,
    Password NVARCHAR(256) NOT NULL,
    Nickname NVARCHAR(100) NOT NULL,
);

CREATE TABLE Chats (
    ChatId INT PRIMARY KEY IDENTITY(1,1),
    ChatName NVARCHAR(100) NOT NULL,
    IsGroup BIT NOT NULL,
    LastUpdated DATETIME NOT NULL,
    CONSTRAINT CHK_GroupChatName
    CHECK (IsGroup = 0 OR (IsGroup = 1 AND LEN(ChatName) > 0))
);

CREATE TABLE ChatParticipants (
    ChatId INT NOT NULL,
    UserId INT NOT NULL,
    LastVisited DATETIME NOT NULL,
    CONSTRAINT FK_ChatParticipant_Chat FOREIGN KEY (ChatId) REFERENCES Chats(ChatId),
    CONSTRAINT FK_ChatParticipant_User FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT UQ_ChatParticipant UNIQUE(UserId, chatId)
);

CREATE TABLE Messages (
    MessageId INT PRIMARY KEY IDENTITY(1,1),
    ChatId INT NOT NULL,
    SenderId INT NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    SentAt DATETIME NOT NULL,
    CONSTRAINT FK_Message_Chat FOREIGN KEY (ChatId) REFERENCES Chats(ChatId),
    CONSTRAINT FK_Message_Sender FOREIGN KEY (SenderId) REFERENCES Users(UserId),
);

CREATE TABLE RefreshTokens
(
    UserId INT NOT NULL PRIMARY KEY,
    Token NVARCHAR(128) NOT NULL,
    ExpiresAt DATETIME NOT NULL
);


----

CREATE PROCEDURE GetAllUsers
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT * FROM Users;
END

CREATE PROCEDURE GetAllChats
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM Chats;
END;

CREATE PROCEDURE GetAllMessages
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM messages;
END;

CREATE PROCEDURE GetAllChatParticipants
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM ChatParticipants;
END;

CREATE PROCEDURE GetAllRefreshTokens
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM RefreshTokens;
END;

----

DROP PROCEDURE LoginUser;
CREATE PROCEDURE LoginUser
    @Username NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT UserId, Nickname, Password
    FROM Users
    WHERE Username = @Username;
END

---
CREATE PROCEDURE UpdateRefreshToken
    @UserId INT,
    @Token NVARCHAR(128),
    @ExpiresAt DATETIME
AS
BEGIN

    SET NOCOUNT ON;

    UPDATE RefreshTokens
    SET Token = @Token,
    ExpiresAt = @ExpiresAt
    WHERE UserId = @UserId;
END;

---

CREATE PROCEDURE CreateRefreshToken
    @UserId INT,
    @Token NVARCHAR(128),
    @ExpiresAt DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO RefreshTokens (UserId, Token, ExpiresAt)
    VALUES (@UserId, @Token, @ExpiresAt);
END;

---
CREATE PROCEDURE RegisterUser
    @Username NVARCHAR(100),
    @Password NVARCHAR(256),
    @Nickname NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        INSERT INTO Users (Username, Password, Nickname)
        VALUES (@Username, @Password, @Nickname);

        SELECT SCOPE_IDENTITY() AS UserId;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() = 2627 -- Unique constraint violation
        BEGIN
            RAISERROR('Username already exists', 16, 1);
            RETURN;
        END
        ELSE
            THROW;
    END CATCH
END
---

CREATE PROCEDURE UpdateChatLastVisited
    @ChatId INT,
    @UserId INT,
    @Date DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE ChatParticipants
        SET LastVisited = @Date
    WHERE ChatId = @ChatId
    AND UserId = @UserId;
END;

---

CREATE PROCEDURE UpdateChatLastUpdated
    @ChatId INT,
    @Date DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Chats
    SET LastUpdated = @Date
    WHERE ChatId = @ChatId;
END;

---

CREATE PROCEDURE InsertParticipantIntoChat
    @ChatId INT,
    @UserId INT,
    @Date DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO ChatParticipants (ChatId, UserId, LastVisited)
    VALUES (@ChatId, @UserId, @Date) 
END

---

CREATE PROCEDURE DeleteChat
    @ChatId INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM Messages
    WHERE ChatId = @ChatId;
    
    DELETE FROM Chats
    WHERE ChatId = @ChatId;
END;

---

CREATE PROCEDURE DeleteParticipantFromChat
   @ChatId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM ChatParticipants
    WHERE ChatId = @ChatId AND UserId = @UserId;
END;

---

CREATE PROCEDURE AddMessage
    @ChatId INT,
    @SenderId INT,
    @Content NVARCHAR(MAX),
    @Date DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MessageId INT;
    INSERT INTO Messages (ChatId, SenderId, Content, SentAt)
    VALUES (@ChatId, @SenderId, @Content, @Date);

    SET @MessageId = SCOPE_IDENTITY();

    SELECT @MessageId AS MessageId;
END

---

CREATE PROCEDURE CreateChat
    @Name NVARCHAR(100),
    @IsGroup BIT,
    @Date DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ChatId INT;

    INSERT INTO Chats (ChatName, IsGroup, LastUpdated)
    VALUES (@Name, @IsGroup, @Date);

    set @ChatId = SCOPE_IDENTITY();

    SELECT @ChatId AS ChatId;
END;

---

CREATE PROCEDURE GetAvgMessageLengthPerHour
    @TargetDate DATE -- Input parameter: the specific date we want to analyze
AS
BEGIN
    SET NOCOUNT ON;

    -- Step 1: prepare in-memory table of hours
    DECLARE @Hours TABLE (Hour INT);

    -- Counter variable
    DECLARE @i INT = 0;

    -- Loop and insert hours 0 to 23 into the @Hours table
    WHILE @i < 24
    BEGIN
        INSERT INTO @Hours VALUES (@i);
        SET @i = @i + 1;
    END;

    -- Step 2: left join the hours table with the messages table
    SELECT 
        h.Hour,   -- Each hour of the day
        ISNULL(AVG(CAST(LEN(m.Content) AS float)), 0) AS AvgMessageLength
        -- Compute the average message length for h.Hour.
        -- Replace NULL (no messages for that hour) with 0.
        -- Cast the average to decimal number.

    FROM @Hours h
    LEFT JOIN Messages m
        ON DATEPART(HOUR, m.SentAt) = h.Hour        -- Match messages sent in that hour
        AND CAST(m.SentAt AS DATE) = @TargetDate    -- and only on the given target date

    GROUP BY h.Hour        -- One row per hour
    ORDER BY h.Hour        -- Sort ascending
END