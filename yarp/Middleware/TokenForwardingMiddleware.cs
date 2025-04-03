using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;

namespace YarpK8sProxy.Middleware
{
    public class TokenForwardingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly ILogger<TokenForwardingMiddleware> _logger;

        public TokenForwardingMiddleware(
            RequestDelegate next,
            ITokenAcquisition tokenAcquisition,
            ILogger<TokenForwardingMiddleware> logger)
        {
            _next = next;
            _tokenAcquisition = tokenAcquisition;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                try
                {
                    _logger.LogInformation("User is authenticated, attempting to get user_impersonation token");
                    // Get token with user_impersonation scope
                    var token = await _tokenAcquisition.GetAccessTokenForUserAsync(
                        new[] { "https://management.azure.com/user_impersonation" },
                        authenticationScheme: OpenIdConnectDefaults.AuthenticationScheme);

                    _logger.LogInformation("Successfully obtained user_impersonation token");

                    // Add token to forwarded headers
                    context.Request.Headers.Authorization = 
                        new Microsoft.Extensions.Primitives.StringValues($"Bearer {token}");
                    
                    _logger.LogInformation("Added token to request headers");
                }
                catch (MicrosoftIdentityWebChallengeUserException ex)
                {
                    // Redirect to consent page
                    _logger.LogWarning(ex, "Need consent for user_impersonation");
                    context.Response.Redirect("/Account/Consent");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting access token");
                    context.Response.Redirect("/Home/Error");
                    return;
                }
            }
            else
            {
                _logger.LogInformation("User is not authenticated");
            }

            await _next(context);
        }
    }
} 