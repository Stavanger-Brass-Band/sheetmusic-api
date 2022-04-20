using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using SheetMusic.Api.OData;
using SheetMusic.Api.OData.MVC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SheetMusic.Api.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly SheetMusicContext context;

    public ProjectRepository(SheetMusicContext context)
    {
        this.context = context;
    }

    public async Task<List<Project>> GetProjectsAsync(ODataQueryParams? queryParams = null)
    {
        var query = context.Projects.AsQueryable();

        if (queryParams != null && queryParams.HasFilter)
        {
            query = query.ApplyODataFilters(queryParams, m =>
            {
                m.MapField("startDate", p => p.StartDate);
                m.MapField("endDate", p => p.EndDate);
                m.MapField("name", p => p.Name);
            });
        }

        return await query.ToListAsync();
    }

    public async Task<Project> GetProjectAsync(Guid id)
    {
        return await context.Projects.FirstOrDefaultAsync(p => p.Id == id) ?? throw new NotFoundError($"projects/{id}", "Project not found");
    }

    public async Task<List<SheetMusicSet>> GetSetsForProjectAsync(Guid id)
    {
        var query = from p in context.Projects
                    from sc in p.SetConnections
                    where p.Id == id
                    select sc.Set;

        return await query.ToListAsync();
    }

    public async Task<Project> AddNewProjectAsync(Project project)
    {
        project.Id = Guid.NewGuid();

        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();

        return project;
    }

    public async Task<Project> ResolveByIdentifierAsync(string identifier)
    {
        Project? result;

        if (Guid.TryParse(identifier, out var guid))
        {
            result = await context.Projects.FirstOrDefaultAsync(project => project.Id == guid);
        }
        else
        {
            //ignore casing when comparing on title
            result = await context.Projects.FirstOrDefaultAsync(project => project.Name.ToLower() == identifier.ToLower());
        }

        if (result is null)
            throw new NotFoundError($"projects/{identifier}", "Project not found");

        return result;
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }

    public async Task ConnectSetWithProjectAsync(Guid projectId, Guid setId)
    {
        var connection = new ProjectSheetMusicSet
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            SheetMusicSetId = setId
        };

        await context.ProjectSheetMusicSets.AddAsync(connection);
        await context.SaveChangesAsync();
    }

    public async Task DisconnectSetFromProjectAsync(Guid projectId, Guid setId)
    {
        var connection = await context.ProjectSheetMusicSets.FirstOrDefaultAsync(ps => ps.ProjectId == projectId && ps.SheetMusicSetId == setId);

        if (connection is null)
            throw new NotFoundError($"projects/{projectId}/sets/{setId}", "Connection does not exist");

        context.Remove(connection);
        await context.SaveChangesAsync();
    }

    public async Task DeleteProjectAsync(string projectIdentifier)
    {
        var project = await ResolveByIdentifierAsync(projectIdentifier);

        if (project is null)
            throw new NotFoundError($"projects/{projectIdentifier}");

        context.Remove(project);
        await context.SaveChangesAsync();
    }
}
