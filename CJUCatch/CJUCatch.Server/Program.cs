using CJUCatch.Server.Hubs;
using CJUCatch.Server.Options;
using CJUCatch.Server.Services;

var builder = WebApplication.CreateBuilder(args);

var securityOptions = builder.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ?? new SecurityOptions();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = securityOptions.MaxHttpRequestBytes;
});

builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection(SecurityOptions.SectionName));
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = securityOptions.MaxHubMessageBytes;
});
builder.Services.AddSingleton<InstanceRegistry>();
builder.Services.AddSingleton<AttemptLimiter>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (context.Request.ContentLength is > 0 and var contentLength &&
        contentLength > securityOptions.MaxHttpRequestBytes)
    {
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Payload is too large.",
        });
        return;
    }

    await next();
});

app.MapGet("/", () => Results.Ok(new
{
    app = "CJUCatch.Server",
    status = "online",
    message = "Security-first instance server bootstrap is running.",
}));

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    utc = DateTimeOffset.UtcNow,
}));

app.MapGet("/instances", (InstanceRegistry registry) => Results.Ok(registry.ListInstances()));

app.MapHub<PresenceHub>("/hubs/presence");

app.Run();
