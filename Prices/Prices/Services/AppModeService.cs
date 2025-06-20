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
public interface IAppModeService {
    event PropertyChangedEventHandler? PropertyChanged;
    AppMode CurrentMode { get; }
    AppMode RequestedMode { get; }
    void SetMode (AppMode mode);
    void SetRequestedMode (AppMode mode);
}

    /// <summary>アプリモード管理</summary>
public class AppModeService : IAppModeService, INotifyPropertyChanged {
    /// <summary>プロパティの変更を通知するイベント</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>プロパティの変更を通知するヘルパーメソッド</summary>
    /// <param name="propertyName"></param>
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
    public void SetMode (AppMode mode) {
        CurrentMode = mode;
    }

    /// <summary>モードをリクエスト</summary>
    public void SetRequestedMode (AppMode mode) {
        RequestedMode = mode;
    }
}

