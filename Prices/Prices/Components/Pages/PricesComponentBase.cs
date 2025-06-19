using Microsoft.AspNetCore.Components;
using Prices.Services;

namespace Prices.Components.Pages;

public class PricesComponentBase : ComponentBase {
    [CascadingParameter] public AppLockState AppLock { get; set; } = null!;
}
