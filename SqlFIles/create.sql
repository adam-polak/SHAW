/*
    File to setup all the tables in the database
*/

CREATE TABLE roles (
    Id INTEGER PRIMARY KEY,
    Name VARCHAR(10)
);

CREATE TABLE users (
    Id INTEGER PRIMARY KEY,
    Username VARCHAR(20),
    Password VARCHAR(20),
    RoleId INTEGER FOREIGN KEY REFERENCES roles(Id)
);

CREATE TABLE posts (
    Id INTEGER PRIMARY KEY,
    UserId INTEGER FOREIGN KEY REFERENCES users(Id),
    Title VARCHAR(100),
    Body VARCHAR(1000),
    CreatedOn DATE
);

CREATE TABLE responses (
    Id INTEGER PRIMARY KEY,
    Content VARCHAR(1000),
    CreatedOn DATE,
    UserId INTEGER FOREIGN KEY REFERENCES users(id),
    PostId INTEGER FOREIGN KEY REFERENCES posts(id)
);
