using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Tetr4lab;

namespace Prices.Components.Pages;

/// <summary>アプリのモード</summary>
public enum AppMode {
    None = -1,
    Boot = 0,
    Prices,
    Products,
    Stores,
    Categories,
}

/// <summary>コンポーネント基底クラス</summary>
public class PricesComponentBase : ComponentBase, IDisposable {
    [Inject] protected IAppLockState UiState { get; set; } = null!;
    [Inject] protected IAppModeService<AppMode> AppModeService { get; set; } = null!;

    /// <summary>認証状況を得る</summary>
    [CascadingParameter] protected Task<AuthenticationState> AuthState { get; set; } = default!;

    /// <summary>認証済みID</summary>
    protected virtual AuthedIdentity? Identity { get; set; }

    /// <summary>ユーザ識別子</summary>
    protected virtual string UserIdentifier => Identity?.Identifier ?? "unknown";

    /// <summary>アプリモードが変化した</summary>
    protected virtual async void OnAppModeChanged (object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName != "RequestedMode") {
            await InvokeAsync (StateHasChanged);
        }
    }

    /// <summary>アプリロックが変化した</summary>
    protected virtual async void OnAppLockChanged (object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName == "IsLocked") {
            // MainLayoutでも再描画されるが、こちらのボタンのDisabledに反映されない(こちらの再描画が起きない)場合があるため
            await InvokeAsync (StateHasChanged);
        }
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync () {
        await base.OnInitializedAsync ();
        // 購読開始
        UiState.PropertyChanged += OnAppLockChanged;
        AppModeService.PropertyChanged += OnAppModeChanged;
        // 認証・認可
        Identity = await AuthState.GetIdentityAsync ();
    }

    /// <summary>購読終了</summary>
    public virtual void Dispose () {
        UiState.PropertyChanged -= OnAppLockChanged;
        AppModeService.PropertyChanged -= OnAppModeChanged;
    }
}
