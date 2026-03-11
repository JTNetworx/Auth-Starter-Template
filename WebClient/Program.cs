using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using WebClient;
using WebClient.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiUrl = builder.Configuration["Backend:ApiUrl"] ?? "https://localhost:7170/api";

// ── HTTP Clients ──────────────────────────────────────────────────────────────

// "public" — no auth handler; used for login, register, refresh
builder.Services.AddHttpClient("public", client =>
    client.BaseAddress = new Uri(apiUrl.TrimEnd('/') + "/"));

// "api" — has AuthHttpMessageHandler; used for authenticated calls
builder.Services.AddHttpClient("api", client =>
    client.BaseAddress = new Uri(apiUrl.TrimEnd('/') + "/"))
    .AddHttpMessageHandler<AuthHttpMessageHandler>();

// ── Auth Services ─────────────────────────────────────────────────────────────

builder.Services.AddScoped<TokenStorageService>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddScoped<AuthHttpMessageHandler>();

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// ── API Services ──────────────────────────────────────────────────────────────

builder.Services.AddScoped<IAuthApiService, AuthApiService>();
builder.Services.AddScoped<IUserApiService, UserApiService>();
builder.Services.AddScoped<ICountryApiService, CountryApiService>();
builder.Services.AddScoped<IPasskeyApiService, PasskeyApiService>();

// ── App Services ──────────────────────────────────────────────────────────────

builder.Services.AddScoped<NotificationService>();

// ── MudBlazor ─────────────────────────────────────────────────────────────────

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 4000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
});

await builder.Build().RunAsync();
