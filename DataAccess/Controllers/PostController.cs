using System.Data.Common;
using Dapper;
using SHAW.DataAccess.Models;
using SHAW.DataAccess.Util;

namespace SHAW.DataAccess.Controllers;

public class PostController : AutoDbConnection
{
    public PostController(DbConnection connection) : base(connection) {}

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
            foreach(var post in posts)
            {
                post.Likes = await TryGetLikes(post.Id);
                post.Dislikes = await TryGetDislikes(post.Id);
            }

            return posts.ToList();
        }
        catch
        {
            return new List<PostsModel>();
        }
    }

    public async Task<int> TryGetLikes(int postId)
    {
        return 0;
    }

    public async Task<int> TryGetDislikes(int postId)
    {
        return 0;
    }
}