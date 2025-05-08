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
            foreach(var post in posts)
            {
                post.Likes = await GetLikes(post.Id);
                post.Dislikes = await GetDislikes(post.Id);
            }

            return posts.ToList();
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
            return new List<PostsModel>();
        }
    }

    public async Task<int> GetLikes(int postId)
    {
        string sql = "SELECT COUNT(*)"
                    + " FROM post_interactions"
                    + " WHERE PostId = @pid AND Vote = 1;";

        return 
        (
            await _connection.QueryAsync<int>
            (
                sql, new 
                {
                    pid = postId
                }
            )
        ).FirstOrDefault();
    }

    public async Task<int> GetDislikes(int postId)
    {
        string sql = "SELECT COUNT(*)"
                    + " FROM post_interactions"
                    + " WHERE PostId = @pid AND Vote = 0;";

        return 
        (
            await _connection.QueryAsync<int>
            (
                sql, new 
                {
                    pid = postId
                }
            )
        ).FirstOrDefault();
    }

    /// <summary>
    /// Try to interact with the post
    /// </summary>
    /// <param name="postId"></param>
    /// <param name="userId"></param>
    /// <param name="action">if true then like the post, else dislike</param>
    /// <returns></returns>
    public async Task TryInteract(int postId, int userId, bool like)
    {
        string sql = "INSERT INTO post_interactions (Vote, UserId, PostId)"
                    + $" VALUES ({(like ? 1 : 0)}, @uid, @pid)"
                    + $" ON DUPLICATE KEY UPDATE Vote = {(like ? 1 : 0)};";
        await _connection.ExecuteAsync(sql, new { uid = userId, pid = postId });
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