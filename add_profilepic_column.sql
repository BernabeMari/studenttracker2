-- SQL script para idagdag ang ProfilePic column sa Students table
ALTER TABLE Students ADD ProfilePic NVARCHAR(MAX) NULL;

-- Update existing records para bigyan ng empty string value
UPDATE Students SET ProfilePic = '' WHERE ProfilePic IS NULL;

-- Optional: Gawing NOT NULL ang column
-- ALTER TABLE Students ALTER COLUMN ProfilePic NVARCHAR(MAX) NOT NULL; 