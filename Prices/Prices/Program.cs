﻿using Microsoft.AspNetCore.Components.Server.Circuits;
using MudBlazor;
using MudBlazor.Services;
using PetaPoco;
using Prices.Components;
using Prices.Services;
using Tetr4lab;
using System.Globalization;
using Prices.Components.Pages;

var builder = WebApplication.CreateBuilder (args);
var connectionString = $"database=prices;{builder.Configuration.GetConnectionString ("Host")}{builder.Configuration.GetConnectionString ("Account")}Allow User Variables=true;";

// Add services to the container.
builder.Services.AddRazorComponents ()
    .AddInteractiveServerComponents ()
    .AddInteractiveWebAssemblyComponents ();

// MudBlazor
builder.Services.AddMudServices (config => {
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 10000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
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

#if NET8_0_OR_GREATER
// ページにカスケーディングパラメータ`Task<AuthenticationState>`を提供
builder.Services.AddCascadingAuthenticationState ();
#endif

// UIロック状態
builder.Services.AddScoped<AppLockState> ();

// アプリモード
builder.Services.AddScoped<IAppModeService, AppModeService> ();

// 回路の閉鎖を検出するCircuitHandlerをセッション毎に使う
builder.Services.AddScoped<CircuitHandler, CircuitClosureDetector> ();

// PetaPoco with MySqlConnector
builder.Services.AddScoped (_ => (Database) new MySqlDatabase (connectionString, "MySqlConnector"));

// HTTP Client
builder.Services.AddHttpClient ();

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
var basePath = builder.Configuration ["AppBasePath"];
if (!string.IsNullOrEmpty (basePath)) {
    app.UsePathBase (basePath);
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

app.UseStaticFiles ();
app.UseAntiforgery ();
app.UseAuthentication ();
app.UseAuthorization ();

app.MapRazorComponents<App> ()
    .AddInteractiveServerRenderMode ()
    .AddInteractiveWebAssemblyRenderMode ()
    .AddAdditionalAssemblies (typeof (Prices.Client._Imports).Assembly);

System.Diagnostics.Debug.WriteLine ("Initialized");
app.Run ();
