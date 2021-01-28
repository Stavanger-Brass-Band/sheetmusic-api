using SheetMusic.Api.Database.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SheetMusic.Api.Repositories
{
    public interface IUserRepository
    {
        Task<Musician?> AuthenticateAsync(string email, string password);
        IEnumerable<Musician> GetAll();
        Task<Musician> GetByIdAsync(Guid id);
        Task<Musician> CreateAsync(Musician user, string password);
        Task UpdateAsync(Musician user, string? password = null);
        Task DeleteAsync(int id);
    }
}