using MonopolyServer.Database.Entities;

namespace MonopolyServer.DTO;

public class UserDTO
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public ICollection<UserOAuthDTO> OAuth { get; set; } = [];
}