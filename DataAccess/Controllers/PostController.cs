using System.Data.Common;
using Dapper;
using SHAW.DataAccess.Models;
using SHAW.DataAccess.Util;

namespace SHAW.DataAccess.Controllers;

public class PostController : AutoDbConnection
{
    public PostController(DbConnection connection) : base(connection)
    {
    }

    public async Task<List<PostsModel>> GetPosts()
    {
        string sql =
            @"SELECT p.Id, p.Title, p.Body, p.CreatedOn, u.Username as Author
              FROM posts p
              JOIN users u ON p.UserId = u.Id
              ORDER BY p.CreatedOn DESC";

        try
        {
            var posts = await _connection.QueryAsync<PostsModel>(sql);
            return posts.ToList();
        }
        catch
        {
            return new List<PostsModel>();
        }
    }

    public async Task CreatePost(PostsModel postsModel)
    {
        string sql = @"
        INSERT INTO posts (Title, Body, CreatedOn, UserId)
        VALUES (@Title, @Body, @CreatedOn, @UserId)";

        try
        {
            await _connection.ExecuteAsync(sql, new
            {
                postsModel.Title,
                postsModel.Body,
                postsModel.CreatedOn,
                postsModel.UserId
            });
        }
        catch (Exception e)
        {
            throw new Exception("Failed to create post", e);
        }
    }
}