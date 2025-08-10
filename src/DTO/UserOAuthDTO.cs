using MonopolyServer.Database.Enums;

namespace MonopolyServer.DTO;

public class UserOAuthDTO
{
    public Guid Id { get; set; }

    public string ProviderName { get; set; }

    public string OAuthID { get; set; }
}