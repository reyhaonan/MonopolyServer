using MonopolyServer.Database.Enums;

namespace MonopolyServer.Database.Entities;

public class UserOAuth
{

    public Guid Id { get; set; }

    public ProviderName ProviderName { get; set; }

    public string OAuthID { get; set; }

    public Guid? UserId { get; set; }
    public User User { get; set; } = null!;
}