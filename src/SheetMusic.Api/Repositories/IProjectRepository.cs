using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.OData.MVC;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SheetMusic.Api.Repositories
{
    public interface IProjectRepository
    {
        Task<Project> AddNewProjectAsync(Project project);
        Task<Project> GetProjectAsync(Guid id);
        Task<List<Project>> GetProjectsAsync(ODataQueryParams? queryParams = null);
        Task<List<SheetMusicSet>> GetSetsForProjectAsync(Guid id);
        Task<Project> ResolveByIdentifierAsync(string identifier);
        Task SaveChangesAsync();
        Task ConnectSetWithProjectAsync(Guid projectId, Guid setId);
        Task DisconnectSetFromProjectAsync(Guid projectId, Guid setId);
        Task DeleteProjectAsync(string projectIdentifier);
    }
}