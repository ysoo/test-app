using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestApp.Services; // Namespace for our service
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;

var builder = WebApplication.CreateBuilder(args);

// Enable detailed JWT errors in development
IdentityModelEventSource.ShowPII = true;

// Configure authentication to validate tokens issued for management.azure.com
builder.Services.AddAuthentication(options =>
{
    // Specify that JWT bearer is used to authenticate requests
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddMicrosoftIdentityWebApp(options =>
{
    // Bind settings from configuration (AzureAd section)
    builder.Configuration.GetSection("AzureAd").Bind(options);

    // Override the audience to accept tokens for management.azure.com
    options.TokenValidationParameters.ValidAudience = "https://management.azure.com";
})
// You can keep token acquisition if your API later calls downstream APIs
.EnableTokenAcquisitionToCallDownstreamApi()
.AddInMemoryTokenCaches();

builder.Services.AddAuthorization(options =>
{
    // Add a policy that requires the "user_impersonation" scope in the token
    options.AddPolicy("RequireUserImpersonation", policy =>
        policy.RequireClaim("scp", "user_impersonation"));
});

// Register the tag updater service (your existing logic)
builder.Services.AddScoped<ResourceManagerService>();

// Add MVC services (controllers and views)
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure error handling based on environment
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Add logging middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Request received: {Path}", context.Request.Path);
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        logger.LogInformation("Authorization header present");
    }
    
    await next();
});

// No authentication middleware is added since this UI is public.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// A health endpoint for monitoring the service status.
app.MapGet("/health", () => "Healthy");

// Start the application on port 8080.
app.Run("http://+:8080");
