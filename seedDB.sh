#!/bin/bash

# seedDB.sh
# Check if jq is installed (needed for JSON parsing)
if ! command -v jq &> /dev/null; then
    echo "jq is required but not installed. Please install jq first."
    exit 1
fi

# Read database credentials from secrets.json
if [ ! -f "secrets.json" ]; then
    echo "secrets.json not found. Please create it first."
    exit 1
fi

DB_USER=$(jq -r '.DatabaseUsername' secrets.json)
DB_PASS=$(jq -r '.DatabasePassword' secrets.json)

# Find the correct path to initDatabase.sql based on DbConnectionFactory.cs
INIT_DB_PATH="./SqlFiles/initDatabase.sql"
if [ ! -f "$INIT_DB_PATH" ]; then
    echo "Warning: $INIT_DB_PATH not found. Database may need to be initialized separately."
fi

# Create seed data SQL file with conditionals to avoid duplicate entries
cat > seedData.sql << 'EOL'
USE shawdb;

-- Insert roles (only if they don't exist)
INSERT IGNORE INTO roles (Id, Name) VALUES
(1, 'student'),
(2, 'counselor');

-- Insert sample users (only if they don't exist)
INSERT IGNORE INTO users (Username, Password, RoleId) VALUES
('student123', 'pass123', 1),
('counselor1', 'pass123', 2),
('student456', 'pass123', 1),
('counselor2', 'pass123', 2);

-- Insert sample posts
INSERT INTO posts (UserId, Title, Body, CreatedOn) VALUES
(1, 'Need Help with Stress Management', 'I''ve been feeling overwhelmed with my coursework lately. Any tips for managing academic stress?', CURDATE()),
(2, 'Wellness Workshop Next Week', 'Join us for a workshop on mindfulness and meditation techniques. All students welcome!', DATE_SUB(CURDATE(), INTERVAL 1 DAY)),
(3, 'Looking for Study Group', 'Anyone interested in forming a study group for final exams? It helps reduce anxiety when we study together.', DATE_SUB(CURDATE(), INTERVAL 2 DAY)),
(4, 'Self-Care Tips During Finals', 'Remember to take care of yourself during finals week. Here are some helpful strategies...', DATE_SUB(CURDATE(), INTERVAL 3 DAY)),
(1, 'Sleep Schedule Help', 'My sleep schedule is completely messed up. How can I fix it?', DATE_SUB(CURDATE(), INTERVAL 4 DAY));

-- Insert sample categories (only if they don't exist)
INSERT IGNORE INTO categories (Name) VALUES
('Mental Health'),
('Academic Support'),
('Wellness'),
('Social Connection');

-- Get the actual category IDs
SET @mental_health_id = (SELECT Id FROM categories WHERE Name = 'Mental Health' LIMIT 1);
SET @academic_id = (SELECT Id FROM categories WHERE Name = 'Academic Support' LIMIT 1);
SET @wellness_id = (SELECT Id FROM categories WHERE Name = 'Wellness' LIMIT 1);
SET @social_id = (SELECT Id FROM categories WHERE Name = 'Social Connection' LIMIT 1);

-- Link posts to categories (with DELETE first to avoid duplicates)
DELETE FROM post_categories WHERE PostId IN (1, 2, 3, 4, 5);
INSERT INTO post_categories (PostId, CategoryId) VALUES
(1, @mental_health_id),
(1, @academic_id),
(2, @wellness_id),
(3, @academic_id),
(3, @social_id),
(4, @mental_health_id),
(4, @wellness_id),
(5, @mental_health_id);

-- Insert sample tags (only if they don't exist)
INSERT IGNORE INTO tags (Name) VALUES
('stress'),
('study'),
('wellness'),
('sleep'),
('anxiety');

-- Get the actual tag IDs
SET @stress_id = (SELECT Id FROM tags WHERE Name = 'stress' LIMIT 1);
SET @study_id = (SELECT Id FROM tags WHERE Name = 'study' LIMIT 1);
SET @wellness_id = (SELECT Id FROM tags WHERE Name = 'wellness' LIMIT 1);
SET @sleep_id = (SELECT Id FROM tags WHERE Name = 'sleep' LIMIT 1);
SET @anxiety_id = (SELECT Id FROM tags WHERE Name = 'anxiety' LIMIT 1);

-- Link posts to tags (with DELETE first to avoid duplicates)
DELETE FROM post_tags WHERE PostId IN (1, 2, 3, 4, 5);
INSERT INTO post_tags (PostId, TagId) VALUES
(1, @stress_id),
(1, @anxiety_id),
(2, @wellness_id),
(3, @study_id),
(4, @wellness_id),
(5, @sleep_id);
EOL

# Execute the seed data SQL (skip initDatabase.sql since it's handled by the app)
mysql -u"$DB_USER" -p"$DB_PASS" < seedData.sql

# Clean up
rm seedData.sql

echo "Database seeded successfully!"