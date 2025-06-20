using Microsoft.AspNetCore.Components;
using Prices.Services;

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
public class PricesComponentBase : ComponentBase {
    [Inject] protected IAppLockState UiState { get; set; } = null!;
    [Inject] protected IAppModeService<AppMode> AppModeService { get; set; } = null!;
}
