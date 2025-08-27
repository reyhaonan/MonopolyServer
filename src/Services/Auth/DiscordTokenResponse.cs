using System.Text.Json.Serialization;

namespace MonopolyServer.Services.Auth;

public record DiscordTokenResponse(string access_token, int expires_in, string token_type, string refresh_token, string scope)
{
    [JsonInclude]
    public string access_token = access_token;
    [JsonInclude]
    public int expires_in = expires_in;
    [JsonInclude]
    public string token_type = token_type;
    [JsonInclude]
    public string refresh_token = refresh_token;
    [JsonInclude]
    public string scope = scope;
}