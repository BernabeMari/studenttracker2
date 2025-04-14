-- Users table
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Password NVARCHAR(255) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    UserType NVARCHAR(20) NOT NULL, -- 'Student' or 'Parent'
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- StudentParentConnection table
CREATE TABLE StudentParentConnection (
    ConnectionId INT IDENTITY(1,1) PRIMARY KEY,
    StudentId INT REFERENCES Users(UserId),
    ParentId INT REFERENCES Users(UserId),
    Status NVARCHAR(20) DEFAULT 'Pending', -- 'Pending', 'Approved', 'Rejected'
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT UQ_StudentParent UNIQUE(StudentId, ParentId)
);

-- LocationTracking table
CREATE TABLE LocationTracking (
    TrackingId INT IDENTITY(1,1) PRIMARY KEY,
    StudentId INT REFERENCES Users(UserId),
    Latitude DECIMAL(10, 8) NOT NULL,
    Longitude DECIMAL(11, 8) NOT NULL,
    TrackingDateTime DATETIME DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1
);

-- TrackingSession table
CREATE TABLE TrackingSession (
    SessionId INT IDENTITY(1,1) PRIMARY KEY,
    StudentId INT REFERENCES Users(UserId),
    StartTime DATETIME DEFAULT GETDATE(),
    EndTime DATETIME NULL,
    IsActive BIT DEFAULT 1
); 