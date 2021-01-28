using SheetMusic.Api.Database.Entities;
using System;

namespace SheetMusic.Api.Controllers.ViewModels
{
    public class ApiUser
    {
        public ApiUser(Musician musician)
        {
            if (musician == null)
            {
                throw new ArgumentNullException(nameof(musician));
            }

            Id = musician.Id;
            Name = musician.Name ?? null!;
            Email = musician.Email ?? null!;
            Inactive = musician.Inactive;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool Inactive { get; set; }
    }
}
