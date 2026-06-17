
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Prevent app crash when a background service hits a transient SQL timeout.
// Without this, HostOptions defaults to StopHost, which kills the entire process.
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

var umbracoBuilder = builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers();

// Azure Blob Storage for media: only active outside local development.
// Connection string is injected via Azure App Service environment variables.
if (!builder.Environment.IsDevelopment())
{
    umbracoBuilder.AddAzureBlobMediaFileSystem();
}

umbracoBuilder.Build();

WebApplication app = builder.Build();


await app.BootUmbracoAsync();


app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
