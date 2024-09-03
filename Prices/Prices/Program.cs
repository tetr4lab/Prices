using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using MudBlazor;
using MudBlazor.Services;
using PetaPoco;
using Prices.Client.Pages;
using Prices.Components;
using Prices.Services;

var builder = WebApplication.CreateBuilder (args);
var connectionString = $"database=exlibris;{builder.Configuration.GetConnectionString ("Host")}{builder.Configuration.GetConnectionString ("Account")}Allow User Variables=true;";

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

// �N�b�L�[�ƃO�[�O���̔F�؂��\��
builder.Services.AddAuthentication (options => {
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
    .AddCookie ()
    .AddGoogle (options => {
        options.ClientId = builder.Configuration ["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration ["Authentication:Google:ClientSecret"]!;
    });

// ���[���A�h���X��ێ�����N���[����v������F�p�̃|���V�[���\��
builder.Services.AddAuthorization (options => {
    // �Ǘ���
    options.AddPolicy ("Admin", policyBuilder => {
        policyBuilder.RequireClaim (ClaimTypes.Email,
            builder.Configuration ["Identity:Claims:EmailAddress:Admin:0"]!
        );
    });
    // ��ʃ��[�U (�Ǘ��҂��܂�)
    options.AddPolicy ("Users", policyBuilder => {
        policyBuilder.RequireClaim (ClaimTypes.Email,
            builder.Configuration ["Identity:Claims:EmailAddress:Admin:0"]!,
            builder.Configuration ["Identity:Claims:EmailAddress:User:0"]!,
            builder.Configuration ["Identity:Claims:EmailAddress:User:1"]!
        );
    });
});

// PetaPoco with MySqlConnector
builder.Services.AddScoped (_ => (Database) new MySqlDatabase (connectionString, "MySqlConnector"));

// HTTP Client
builder.Services.AddHttpClient ();

// DataSet
builder.Services.AddScoped<PricesDataSet> ();

var app = builder.Build ();

// Application level Culture
app.UseRequestLocalization (new RequestLocalizationOptions ()
    .AddSupportedCultures (["ja-JP",])
    .AddSupportedUICultures (["ja-JP",]));

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
