using Microsoft.AspNetCore.Components;
using Prices.Services;

namespace Prices.Components.Pages;

/// <summary>コンポーネント基底クラス</summary>
public class PricesComponentBase : ComponentBase {
    [Inject] protected IAppLockState UiState { get; set; } = null!;
    [Inject] protected IAppModeService AppModeService { get; set; } = null!;
}
