using System.Globalization;
using Diapason.Web.Components;
using Microsoft.AspNetCore.Localization;

namespace Diapason.Web;

public static class DiapasonApp
{
    public static readonly CultureInfo[] Cultures = [new("it"), new("en")];

    /// <summary>Numbers and dates are shown in the Italian format whatever the language (spec 001, criterion 13).</summary>
    public static readonly CultureInfo Numbers = new("it-IT");

    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            // Fixed, so the static assets and settings are found also when the app is built by the tests.
            ApplicationName = typeof(DiapasonApp).Assembly.GetName().Name,
        });

        builder.Services.AddOptions<DiapasonOptions>()
            .Bind(builder.Configuration.GetSection(DiapasonOptions.Section))
            .PostConfigure(o =>
            {
                var folder = builder.Configuration[DiapasonOptions.DataFolderVariable];
                if (!string.IsNullOrWhiteSpace(folder)) o.DataFolder = folder;
            });
        var port = builder.Configuration.GetSection(DiapasonOptions.Section).Get<DiapasonOptions>()?.Port ?? new DiapasonOptions().Port;
        // Loopback only, whatever ASPNETCORE_URLS says: the data are personal (README, "Rules").
        builder.WebHost.ConfigureKestrel(k => k.ListenLocalhost(port));

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<DataState>();
        builder.Services.AddLocalization(o => o.ResourcesPath = "Resources");
        builder.Services.Configure<RequestLocalizationOptions>(o =>
        {
            o.DefaultRequestCulture = new RequestCulture(Cultures[0]);
            o.SupportedCultures = Cultures;
            o.SupportedUICultures = Cultures;
            o.RequestCultureProviders = [new CookieRequestCultureProvider()];
        });
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();

        var app = builder.Build();
        if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseRequestLocalization();
        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapGet("/culture", (string name, string? redirect, HttpContext http) =>
        {
            if (Cultures.Any(c => c.Name == name))
                http.Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(name)),
                    new CookieOptions { MaxAge = TimeSpan.FromDays(365), HttpOnly = true, SameSite = SameSiteMode.Lax });
            // Only local paths, so the link cannot send anyone elsewhere.
            return Results.LocalRedirect(redirect is { Length: > 0 } && redirect.StartsWith('/') && !redirect.StartsWith("//")
                ? redirect : "/");
        });
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

        // Read once at start-up, so the first page and the log show the state of the folder (criterion 12).
        app.Lifetime.ApplicationStarted.Register(() => _ = app.Services.GetRequiredService<DataState>().Current);
        return app;
    }
}
