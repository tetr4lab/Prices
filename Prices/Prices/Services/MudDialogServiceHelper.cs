using Microsoft.AspNetCore.Components;
using MudBlazor;
using Prices.Data;
using Prices.Components.Pages;

namespace Prices.Services;

public static class MudDialogServiceHelper {

    /// <summary>リロードの確認</summary>
    public static async Task<DialogResult?> ReloadConfirmation (this IDialogService dialogService, IEnumerable<string?> message, string cancellationLabel = "")
        => await Confirmation (dialogService, message, title: "リロードの確認", acceptionLabel: "Reload", acceptionColor: Color.Success, cancellationLabel: cancellationLabel);

    /// <summary>汎用の確認</summary>
    public static async Task<DialogResult?> Confirmation (this IDialogService dialogService, IEnumerable<string?> message, string? title = null, MaxWidth width = MaxWidth.Small, DialogPosition position = DialogPosition.Center, string acceptionLabel = "Ok", Color acceptionColor = Color.Success, string cancellationLabel = "Cancel", Color cancellationColor = Color.Default) {
        var options = new DialogOptions { MaxWidth = width, FullWidth = true, Position = position, BackdropClick = false, };
        var parameters = new DialogParameters {
            ["Contents"] = message,
            ["AcceptionLabel"] = acceptionLabel,
            ["AcceptionColor"] = acceptionColor,
            ["CancellationLabel"] = cancellationLabel,
            ["CancellationColor"] = cancellationColor,
        };
        return await (await dialogService.ShowAsync<ConfirmationDialog> (title ?? "確認", parameters, options)).Result;
    }

}
