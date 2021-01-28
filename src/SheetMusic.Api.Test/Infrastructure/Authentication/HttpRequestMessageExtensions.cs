using System;
using System.Net.Http;

namespace SheetMusic.Api.Test.Infrastructure.Authentication
{
    public static class HttpRequestMessageExtensions
    {
        public static void AddTestUserToken(this HttpRequestMessage message, string email)
        {
            var testToken = new
            {
                Name = Guid.NewGuid().ToString(),
                Mail = email
            };

            message.Headers.Add("Authorization", AuthTokenUtilities.WrapAuthToken(testToken));
        }
    }
}
