using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Api_TaskManager.Tests.Integration
{
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authHeader = Request.Headers["Authorization"];
            if (authHeader.Count == 0)
            {
                return Task.FromResult(AuthenticateResult.Fail("No authorization header"));
            }

            var userIdHeader = Request.Headers["X-Test-UserId"];
            var roleHeader = Request.Headers["X-Test-Role"];

            var userId = userIdHeader.Count > 0 ? userIdHeader[0] ?? "1" : "1";
            var role = roleHeader.Count > 0 ? roleHeader[0] ?? "User" : "User";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}