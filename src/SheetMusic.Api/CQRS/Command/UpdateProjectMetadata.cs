using MediatR;
using SheetMusic.Api.Controllers.RequestModels;
using SheetMusic.Api.CQRS.Query;
using SheetMusic.Api.Database;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command;

public class UpdateProjectMetadata(string identifier, UpdateProjectRequest request) : IRequest
{
    private readonly string identifier = identifier;
    private readonly UpdateProjectRequest request = request;

    public class Handler(SheetMusicContext db, IMediator mediator) : IRequestHandler<UpdateProjectMetadata>
    {
        public async Task Handle(UpdateProjectMetadata command, CancellationToken cancellationToken)
        {
            var project = await mediator.Send(new GetProject(command.identifier));

            project.Name = command.request.Name;
            project.StartDate = command.request.StartDate;
            project.EndDate = command.request.EndDate;

            await db.SaveChangesAsync();
        }
    }
}
