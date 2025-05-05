namespace SHAW.DataAccess.Models;

public class UserModel
{
    public required int Id { get; set; }
    public required string Username { get; set; }
    public required RoleType RoleId { get; set; }
}