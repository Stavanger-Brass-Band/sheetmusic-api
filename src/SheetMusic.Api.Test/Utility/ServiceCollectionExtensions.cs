using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace SheetMusic.Api.Test.Utility
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection TryRemoveService<T>(this IServiceCollection services)
        {
            var descriptor = services.SingleOrDefault(s => s.ServiceType == typeof(T));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            return services;
        }
    }
}
