using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestApp.Services; // Namespace for our service

var builder = WebApplication.CreateBuilder(args);

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

// No authentication middleware is added since this UI is public.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// A health endpoint for monitoring the service status.
app.MapGet("/health", () => "Healthy");

// Start the application on port 8080.
app.Run("http://+:8080");
