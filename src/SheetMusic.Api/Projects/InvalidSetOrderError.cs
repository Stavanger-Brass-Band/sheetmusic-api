using System;
using System.Net;
using SheetMusic.Api.Errors;

namespace SheetMusic.Api.Projects;

/// <summary>
/// Thrown when the set identifiers provided for reordering do not exactly match the sets currently assigned to the project
/// </summary>
public class InvalidSetOrderError(Guid projectId) : ExceptionBase($"Provided set identifiers must exactly match the sets currently assigned to project {projectId}")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}
