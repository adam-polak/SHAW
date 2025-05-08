namespace SHAW.DataAccess.Models;

public class PostsModel
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public DateTime CreatedOn { get; set; }
    public required string Author { get; set; }
    public int Likes { get; set; }
    public int Dislikes { get; set; }
    public int UserId { get; set; }
}