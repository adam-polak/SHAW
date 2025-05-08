namespace SHAW.DataAccess.Models;

public class CommentModel
{
    public int Id { get; set; }
    public DateTime CreatedOn { get; set; }
    public required string Username { get; set; }
    public required string Content { get; set; }
}