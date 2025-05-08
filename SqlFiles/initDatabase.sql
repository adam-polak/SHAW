/*
    File to initialize the SHAW database
    - program will run these commands if the 'shawdb' is not found on your MySQL
*/

CREATE DATABASE shawdb; 

CREATE TABLE roles (
    Id INTEGER PRIMARY KEY,
    Name VARCHAR(10)
);

CREATE TABLE users (
    Id INTEGER PRIMARY KEY AUTO_INCREMENT,
    Username VARCHAR(20) UNIQUE,
    Password VARCHAR(20),
    RoleId INTEGER,
    FOREIGN KEY (RoleId) REFERENCES roles(Id),
    LoginKey VARCHAR(100) UNIQUE
);

CREATE TABLE posts (
    Id INTEGER PRIMARY KEY AUTO_INCREMENT,
    UserId INTEGER,
    FOREIGN KEY (UserId) REFERENCES users(Id),
    Title VARCHAR(100),
    Body VARCHAR(1000),
    CreatedOn DATE DEFAULT (CURDATE())
);

CREATE TABLE responses (
    Id INTEGER PRIMARY KEY AUTO_INCREMENT,
    Content VARCHAR(1000),
    CreatedOn DATE DEFAULT (CURDATE()),
    UserId INTEGER,
    PostId INTEGER,
    ParentResponseId INTEGER,
    FOREIGN KEY (UserId) REFERENCES users(id),
    FOREIGN KEY (PostId) REFERENCES posts(id),
    FOREIGN KEY (ParentResponseId) REFERENCES responses(id)
);

CREATE TABLE post_interactions (
    Id INTEGER PRIMARY KEY AUTO_INCREMENT,
    Vote BOOLEAN,
    UserId INTEGER,
    PostId INTEGER,
    UNIQUE (UserId, PostId),
    FOREIGN KEY (UserId) REFERENCES users(id),
    FOREIGN KEY (PostId) REFERENCES posts(id)
);

CREATE TABLE responses_interactions (
    Id INTEGER PRIMARY KEY AUTO_INCREMENT,
    Vote BOOLEAN,
    UserId INTEGER,
    ResponseId INTEGER,
    UNIQUE (UserId, ResponseId),
    FOREIGN KEY (UserId) REFERENCES users(id),
    FOREIGN KEY (ResponseId) REFERENCES responses(id)
);

CREATE TABLE private_message_room (
    Id INTEGER PRIMARY KEY AUTO_INCREMENT,
    UserOne INTEGER,
    UserTwo INTEGER,
    CHECK (UserOne < UserTwo),
    UNIQUE(UserOne, UserTwo),
    FOREIGN KEY (UserOne) REFERENCES users(id),
    FOREIGN KEY (UserTwo) REFERENCES users(id)
);

CREATE TABLE private_messages (
    Id INTEGER PRIMARY KEY AUTO_INCREMENT,
    Content VARCHAR(1000),
    UserId INTEGER,
    RoomId INTEGER,
    CreatedOn DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES users(id),
    FOREIGN KEY (RoomId) REFERENCES private_message_room(id)
);

CREATE TABLE categories (
    Id INTEGER PRIMARY KEY AUTO_INCREMENT,
    Name VARCHAR(50) UNIQUE
);

CREATE TABLE post_categories (
    PostId INTEGER,
    CategoryId INTEGER,
    PRIMARY KEY (PostId, CategoryId),
    FOREIGN KEY (PostId) REFERENCES posts(Id),
    FOREIGN KEY (CategoryId) REFERENCES categories(Id)
);

CREATE TABLE tags (
    Id INTEGER PRIMARY KEY AUTO_INCREMENT,
    Name VARCHAR(50) UNIQUE
);

CREATE TABLE post_tags (
    PostId INTEGER,
    TagId INTEGER,
    PRIMARY KEY (PostId, TagId),
    FOREIGN KEY (PostId) REFERENCES posts(Id),
    FOREIGN KEY (TagId) REFERENCES tags(Id)
);

CREATE TABLE user_profiles (
    UserId INTEGER PRIMARY KEY,
    Bio TEXT,
    ProfilePicture VARCHAR(255),
    FOREIGN KEY (UserId) REFERENCES users(Id)
);