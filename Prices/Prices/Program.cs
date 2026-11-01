using Microsoft.AspNetCore.Components.Server.Circuits;
using MudBlazor;
using MudBlazor.Services;
using PetaPoco;
using Prices.Components;
using Prices.Services;
using Tetr4lab.Auth;
using Tetr4lab.Razor;
using Tetr4lab.Server;
using System.Globalization;
using Prices.Components.Pages;

System.Text.Encoding.RegisterProvider (System.Text.CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder (args);
var connectionString = $"database=prices;{builder.Configuration.GetConnectionString ("Host")}{builder.Configuration.GetConnectionString ("Account")}Allow User Variables=true;";

// Add services to the container.
builder.Services.AddRazorComponents ()
    .AddInteractiveServerComponents ()
    .AddHubOptions (options => options.MaximumReceiveMessageSize = 64 * 1024) // 受信サイズ最大64KB(デフォルト32KB)
    ;

// MudBlazor
builder.Services.AddMudServices (config => {
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 10000;
    config.SnackbarConfiguration.HideTransitionDuration = 1000;
    config.SnackbarConfiguration.ShowTransitionDuration = 1000;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
    config.SnackbarConfiguration.MaximumOpacity = 80;
});

// クッキーとグーグルの認証を構成
builder.Services.AddAuthentication (
    builder.Configuration ["Authentication:Google:ClientId"]!,
    builder.Configuration ["Authentication:Google:ClientSecret"]!
);

// メールアドレスを保持するクレームを要求する認可用のポリシーを構成
await builder.Services.AddAuthorizationAsync (
    $"database=accounts;{builder.Configuration.GetConnectionString ("Host")}{builder.Configuration.GetConnectionString ("Account")}Allow User Variables=true;",
    new () {
        { "Admin", "Administrator" },
        { "Users", "Family" },
    }
);

// ページにカスケーディングパラメータ`Task<AuthenticationState>`を提供
builder.Services.AddCascadingAuthenticationState ();

// 認証状態
builder.Services.AddScoped<AuthStateProvider> ();

// UIロック状態
builder.Services.AddScoped<IAppLockState, AppLockState> ();

// アプリモード
builder.Services.AddScoped<IAppModeService<AppMode>, AppModeService<AppMode>> ();

// 回路の閉鎖を検出するCircuitHandlerをセッション毎に使う
builder.Services.AddScoped<CircuitHandler, CircuitClosureDetector> ();

// PetaPoco with MySqlConnector
builder.Services.AddScoped (_ => (Database) new MySqlDatabase (connectionString, "MySqlConnector"));

// DataSet
builder.Services.AddScoped<PricesDataSet> ();

var app = builder.Build ();

// Application level Culture
app.UseRequestLocalization (new RequestLocalizationOptions ()
    .SetDefaultCulture ("ja-JP")
    .AddSupportedCultures (["ja-JP",])
    .AddSupportedUICultures (["ja-JP",])
);
Thread.CurrentThread.CurrentCulture = new CultureInfo ("ja-JP");

// Application Base Path
var basePath = builder.Configuration ["AppBasePath"]?.TrimEnd ('/');
if (!string.IsNullOrEmpty (basePath)) {
    app.UsePathBase (basePath);
    app.MapBlazorHub ($"{basePath}/_blazor");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment ()) {
    app.UseWebAssemblyDebugging ();
} else {
    app.UseExceptionHandler ("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts ();
}

app.UseHttpsRedirection ();

app.UseAntiforgery ();
app.UseAuthentication ();
app.UseAuthorization ();

app.MapStaticAssets ();
app.MapRazorComponents<App> ()
    .AddInteractiveServerRenderMode ();

System.Diagnostics.Debug.WriteLine ("Initialized");
app.Run ();
