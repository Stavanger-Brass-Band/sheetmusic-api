using MediatR;
using SheetMusic.Api.Controllers.RequestModels;
using SheetMusic.Api.CQRS.Query;
using SheetMusic.Api.Database;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command
{
    public class UpdateProjectMetadata : IRequest
    {
        private readonly string identifier;
        private readonly UpdateProjectRequest request;

        public UpdateProjectMetadata(string identifier, UpdateProjectRequest request)
        {
            this.identifier = identifier;
            this.request = request;
        }

        public class Handler : AsyncRequestHandler<UpdateProjectMetadata>
        {
            private readonly SheetMusicContext db;
            private readonly IMediator mediator;

            public Handler(SheetMusicContext db, IMediator mediator)
            {
                this.db = db;
                this.mediator = mediator;
            }

            protected override async Task Handle(UpdateProjectMetadata command, CancellationToken cancellationToken)
            {
                var project = await mediator.Send(new GetProject(command.identifier));

                project.Name = command.request.Name;
                project.StartDate = command.request.StartDate;
                project.EndDate = command.request.EndDate;

                await db.SaveChangesAsync();
            }
        }
    }
}
