using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System;

namespace ChangeVersion.Server
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder
        ) : base(options, logger, encoder) 
        { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(Options.HeaderName, out Microsoft.Extensions.Primitives.StringValues extractedApiKey))
                return Task.FromResult(AuthenticateResult.Fail("API Key header not found"));

            if (!string.Equals(extractedApiKey, Options.Key, StringComparison.Ordinal))
                return Task.FromResult(AuthenticateResult.Fail("Invalid API Key"));

            // Create a trivial authenticated user
            Claim[] claims = new[] { new Claim(ClaimTypes.Name, "cv-client") };
            ClaimsIdentity identity = new ClaimsIdentity(claims, Scheme.Name);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
            AuthenticationTicket ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
