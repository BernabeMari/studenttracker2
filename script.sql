IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Users] (
    [UserId] int NOT NULL IDENTITY,
    [Username] nvarchar(50) NOT NULL,
    [Password] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [UserType] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([UserId])
);
GO

CREATE TABLE [LocationTrackings] (
    [TrackingId] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [Latitude] decimal(18,2) NOT NULL,
    [Longitude] decimal(18,2) NOT NULL,
    [TrackingDateTime] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocationTrackings] PRIMARY KEY ([TrackingId]),
    CONSTRAINT [FK_LocationTrackings_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
);
GO

CREATE TABLE [StudentParentConnections] (
    [ConnectionId] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [ParentId] int NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_StudentParentConnections] PRIMARY KEY ([ConnectionId]),
    CONSTRAINT [FK_StudentParentConnections_Users_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentParentConnections_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [TrackingSessions] (
    [SessionId] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [StartTime] datetime2 NOT NULL,
    [EndTime] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_TrackingSessions] PRIMARY KEY ([SessionId]),
    CONSTRAINT [FK_TrackingSessions_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_LocationTrackings_StudentId] ON [LocationTrackings] ([StudentId]);
GO

CREATE INDEX [IX_StudentParentConnections_ParentId] ON [StudentParentConnections] ([ParentId]);
GO

CREATE INDEX [IX_StudentParentConnections_StudentId] ON [StudentParentConnections] ([StudentId]);
GO

CREATE INDEX [IX_TrackingSessions_StudentId] ON [TrackingSessions] ([StudentId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250313083900_InitialCreate', N'6.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [LocationTrackings] DROP CONSTRAINT [FK_LocationTrackings_Users_StudentId];
GO

ALTER TABLE [StudentParentConnections] DROP CONSTRAINT [FK_StudentParentConnections_Users_ParentId];
GO

ALTER TABLE [StudentParentConnections] DROP CONSTRAINT [FK_StudentParentConnections_Users_StudentId];
GO

ALTER TABLE [TrackingSessions] DROP CONSTRAINT [FK_TrackingSessions_Users_StudentId];
GO

DROP TABLE [Users];
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LocationTrackings]') AND [c].[name] = N'IsActive');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [LocationTrackings] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [LocationTrackings] DROP COLUMN [IsActive];
GO

EXEC sp_rename N'[LocationTrackings].[TrackingDateTime]', N'Timestamp', N'COLUMN';
GO

EXEC sp_rename N'[LocationTrackings].[StudentId]', N'SessionId', N'COLUMN';
GO

EXEC sp_rename N'[LocationTrackings].[TrackingId]', N'LocationId', N'COLUMN';
GO

EXEC sp_rename N'[LocationTrackings].[IX_LocationTrackings_StudentId]', N'IX_LocationTrackings_SessionId', N'INDEX';
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LocationTrackings]') AND [c].[name] = N'Longitude');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [LocationTrackings] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [LocationTrackings] ALTER COLUMN [Longitude] decimal(11,8) NOT NULL;
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LocationTrackings]') AND [c].[name] = N'Latitude');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [LocationTrackings] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [LocationTrackings] ALTER COLUMN [Latitude] decimal(10,8) NOT NULL;
GO

CREATE TABLE [Parents] (
    [ParentId] int NOT NULL IDENTITY,
    [Username] nvarchar(50) NOT NULL,
    [Password] nvarchar(max) NOT NULL,
    [Email] nvarchar(450) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Parents] PRIMARY KEY ([ParentId])
);
GO

CREATE TABLE [Students] (
    [StudentId] int NOT NULL IDENTITY,
    [Username] nvarchar(50) NOT NULL,
    [Password] nvarchar(max) NOT NULL,
    [Email] nvarchar(450) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Students] PRIMARY KEY ([StudentId])
);
GO

CREATE UNIQUE INDEX [IX_Parents_Email] ON [Parents] ([Email]);
GO

CREATE UNIQUE INDEX [IX_Parents_Username] ON [Parents] ([Username]);
GO

CREATE UNIQUE INDEX [IX_Students_Email] ON [Students] ([Email]);
GO

CREATE UNIQUE INDEX [IX_Students_Username] ON [Students] ([Username]);
GO

ALTER TABLE [LocationTrackings] ADD CONSTRAINT [FK_LocationTrackings_TrackingSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [TrackingSessions] ([SessionId]) ON DELETE CASCADE;
GO

ALTER TABLE [StudentParentConnections] ADD CONSTRAINT [FK_StudentParentConnections_Parents_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Parents] ([ParentId]) ON DELETE NO ACTION;
GO

ALTER TABLE [StudentParentConnections] ADD CONSTRAINT [FK_StudentParentConnections_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([StudentId]) ON DELETE NO ACTION;
GO

ALTER TABLE [TrackingSessions] ADD CONSTRAINT [FK_TrackingSessions_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([StudentId]) ON DELETE CASCADE;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250313085710_UpdateLocationTracking', N'6.0.0');
GO

COMMIT;
GO

