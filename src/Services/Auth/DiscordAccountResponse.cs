
using System.Text.Json.Serialization;

namespace MonopolyServer.Services.Auth;

public record DiscordAccountResponse(string id, string username, string avatar)
{
    [JsonInclude]
    public string id = id;
    [JsonInclude]
    public string username = username;
    [JsonInclude]
    public string avatar = avatar;
}