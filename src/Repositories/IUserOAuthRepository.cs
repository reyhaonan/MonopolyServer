using MonopolyServer.Database.Entities;
using MonopolyServer.Database.Enums;

namespace MonopolyServer.Repositories;

public interface IUserOAuthRepository
{
    public Task<UserOAuth?> GetByProviderNameAndId(ProviderName providerName, string id);

    public Task<UserOAuth> Create(UserOAuth userOAuth);

    public Task<UserOAuth> UpdateUserId(Guid id, Guid newUserId);
}