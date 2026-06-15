var builder = WebApplication.CreateBuilder(args);

// Add logging to see YARP routing decisions in development console
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add YARP Reverse Proxy services and load routing configuration from appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();

// Map YARP endpoint to act as the reverse proxy load balancer
app.MapReverseProxy();

app.Run();
