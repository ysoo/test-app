using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Primitives;
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
            if (context.User.Identity?.IsAuthenticated == false)
            {
                _logger.LogInformation("User is not authenticated");
                await _next(context);
                return;
            }

            var authResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            // Use the Microsoft.Identity.Web extension method GetTokenValue to extract the token.
            var token = authResult?.Ticket?.Properties?.GetTokenValue("user_impersonation_token");
            
            if (!string.IsNullOrEmpty(token))
            {
                _logger.LogInformation("Token found in authentication ticket. Adding token to request headers.");
                context.Request.Headers.Authorization = new StringValues($"Bearer {token}");
            } else {
                try
                {
                    _logger.LogInformation("User is authenticated, attempting to get user_impersonation token");
                    // Get token with user_impersonation scope
                    token = await _tokenAcquisition.GetAccessTokenForUserAsync(
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
            await _next(context);
        }
    }
} 