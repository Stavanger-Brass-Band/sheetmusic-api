using MediatR;
using SheetMusic.Api.CQRS.Query;
using SheetMusic.Api.Database;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command;

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
