using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace SheetMusic.Api.Repositories
{
    public class SetRepository : ISetRepository
    {
        private readonly SheetMusicContext context;
        private readonly IBlobClient blobClient;
        private readonly IPartRepository partRepository;
        private readonly ILogger<SetRepository> logger;

        public SetRepository(SheetMusicContext context, IBlobClient blobClient, IPartRepository partRepository, ILogger<SetRepository> logger)
        {
            this.context = context;
            this.blobClient = blobClient;
            this.partRepository = partRepository;
            this.logger = logger;
        }

        public async Task AddMusicPartForSetAsync(Guid setId, Guid partId)
        {
            var set = await context.SheetMusicSets.FirstAsync(s => s.Id == setId);

            context.SheetMusicParts.Add(new SheetMusicPart
            {
                Id = Guid.NewGuid(),
                MusicPartId = partId,
                SetId = set.Id
            });

            await context.SaveChangesAsync();
        }

        public async Task AddNewSetAsync(SheetMusicSet set)
        {
            if (context.SheetMusicSets.Any(s => s.ArchiveNumber == set.ArchiveNumber))
                throw new ArchiveNumberOccupiedError(set.ArchiveNumber);

            await context.SheetMusicSets.AddAsync(set);
            await context.SaveChangesAsync();
        }

        public async Task AddPartContentForSetAsync(string identifier, Stream zipFileStream)
        {
            var set = await ResolveByIdentiferAsync(identifier);

            logger.LogInformation($"Resolver identifier [{identifier}] as set {set.Id}");

            using (var zipArchive = new ZipArchive(zipFileStream))
            {
                foreach (var entry in zipArchive.Entries)
                {
                    logger.LogInformation($"Processing entry {entry.Name}");

                    var partName = Path.GetFileNameWithoutExtension(entry.Name);
                    var part = await partRepository.GetOrCreatePartAsync(partName);

                    if (set.Parts.Any(sp => sp.MusicPartId == part.Id))
                    {
                        throw new MusicSetPartAlreadyAddedError(set.Title, part.Name);
                    }
                    context.SheetMusicParts.Add(new SheetMusicPart { Id = Guid.NewGuid(), MusicPartId = part.Id, SetId = set.Id });
                    logger.LogInformation($"Part identified as {part.Name}. Uploading.");

                    var partIdentifier = new PartRelatedToSet(set.Id, part.Id);

                    await blobClient.AddMusicPartContentAsync(partIdentifier, entry.Open());
                    await context.SaveChangesAsync();
                }
            }
        }

        public async Task DeleteSetAsync(SheetMusicSet set)
        {
            await blobClient.DeleteSetContentAsync(set.Id);
            context.SheetMusicSets.Remove(set);
            await context.SaveChangesAsync();
        }

        public async Task<SheetMusicPart> GetMusicPartForSetAsync(Guid setId, Guid partId)
        {
            return await context.SheetMusicParts
                .Include(smp => smp.Part)
                .FirstOrDefaultAsync(smp => smp.SetId == setId && smp.MusicPartId == partId);
        }

        public async Task<int> GetNextAvailableArchiveNumberAsync()
        {
            if (!await context.SheetMusicSets.AnyAsync()) //no sets in the database, start on 1
                return 1;

            return await context.SheetMusicSets.MaxAsync(s => s.ArchiveNumber) + 1;
        }

        public async Task<Stream> GetPartPdfsAsZipForSetAsync(string identifier)
        {
            var set = await ResolveByIdentiferAsync(identifier);

            var memstream = new MemoryStream();

            using (var zip = new ZipArchive(memstream, ZipArchiveMode.Create, true))
            {
                foreach (var partRelation in set.Parts)
                {
                    var entry = zip.CreateEntry($"{partRelation.Part.Name}.pdf");
                    using (var entryStream = entry.Open())
                    {
                        var id = new PartRelatedToSet(set.Id, partRelation.MusicPartId);
                        var contents = await blobClient.GetMusicPartContentStreamAsync(id);
                        contents.CopyTo(entryStream);

                        await entryStream.FlushAsync();
                    }
                }

                return memstream;
            }
        }

        public async Task<List<SheetMusicSet>> GetSetsAsync()
        {
            return await context.SheetMusicSets
                .Include(s => s.Parts).ThenInclude(p => p.Part)
                .ToListAsync();
        }

        public async Task<List<SheetMusicSet>> GetSetsWithPartsAsync()
        {
            return await context.SheetMusicSets
                .Include(s => s.Parts).ThenInclude(p => p.Part)
                .Where(s => s.Parts.Count > 0)
                .ToListAsync();
        }

        public async Task<SheetMusicSet> ResolveByIdentiferAsync(string identifier)
        {
            SheetMusicSet result = null!;

            if (Guid.TryParse(identifier, out var guid))
            {
                result = await context.SheetMusicSets
                    .Include(s => s.Parts).ThenInclude(p => p.Part)
                    .FirstOrDefaultAsync(set => set.Id == guid);
            }
            else if (int.TryParse(identifier, out var archiveNumber))
            {
                result = await context.SheetMusicSets
                    .Include(s => s.Parts).ThenInclude(p => p.Part)
                    .FirstOrDefaultAsync(set => set.ArchiveNumber == archiveNumber);
            }
            else
            {
                //ignore casing when comparing on title
                result = await context.SheetMusicSets
                    .Include(s => s.Parts).ThenInclude(p => p.Part)
                    .SingleOrDefaultAsync(set => set.Title.ToLower() == identifier.ToLower());
            }
            if (result == null) throw new NotFoundError($"sets/{identifier}", "Set not found");

            return result;
        }

        public async Task<List<SheetMusicSet>> SearchAsync(string searchTerm)
        {
            var matchingItemsQuery = context.SheetMusicSets.Where(set =>
                set.ArchiveNumber.ToString().Contains(searchTerm) ||
                set.Title.Contains(searchTerm) ||
                (set.Arranger != null && set.Arranger.Contains(searchTerm)) ||
                (set.Composer != null && set.Composer.Contains(searchTerm)));

            return await matchingItemsQuery
                .Include(s => s.Parts)
                .ToListAsync();
        }

        public async Task DeleteMusicPartForSetAsync(Guid setId, Guid partId)
        {
            var musicPart = await context.SheetMusicParts.FirstOrDefaultAsync(smp => smp.SetId == setId && smp.MusicPartId == partId);

            if (musicPart != null)
            {
                context.Remove(musicPart);
                await context.SaveChangesAsync();
            }
        }
    }
}
