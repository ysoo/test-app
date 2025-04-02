using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Configuration;
using YarpK8sProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Register our provider as the IProxyConfigProvider
builder.Services.AddSingleton<IProxyConfigProvider, K8sProxyConfigProvider>();

// Add the reverse proxy
builder.Services.AddReverseProxy();

var app = builder.Build();

// Add logging middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation(
        "Request: {Method} {Path}",
        context.Request.Method,
        context.Request.Path);
    
    await next();
});

app.MapReverseProxy();

app.Run();
