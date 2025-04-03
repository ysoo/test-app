using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Yarp.ReverseProxy.Configuration;
using YarpK8sProxy.Configuration;
using System.Security.Cryptography.X509Certificates;
using YarpK8sProxy.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add HTTPS configuration
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // Configure HTTP endpoint
    serverOptions.ListenAnyIP(8080, listenOptions =>
    {
        var logger = serverOptions.ApplicationServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Configuring HTTP endpoint on port 8080");
    });

    // Configure HTTPS endpoint on the same port
    serverOptions.ListenAnyIP(8443, listenOptions =>
    {
        var certPath = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Path");
        var keyPath = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__KeyPath");

        if (File.Exists(certPath) && File.Exists(keyPath))
        {
            try
            {
                var cert = X509Certificate2.CreateFromPemFile(certPath, keyPath);
                listenOptions.UseHttps(cert, httpsOptions => 
                {
                    httpsOptions.ServerCertificate = cert;
                    httpsOptions.AllowAnyClientCertificate();
                });
                var logger = serverOptions.ApplicationServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("HTTPS configured successfully on port 8443");
            }
            catch (Exception ex)
            {
                var logger = serverOptions.ApplicationServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Failed to load certificate");
            }
        }
    });
});

// Add authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddMicrosoftIdentityWebApp(options =>
{
    builder.Configuration.GetSection("AzureAd").Bind(options);
    options.Scope.Add("https://management.azure.com/user_impersonation");
    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = context =>
        {
            // Check if we're requesting consent explicitly
            if (context.Properties.Items.ContainsKey(OpenIdConnectDefaults.RedirectUriForCodePropertiesKey))
            {
                // Add prompt=consent only when explicitly requested
                context.ProtocolMessage.Prompt = "consent";
            }
            return Task.CompletedTask;
        },
       OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            context.HandleResponse();
            return Task.CompletedTask;
        },
        // Handle token validation errors
        OnRemoteFailure = context =>
        {
            Console.WriteLine($"Remote authentication failure: {context.Failure?.Message}");
            
            // Check if this is a consent error
            if (context.Failure?.Message?.Contains("AADSTS65001") == true || 
                context.Failure?.Message?.Contains("consent") == true)
            {
                // Redirect to a page that explains consent is required
                context.Response.Redirect("/Home/ConsentRequired");
                context.HandleResponse();
            }
            
            return Task.CompletedTask;
        },
        // This is the important part - handle post-authentication redirect
        OnTokenValidated = async context =>
        {
            var tokenAcquisition = context.HttpContext.RequestServices
                .GetRequiredService<ITokenAcquisition>();
             try
            {
                // Try to get the token with user_impersonation scope
                var token = await tokenAcquisition.GetAccessTokenForUserAsync(
                    new[] { "https://management.azure.com/user_impersonation" },
                    authenticationScheme: OpenIdConnectDefaults.AuthenticationScheme);
                
                // Store the token for forwarding to the backend
                context.Properties.Items["user_impersonation_token"] = token;
            }
            catch (MicrosoftIdentityWebChallengeUserException)
            {
                // User hasn't consented to user_impersonation yet
                context.Properties.Items["needs_user_impersonation"] = "true";
            }
        }
    };
})
.EnableTokenAcquisitionToCallDownstreamApi()
.AddInMemoryTokenCaches();

builder.Services.Configure<MicrosoftIdentityOptions>(options =>
{
    options.ResponseType = "code";
    // Force HTTPS for the redirect URI
    options.Instance = "https://login.microsoftonline.com/";
    options.CallbackPath = "/signin-oidc";
});

// Register our provider
builder.Services.AddSingleton<IProxyConfigProvider, K8sProxyConfigProvider>();

// Add the reverse proxy
builder.Services.AddReverseProxy();

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("authenticated", policy =>
        policy.RequireAuthenticatedUser());
});

var app = builder.Build();

// Add HTTPS redirection before other middleware
app.UseHttpsRedirection();

// Add logging middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation(
        "Request: {Method} {Scheme} {Path} {Host}",
        context.Request.Method,
        context.Request.Scheme,
        context.Request.Path,
        context.Request.Host);
    
    try 
    {
        await next();
        
        logger.LogInformation(
            "Response: {StatusCode} for {Method} {Path}",
            context.Response.StatusCode,
            context.Request.Method,
            context.Request.Path);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, 
            "Error processing request {Method} {Path}",
            context.Request.Method,
            context.Request.Path);
        throw;
    }
});

// Add the middleware before UseAuthentication
app.UseMiddleware<TokenForwardingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.Run();
