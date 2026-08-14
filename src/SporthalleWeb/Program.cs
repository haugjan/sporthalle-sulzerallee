
using System.Globalization;

var swissCulture = new CultureInfo("de-CH");
CultureInfo.DefaultThreadCurrentCulture   = swissCulture;
CultureInfo.DefaultThreadCurrentUICulture = swissCulture;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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
        var absDataSource = Path.IsPathRooted(dataSource)
            ? dataSource
            : Path.GetFullPath(dataSource.Replace('/', Path.DirectorySeparatorChar),
                               builder.Environment.ContentRootPath);
        var resolved = $"Data Source={absDataSource};Foreign Keys=True;Pooling=True";
        builder.Configuration[connKey] = resolved;

        Directory.CreateDirectory(Path.GetDirectoryName(absDataSource)!);
        using (var init = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={absDataSource}"))
        {
            init.Open();
            using var wal = init.CreateCommand();
            wal.CommandText = "PRAGMA journal_mode=WAL;";
            wal.ExecuteNonQuery();
        }
    }
}

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

if (builder.Environment.IsDevelopment())
{
    builder.Services.PostConfigure<OpenIddict.Server.AspNetCore.OpenIddictServerAspNetCoreOptions>(options =>
        options.DisableTransportSecurityRequirement = true);
}

if (!builder.Environment.IsDevelopment())
{
    umbracoBuilder.AddAzureBlobMediaFileSystem();
}

umbracoBuilder.Build();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

if (!app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var host = context.Request.Host.Host;
        if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            var proto = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? "https";
            var url = $"{proto}://{host[4..]}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
            context.Response.Redirect(url, permanent: true);
            return;
        }
        if ((host.Equals("admin.sporthalle-sulzerallee.ch", StringComparison.OrdinalIgnoreCase) ||
             host.Equals("admin-dev.sporthalle-sulzerallee.ch", StringComparison.OrdinalIgnoreCase)) &&
            !context.Request.Path.StartsWithSegments("/umbraco"))
        {
            var proto = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? "https";
            context.Response.Redirect($"{proto}://{context.Request.Host}/umbraco", permanent: false);
            return;
        }
        await next(context);
    });
}

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
    });

await app.RunAsync();
