
using System.Text.Json.Serialization;

namespace MonopolyServer.Services.Auth;

public record DiscordAccountResponse(string id, string global_name, string avatar)
{
    [JsonInclude]
    public string id = id;
    [JsonInclude]
    public string global_name = global_name;
    [JsonInclude]
    public string avatar = avatar;
}