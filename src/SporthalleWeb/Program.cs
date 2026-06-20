
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Resolve SQLite Data Source to an absolute path using content root.
// A relative path in the connection string resolves from Directory.GetCurrentDirectory()
// which may differ from the content root when dotnet run is invoked from the repo root.
var sqliteProviderKey = "ConnectionStrings:umbracoDbDSN_ProviderName";
if (builder.Configuration[sqliteProviderKey] == "Microsoft.Data.Sqlite")
{
    const string connKey = "ConnectionStrings:umbracoDbDSN";
    var connStr = builder.Configuration[connKey] ?? string.Empty;
    const string prefix = "Data Source=";
    var start = connStr.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
    if (start >= 0)
    {
        var valueStart = start + prefix.Length;
        var end = connStr.IndexOf(';', valueStart);
        var dataSource = end >= 0 ? connStr[valueStart..end] : connStr[valueStart..];
        // Resolve relative path to absolute, then strip Cache=Shared (triggers SQLite URI
        // mode on Windows which breaks absolute Windows paths) and Mode= (default is
        // ReadWriteCreate anyway). Always rebuild with a clean minimal connection string.
        var absDataSource = Path.IsPathRooted(dataSource)
            ? dataSource
            : Path.GetFullPath(dataSource.Replace('/', Path.DirectorySeparatorChar),
                               builder.Environment.ContentRootPath);
        var resolved = $"Data Source={absDataSource};Foreign Keys=True;Pooling=True";
        builder.Configuration[connKey] = resolved;

        // Pre-create the SQLite file so Umbraco's availability check can open it.
        // Without this the check fails with SQLITE_CANTOPEN because the file doesn't
        // exist yet and the check opens with ReadWrite (no-create) mode.
        Directory.CreateDirectory(Path.GetDirectoryName(absDataSource)!);
        if (!File.Exists(absDataSource))
        {
            using var init = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={absDataSource}");
            init.Open();
        }
    }
}

// Prevent app crash when a background service hits a transient SQL timeout.
// Without this, HostOptions defaults to StopHost, which kills the entire process.
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();

var umbracoBuilder = builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers();

// Allow HTTP in local development (OpenIddict requires HTTPS by default).
if (builder.Environment.IsDevelopment())
{
    builder.Services.PostConfigure<OpenIddict.Server.AspNetCore.OpenIddictServerAspNetCoreOptions>(options =>
        options.DisableTransportSecurityRequirement = true);
}

// Azure Blob Storage for media: only active outside local development.
// Connection string is injected via Azure App Service environment variables.
if (!builder.Environment.IsDevelopment())
{
    umbracoBuilder.AddAzureBlobMediaFileSystem();
}

umbracoBuilder.Build();

WebApplication app = builder.Build();


await app.BootUmbracoAsync();

// Serve wwwroot static files before Umbraco's media middleware so that
// /media/* files committed to wwwroot/media/ are reachable directly.
app.UseStaticFiles();

app.MapBlazorHub();
app.MapRazorPages();

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
        u.EndpointRouteBuilder.MapBlazorHub();
    });

await app.RunAsync();
