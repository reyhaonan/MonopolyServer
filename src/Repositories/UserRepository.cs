using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MonopolyServer.Database;
using MonopolyServer.Database.Entities;

namespace MonopolyServer.Repositories;

public class UserRepository: IUserRepository
{
    
    private readonly MonopolyDbContext _dbContext;

    public UserRepository(MonopolyDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    public async Task<User> GetById(Guid id)
    {
        return await _dbContext.Users.Where(u => u.Id == id).FirstAsync();
    }
    public async Task<User> Create(User user)
    {
        await _dbContext.Users.AddAsync(user);

        await _dbContext.SaveChangesAsync();

        return user;
    }

}