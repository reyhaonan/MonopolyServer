using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MonopolyServer.Database.Entities;

public class User
{
    public Guid Id { get; set; }

    public string Username { get; set; }

    public ICollection<UserOAuth> OAuth { get; } = new List<UserOAuth>();
}