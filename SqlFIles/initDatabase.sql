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
    Id INTEGER PRIMARY KEY,
    Username VARCHAR(20),
    Password VARCHAR(20),
    RoleId INTEGER,
    FOREIGN KEY (RoleId) REFERENCES roles(Id)
);

CREATE TABLE posts (
    Id INTEGER PRIMARY KEY,
    UserId INTEGER,
    FOREIGN KEY (UserId) REFERENCES users(Id),
    Title VARCHAR(100),
    Body VARCHAR(1000),
    CreatedOn DATE
);

CREATE TABLE responses (
    Id INTEGER PRIMARY KEY,
    Content VARCHAR(1000),
    CreatedOn DATE,
    UserId INTEGER,
    PostId INTEGER,
    FOREIGN KEY (UserId) REFERENCES users(id),
    FOREIGN KEY (PostId) REFERENCES posts(id)
);
