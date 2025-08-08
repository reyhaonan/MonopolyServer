using Microsoft.EntityFrameworkCore;

namespace MonopolyServer.Database.Entity;

[PrimaryKey("id")]
public class User
{
    public Guid id;
}