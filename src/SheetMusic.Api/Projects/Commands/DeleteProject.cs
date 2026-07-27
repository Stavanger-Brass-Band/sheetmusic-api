using MediatR;
using SheetMusic.Api.Database;
using SheetMusic.Api.Projects.Queries;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Projects.Commands;

public class DeleteProject(string projectIdentifier) : IRequest
{
    public string ProjectIdentifier { get; } = projectIdentifier;

    public class Handler(SheetMusicContext db, IMediator mediator) : IRequestHandler<DeleteProject>
    {
        public async Task Handle(DeleteProject request, CancellationToken cancellationToken)
        {
            var project = await mediator.Send(new GetProject(request.ProjectIdentifier), cancellationToken);

            db.Projects.Remove(project);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
