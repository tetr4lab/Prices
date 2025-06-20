using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Prices.Services;

/// <summary>アプリのモード</summary>
public enum AppMode {
    None = -1,
    Boot = 0,
    Prices,
    Products,
    Stores,
    Categories,
}

/// <summary>アプリモード管理</summary>
/// <remarks><example>Program.cs でのサービス登録<code>
/// builder.Services.AddScoped&lt;<see cref="IAppModeService"/>, <see cref="AppModeService"/>&gt; ();
/// </code></example></remarks>
public interface IAppModeService {
    /// <summary>プロパティの変更を通知するイベント</summary>
    /// <remarks><example>使用例<code>
    /// @implements IDisposable
    /// @inject IAppModeService AppModeService
    /// @code {
    ///     // イベントハンドラ
    ///     protected void OnAppModePropertyChanged (object? sender, PropertyChangedEventArgs e) {
    ///         if (e.PropertyName == &quot;CurrentMode&quot;) {
    ///             InvokeAsync (StateHasChanged); // モード変更による再描画
    ///         }
    ///         if (e.PropertyName == &quot;RequestedMode&quot; &amp;&amp; sender is IAppModeService service) {
    ///             if (service.RequestedMode != AppMode.None &amp;&amp; service.RequestedMode != service.CurrentMode) {
    ///                 if (true /*or 可否判断*/) {
    ///                     service.SetMode (service.RequestedMode); // モード変更要求を受け付けて実際に変更
    ///                 }
    ///                 service.SetRequestedMode (AppMode.None);
    ///             }
    ///         }
    ///     }
    ///     protected override void OnInitialized () {
    ///         base.OnInitialized ();
    ///         AppModeService.PropertyChanged += OnAppModePropertyChanged; // 購読開始
    ///     }
    ///     public void Dispose () {
    ///         AppModeService.PropertyChanged -= OnAppModePropertyChanged; // 購読終了
    ///     }
    /// }
    /// </code></example></remarks>
    event PropertyChangedEventHandler? PropertyChanged;
    /// <summary>現在のモード</summary>
    AppMode CurrentMode { get; }
    /// <summary>要求されたモード</summary>
    /// <remarks>See: <see cref="PropertyChanged"/></remarks>
    AppMode RequestedMode { get; }
    /// <summary>モードを設定</summary>
    void SetMode (AppMode mode);
    /// <summary>モードを要求</summary>
    /// <remarks>See: <see cref="PropertyChanged"/></remarks>
    void SetRequestedMode (AppMode mode);
}

    /// <summary>アプリモード管理</summary>
public class AppModeService : IAppModeService, INotifyPropertyChanged {
    /// <summary>プロパティの変更を通知するイベント</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>プロパティの変更を通知するヘルパーメソッド</summary>
    /// <param name="propertyName">変更されたプロパティとして通知に含める名前 (省略時は元の名前)</param>
    protected void OnPropertyChanged ([CallerMemberName] string? propertyName = null) {
        PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
    }

    /// <summary>アプリのモード</summary>
    public AppMode CurrentMode {
        get => _currentMode;
        protected set {
            if (_currentMode != value) {
                _currentMode = value;
                OnPropertyChanged ();
            }
        }
    }
    protected AppMode _currentMode = AppMode.Boot;

    /// <summary>リクエストされたアプリモード</summary>
    public AppMode RequestedMode {
        get => _requestedMode;
        protected set {
            if (_requestedMode != value) {
                _requestedMode = value;
                OnPropertyChanged ();
            }
        }
    }
    protected AppMode _requestedMode = AppMode.None;

    /// <summary>モードを設定</summary>
    /// <param name="mode">新しいモード</param>
    public void SetMode (AppMode mode) {
        CurrentMode = mode;
    }

    /// <summary>モードをリクエスト</summary>
    /// <param name="mode">要求するモード</param>
    public void SetRequestedMode (AppMode mode) {
        RequestedMode = mode;
    }
}

