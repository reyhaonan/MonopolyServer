using MonopolyServer.Database.Entities;

namespace MonopolyServer.Repositories;

public interface IUserRepository
{
    public Task<User> GetById(Guid id);

    public Task<User> Create(User user);
    
}