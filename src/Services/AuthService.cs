using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MonopolyServer.Services;

public class AuthService
{
    private readonly IConfiguration _config;

    public AuthService(IConfiguration config)
    {
        _config = config;
    }
    public (string, string) GenerateToken(Guid user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var csrfToken = GenerateCsrfToken();

        var claims = new Dictionary<string, object>
        {
            [ClaimTypes.Sid] = user.ToString(),
            ["xsrf_token"] = csrfToken
        };

        var descriptor = new SecurityTokenDescriptor
        {

            Issuer = _config["JWT:Issuer"],
            Audience = _config["JWT:Audience"],
            Claims = claims,
            Expires = DateTime.UtcNow.AddMinutes(120),
            SigningCredentials = credentials,
        };

        var handler = new JsonWebTokenHandler();
        handler.SetDefaultTimesOnTokenCreation = false;

        return (handler.CreateToken(descriptor), csrfToken);
    }

    public static string GenerateCsrfToken(int length = 32)
    {
        // Create a byte array to hold the random bytes.
        byte[] tokenBytes = new byte[length];

        // Fill the array with cryptographically strong random bytes.
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }

        // Convert the byte array to a hexadecimal string.
        // This makes it safe to transmit and store as text.
        StringBuilder sb = new StringBuilder(length * 2); // Each byte becomes two hex characters
        foreach (byte b in tokenBytes)
        {
            sb.AppendFormat("{0:x2}", b);
        }

        return sb.ToString();
    }
}