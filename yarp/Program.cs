using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Configuration;
using YarpK8sProxy.Configuration;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

// Add HTTPS configuration
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(8080, listenOptions =>
    {
        var certPath = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Path");
        var keyPath = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__KeyPath");

        if (File.Exists(certPath) && File.Exists(keyPath))
        {
            try
            {
                var cert = X509Certificate2.CreateFromPemFile(certPath, keyPath);
                listenOptions.UseHttps(cert);
                var logger = serverOptions.ApplicationServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("HTTPS configured successfully");
            }
            catch (Exception ex)
            {
                var logger = serverOptions.ApplicationServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Failed to load certificate");
            }
        }
        else
        {
            var logger = serverOptions.ApplicationServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("No certificate found, using HTTP");
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
    options.Events = new OpenIdConnectEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError("Authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        }
    };
});

// Register our provider as the IProxyConfigProvider
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

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.Run();
