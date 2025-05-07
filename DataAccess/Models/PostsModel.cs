namespace SHAW.DataAccess.Models;

public class PostsModel
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public DateTime CreatedOn { get; set; }
    public string Author { get; set; }
}