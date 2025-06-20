using Microsoft.AspNetCore.Components;
using Prices.Services;

namespace Prices.Components.Pages;

/// <summary>コンポーネント基底クラス</summary>
public class PricesComponentBase : ComponentBase {
    [Inject] protected AppLockState UiState { get; set; } = null!;
    [Inject] protected IAppModeService AppModeManager { get; set; } = null!;
}
