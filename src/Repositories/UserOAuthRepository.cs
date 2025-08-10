using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MonopolyServer.Database;
using MonopolyServer.Database.Entities;
using MonopolyServer.Database.Enums;

namespace MonopolyServer.Repositories;

public class UserOAuthRepository : IUserOAuthRepository
{

    private readonly MonopolyDbContext _dbContext;

    public UserOAuthRepository(MonopolyDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<UserOAuth?> GetByProviderNameAndId(ProviderName providerName, string id)
    {
        return await _dbContext.UserOAuth.Where(e => e.ProviderName == providerName && e.OAuthID == id).Include(e => e.User).FirstOrDefaultAsync();
    }

    public async Task<UserOAuth> Create(UserOAuth userOAuth)
    {
        await _dbContext.UserOAuth.AddAsync(userOAuth);

        await _dbContext.SaveChangesAsync();

        return userOAuth;
    }

    public async Task<UserOAuth> UpdateUserId(Guid id, Guid newUserId)
    {
        var result = await _dbContext.UserOAuth.FirstAsync(e => e.Id == id);
        result.UserId = newUserId;
        await _dbContext.SaveChangesAsync();

        return result;
    }
}