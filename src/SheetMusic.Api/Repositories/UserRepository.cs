using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetMusic.Api.Repositories;

public class UserRepository(SheetMusicContext context) : IUserRepository
{
    public async Task<Musician?> AuthenticateAsync(string email, string password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            return null;

        var user = await context.Musicians.SingleOrDefaultAsync(x => x.Email == email && !x.Inactive);

        // check if username exists
        if (user == null)
            return null;

        // check if password is correct
        if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
            return null;

        // authentication successful
        return user;
    }

    public IEnumerable<Musician> GetAll()
    {
        return context.Musicians;
    }

    public async Task<Musician> GetByIdAsync(Guid id)
    {
        return await context.Musicians.FindAsync(id) ?? throw new NotFoundError($"musicians/{id}", $"{nameof(Musician)} not found");
    }

    public async Task<Musician> CreateAsync(Musician user, string password)
    {
        // validation
        if (string.IsNullOrWhiteSpace(password))
            throw new MissingInputError(nameof(password));

        if (context.Musicians.Any(x => x.Email == user.Email))
            throw new UserAlreadyExistsError(user.Email ?? "<empty email>");

        CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

        user.PasswordHash = passwordHash;
        user.PasswordSalt = passwordSalt;

        await context.Musicians.AddAsync(user);
        await context.SaveChangesAsync();

        return user;
    }

    public async Task UpdateAsync(Musician updatedMusician, string? password = null)
    {
        var existingMusician = await context.Musicians.FindAsync(updatedMusician.Id);

        if (existingMusician == null)
            throw new NotFoundError(nameof(Musician));

        if (updatedMusician.Email != existingMusician.Email)
        {
            // username has changed so check if the new username is already taken
            if (await context.Musicians.AnyAsync(x => x.Email == updatedMusician.Email))
                throw new UserAlreadyExistsError(updatedMusician.Email ?? "<empty email>");
        }

        // update user properties
        existingMusician.Name = updatedMusician.Name;

        // update password if it was entered
        if (!string.IsNullOrWhiteSpace(password))
        {
            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);

            existingMusician.PasswordHash = passwordHash;
            existingMusician.PasswordSalt = passwordSalt;
        }

        context.Musicians.Update(existingMusician);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var user = await context.Musicians.FindAsync(id);
        if (user != null)
        {
            context.Musicians.Remove(user);
            await context.SaveChangesAsync();
        }
    }

    // private helper methods

    private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        if (password == null) throw new ArgumentNullException("password");
        if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");

        using var hmac = new System.Security.Cryptography.HMACSHA512();
        passwordSalt = hmac.Key;
        passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    private static bool VerifyPasswordHash(string password, byte[]? storedHash, byte[]? storedSalt)
    {
        if (string.IsNullOrWhiteSpace(password)) throw new MissingInputError(nameof(password));
        if (storedHash?.Length != 64) throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");
        if (storedSalt?.Length != 128) throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordHash");

        using var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        for (int i = 0; i < computedHash.Length; i++)
        {
            if (computedHash[i] != storedHash[i]) return false;
        }

        return true;
    }
}
