using JwtApi.Domain.Entities;
using JwtApi.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace JwtApi.Infrastructure.Persistence;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> GetByIdAsync(Guid id) =>
        db.Users.FindAsync(id).AsTask();

    public Task<User?> GetByRefreshTokenAsync(string refreshToken) =>
        db.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

    public async Task AddAsync(User user) =>
        await db.Users.AddAsync(user);

    public Task UpdateAsync(User user)
    {
        db.Users.Update(user);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() =>
        db.SaveChangesAsync();
}
