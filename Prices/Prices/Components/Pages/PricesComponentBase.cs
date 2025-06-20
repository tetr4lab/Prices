using Microsoft.AspNetCore.Components;
using Prices.Services;

namespace Prices.Components.Pages;

/// <summary>コンポーネント基底クラス</summary>
public class PricesComponentBase : ComponentBase {
    [Inject] protected AppLockState AppLockState { get; set; } = null!;
    [Inject] protected AppModeService AppModeManager { get; set; } = null!;
}
