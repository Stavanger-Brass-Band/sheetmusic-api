using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Configuration;
using SheetMusic.Api.Database;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Repositories;
using SheetMusic.Api.Search;
using System.Reflection;

namespace SheetMusic
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<SheetMusicContext>(options => options.UseSqlServer(Configuration.GetConnectionString("SheetMusicContext")));

            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue; // In case of multipart
            });

            services.AddSheetMusicSecurity(Configuration);
            services.AddSheetMusicVersioning();
            services.AddSheetMusicSwagger();

            services.AddSingleton<IBlobClient, BlobClient>();
            services.AddScoped<IPartRepository, PartRepository>();
            services.AddScoped<ISetRepository, SetRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddSingleton<IndexAdminService>();

            services.AddMediatR(Assembly.GetAssembly(typeof(Startup)));

            services.AddHealthChecks();
            services.AddMemoryCache();

            services.AddControllers()
                .AddFluentValidation(config => config.RegisterValidatorsFromAssemblyContaining<Startup>());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
        {
            app.UseMiddleware<ErrorHandlerMiddleware>();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseHttpsRedirection();
            app.UseCors("AllowMember");

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                // build a swagger endpoint for each discovered API version
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                }
                options.OAuthAppName("SheetMusic API");
            });


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
            });
        }
    }
}
