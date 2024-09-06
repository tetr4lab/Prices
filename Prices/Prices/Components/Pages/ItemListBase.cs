﻿using Prices.Data;
using Prices.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Tetr4lab;
using Prices.Utilities;
using static Prices.Services.PricesDataSet;

namespace Prices.Components.Pages;

public class ItemListBase<T> : ComponentBase, IDisposable where T : BaseModel<T>, IBaseModel, new() {

    /// <summary>列挙する最大数</summary>
    protected const int MaxListingNumber = 500;

    [Inject] protected NavigationManager NavManager { get; set; } = null!;
    [Inject] protected PricesDataSet DataSet { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    /// <summary>検索文字列</summary>
    [CascadingParameter (Name = "Filter")] protected string? FilterText { get; set; }

    /// <summary>セクションラベル設定</summary>
    [CascadingParameter (Name = "Section")] protected EventCallback<string> SetSectionTitle { get; set; }

    /// <summary>セッション数の更新</summary>
    [CascadingParameter (Name = "Session")] protected EventCallback<int> UpdateSessionCount { get; set; }

    /// <summary>項目一覧</summary>
    protected List<T>? items => DataSet.IsReady ? DataSet.GetAll<T> () : null;

    /// <summary>選択項目</summary>
    protected T selectedItem { get; set; } = new ();

    /// <summary>初期化</summary>
    protected override async Task OnInitializedAsync () {
        await base.OnInitializedAsync ();
        await SetSectionTitle.InvokeAsync ($"{typeof (T).Name}s");
        // セッション数の変化を購読
        SessionCounter.Subscribe (this, () => InvokeAsync (StateHasChanged));
        _newItem = NewEditItem;
    }

    /// <summary>破棄</summary>
    public void Dispose () {
        SessionCounter.Unsubscribe (this);
    }

    /// <summary>描画後処理</summary>
    protected override async void OnAfterRender (bool firstRender) {
        base.OnAfterRender (firstRender);
        if (firstRender) {
            await DataSet.InitializeAsync ();
            StateHasChanged ();
        }
    }

    /// <summary>表示の更新</summary>
    protected void Update () { }// `=> StateHasChanged();`の処理は、コールバックを受けた時点で内部的に呼ばれているため、明示的な呼び出しは不要

    /// <summary>表示の更新と反映待ち</summary>
    protected async Task StateHasChangedAsync () {
        StateHasChanged ();
        await TaskEx.DelayOneFrame;
    }

    /// <summary>デフォルト項目数の設定</summary>
    protected override Task OnAfterRenderAsync (bool firstRender) {
        if (_table != null && !_inited) {
            _inited = true;
            _table.SetRowsPerPage (_pageSizeOptions [1]);
        }
        return base.OnAfterRenderAsync (firstRender);
    }
    protected bool _inited;
    protected MudTable<T>? _table;
    /// <summary>項目数の選択肢</summary>
    protected int [] _pageSizeOptions = { 10, 20, 25, 50, 100, 200, MaxListingNumber, };

    /// <summary>バックアップ</summary>
    protected virtual T backupedItem { get; set; }  = new ();

    /// <summary>型チェック</summary>
    private T GetT (object obj) => obj as T ?? throw new ArgumentException ($"The type of the argument '{obj.GetType ()}' does not match the expected type '{typeof (T)}'.");

    /// <summary>編集開始</summary>
    protected virtual void Edit (object obj) {
        var item = GetT (obj);
        backupedItem = item.Clone ();
        System.Diagnostics.Debug.WriteLine ($"Edit {backupedItem}");
    }

    /// <summary>編集完了</summary>
    protected virtual async void Commit (object obj) {
        var item = GetT (obj);
        if (EntityIsValid (item) && !backupedItem.Equals (item)) {
            var result = await DataSet.UpdateAsync (item);
            if (result.IsSuccess) {
                Snackbar.Add ($"{T.TableLabel}を更新しました。", Severity.Normal);
            } else {
                Snackbar.Add ($"{T.TableLabel}を更新できませんでした。", Severity.Error);
            }
        }
    }

    /// <summary>編集取消</summary>
    protected virtual void Cancel (object obj) {
        var item = GetT (obj);
        if (!backupedItem.Equals (item)) {
            backupedItem.CopyTo (item);
        }
        StateHasChanged ();
    }
    //protected bool _canCancelEdit;

    /// <summary>項目追加</summary>
    protected virtual async Task AddItem () {
        if (_isAdding || items == null) { return; }
        _isAdding = true;
        await StateHasChangedAsync ();
        if (EntityIsValid (_newItem)) {
            var result = await DataSet.AddAsync (_newItem);
            if (result.IsSuccess && _table != null) {
                _lastCreatedId = result.Value.Id;
                await DataSet.LoadAsync ();
                await StateHasChangedAsync ();
                _newItem = NewEditItem;
                if (items != null) {
                    var item = items.Find (i => i.Id == _lastCreatedId);
                    if (item != null) {
                        _table.SetEditingItem (item);
                        Edit (item);
                    }
                }
                Snackbar.Add ($"{T.TableLabel}を追加しました。", Severity.Normal);
            } else {
                _lastCreatedId = 0;
                Snackbar.Add ($"{T.TableLabel}を追加できませんでした。", Severity.Error);
            }
        }
        _isAdding = false;
    }
    protected bool _isAdding;
    protected T _newItem = default!;
    protected long _lastCreatedId;

    /// <summary>新規生成用の新規アイテム生成</summary>
    protected virtual T NewEditItem => new ();

    /// <summary>項目削除</summary>
    /// <param name="obj"></param>
    protected virtual async Task DeleteItem (object obj) {
        var item = GetT (obj);
        if (_table == null) { return; }
        // 確認ダイアログ
        var dialogResult = await DialogService.Confirmation ([$"以下の{T.TableLabel}を完全に削除します。", item.ToString ()], title: $"{T.TableLabel}削除", position: DialogPosition.BottomCenter, acceptionLabel: "Delete", acceptionColor: Color.Error);
        if (dialogResult != null && !dialogResult.Canceled && dialogResult.Data is bool ok && ok) {
            _table.SetEditingItem (null);
            var result = await DataSet.RemoveAsync (item);
            if (result.IsSuccess) {
                StateHasChanged ();
                Snackbar.Add ($"{T.TableLabel}を削除しました。", Severity.Normal);
            } else {
                Snackbar.Add ($"{T.TableLabel}を削除できませんでした。", Severity.Error);
            }
        }
    }

}


public class ItemListBase<TItem1, TItem2> : ComponentBase, IDisposable
    where TItem1 : PricesBaseModel<TItem1, TItem2>, IPricesModel, new()
    where TItem2 : PricesBaseModel<TItem2, TItem1>, IPricesModel, new() {

    /// <summary>列挙する最大数</summary>
    protected const int MaxListingNumber = 500;

    [Inject] protected NavigationManager NavManager { get; set; } = null!;
    [Inject] protected PricesDataSet DataSet { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    /// <summary>検索文字列</summary>
    [CascadingParameter (Name = "Filter")] protected string? FilterText { get; set; }

    /// <summary>セクションラベル設定</summary>
    [CascadingParameter (Name = "Section")] protected EventCallback<string> SetSectionTitle { get; set; }

    /// <summary>セッション数の更新</summary>
    [CascadingParameter (Name = "Session")] protected EventCallback<int> UpdateSessionCount { get; set; }

    /// <summary>項目一覧</summary>
    protected List<TItem1>? items => DataSet.IsReady ? DataSet.GetAll<TItem1> () : null;

    /// <summary>選択項目</summary>
    protected TItem1 selectedItem { get; set; } = new TItem1 ();

    /// <summary>複数選択項目</summary>
    protected HashSet<TItem1> selectedItems { get; set; } = new HashSet<TItem1> ();

    /// <summary>単一セッション</summary>
    protected bool isSingleSession => SessionCounter.Count <= 1;

    /// <summary>複数選択可否</summary>
    protected bool allowDeleteItems {
        get => _allowMultiSelection;
        set {
            if (_allowMultiSelection != value) {
                selectedItems = new ();
            }
            _allowMultiSelection = value;
        }
    }
    protected bool _allowMultiSelection = false;

    /// <summary>初期化</summary>
    protected override async Task OnInitializedAsync () {
        await base.OnInitializedAsync ();
        await SetSectionTitle.InvokeAsync ($"{typeof (TItem1).Name}s");
        // セッション数の変化を購読
        SessionCounter.Subscribe (this, () => InvokeAsync (StateHasChanged));
    }

    /// <summary>破棄</summary>
    public void Dispose () {
        SessionCounter.Unsubscribe (this);
    }

    /// <summary>描画後処理</summary>
    protected override async void OnAfterRender (bool firstRender) {
        base.OnAfterRender (firstRender);
        if (firstRender) {
            await DataSet.InitializeAsync ();
            StateHasChanged ();
        }
    }

    /// <summary>表示の更新</summary>
    protected void Update () { }// `=> StateHasChanged();`の処理は、コールバックを受けた時点で内部的に呼ばれているため、明示的な呼び出しは不要

    /// <summary>項目詳細・編集</summary>
    protected async void EditItem (TItem1? item) {
        if (item != null && !allowDeleteItems) {
            await OpenDialog (item);
        }
    }

    /// <summary>ダイアログを開く</summary>
    protected async Task OpenDialog (TItem1 item)
        => await (await DialogService.OpenItemDialog<TItem1, TItem2> (item, OpenRelationDialog, Update)).Result;

    /// <summary>関係ダイアログを開く</summary>
    protected async Task OpenRelationDialog (TItem2 item)
        => await (await DialogService.OpenItemDialog<TItem2, TItem1> (item, OpenDialog, Update)).Result;

    /// <summary>表示の更新と反映待ち</summary>
    protected async Task StateHasChangedAsync () {
        StateHasChanged ();
        await TaskEx.DelayOneFrame;
    }

    /// <summary>項目追加</summary>
    protected async void AddItem () {
        if (_isAdding) { return; }
        _isAdding = true;
        await StateHasChangedAsync ();
        await (await DialogService.OpenItemDialog<TItem1, TItem2> (new TItem1 { DataSet = DataSet, }, OpenRelationDialog, Update)).Result;
        _isAdding = false;
        StateHasChanged ();
    }
    protected bool _isAdding;

    /// <summary>項目一括削除</summary>
    protected async void DeleteItems () {
        if (SessionCounter.Count > 1) {
            Snackbar.Add ("複数の利用者がいる場合は使用できません。");
            return;
        }
        if (_isDeleting) { return; }
        _isDeleting = true;
        await StateHasChangedAsync ();
        if (allowDeleteItems && selectedItems.Count > 0) {
            // 確認ダイアログ
            var targetCount = selectedItems.Count;
            var contents = selectedItems.ToList () [..Math.Min (MaxListingNumber, selectedItems.Count)]
                .ConvertAll (i => $"「{i.Id}: {i.RowLabel}」");
            if (selectedItems.Count > MaxListingNumber) {
                contents.Add ($"他 {selectedItems.Count - MaxListingNumber:N0}{TItem1.Unit}");
            }
            contents.Insert (0, $"以下の{TItem1.TableLabel}({targetCount:N0}{TItem1.Unit})を完全に削除します。");
            var dialogResult = await DialogService.Confirmation (contents, title: $"{TItem1.TableLabel}一括削除", position: DialogPosition.BottomCenter, acceptionLabel: "Delete", acceptionColor: Color.Error);
            if (dialogResult != null && !dialogResult.Canceled && dialogResult.Data is bool ok && ok) {
                var resetAutoIncrement = new Result<int> (Status.Unknown, 0);
                // プログレスダイアログ
                dialogResult = await DialogService.Progress (async update => {
                    // 実際の削除
                    var result = await DataSet.RemoveRangeAsync<TItem1, TItem2> (selectedItems);
                    // 削除されたものをチェックから除外
                    var items = selectedItems;
                    selectedItems = new HashSet<TItem1> ();
                    var allItems = DataSet.GetAll<TItem1> ();
                    if (allItems != null) {
                        if (allItems.Count > 0) {
                            // 残りがある
                            foreach (var item in items) {
                                var finded = allItems.Find (i => item.Id == i.Id);
                                if (finded != null) {
                                    selectedItems.Add (finded);
                                }
                            }
                        } else {
                            // 全て削除されていたらオートインクリメントを初期化
                            resetAutoIncrement = await DataSet.ResetAutoIncrementAsync<TItem1, TItem2> ();
                        }
                    }
                    if (result.Value < targetCount) {
                        // 削除できなかったものがあるならリロード
                        await DataSet.LoadAsync ();
                    }
                    // 表示に反映
                    StateHasChanged ();
                    // 報告
                    var messages = new [] {
                        $"{TItem1.TableLabel} {result.Value:N0}/{targetCount:N0}を削除しました。",
                        result.Value < targetCount ? "一部または全部が削除できませんでした。" : null,
                        result.Status == Status.CommandTimeout
                          || result.Status == Status.DeadlockFound
                          ? $"{result.StatusName}があったので、時間をおいてやり直してみてください。" : null,
                        resetAutoIncrement.IsSuccess && resetAutoIncrement.Value > 0 ? $"{TItem1.TableLabel}が空になったので、Idを初期化しました。" : null,
                    };
                    Snackbar.Add (string.Join ("", messages), result.Value < targetCount ? Severity.Warning : Severity.Normal);
                    update (ProgressDialog.Acceptable, messages);
                }, indeterminate: true, title: "削除中", message: $"選択した{TItem1.TableLabel}を削除中です。アプリを終了しないでください。");
            }
        } else {
            Snackbar.Add ("項目が選択されていません。");
        }
        _isDeleting = false;
        StateHasChanged ();
    }
    protected bool _isDeleting;

    /// <summary>デフォルト項目数の設定</summary>
    protected override Task OnAfterRenderAsync (bool firstRender) {
        if (_table != null && !_inited) {
            _inited = true;
            _table.SetRowsPerPage (_pageSizeOptions [1]);
        }
        return base.OnAfterRenderAsync (firstRender);
    }
    protected bool _inited;
    protected MudTable<TItem1>? _table;
    /// <summary>項目数の選択肢</summary>
    protected int [] _pageSizeOptions = new [] { 10, 20, 25, 50, 100, 200, MaxListingNumber, };

    /// <summary>全ての検索語に対して対象列のどれかが真であれば真を返す</summary>
    protected bool FilterFunc (TItem1 item) {
        if (item != null && FilterText != null) {
            foreach (var word in FilterText.Split ([' ', '　', '\t', '\n'])) {
                if (!string.IsNullOrEmpty (word) && !Any (item.SearchTargets, word)) { return false; }
            }
            return true;
        }
        return false;
        // 対象カラムのどれかが検索語に適合すれば真を返す
        bool Any (IEnumerable<string?> targets, string word) {
            var eq = word.StartsWith ('=');
            word = word [(eq ? 1 : 0)..];
            var or = word.Split ('|');
            foreach (var target in targets) {
                if (!string.IsNullOrEmpty (target)) {
                    if (eq) {
                        // 検索語が'='で始まる場合は、以降がカラムと完全一致する場合に真を返す
                        if (or.Length > 1) {
                            // 検索語が'|'を含む場合は、'|'で分割したいずれかの部分と一致する場合に真を返す
                            foreach (var wd in or) {
                                if (target == wd) { return true; }
                            }
                        } else {
                            if (target == word) { return true; }
                        }
                    }
                    else {
                        // 検索語がカラムに含まれる場合に真を返す
                        if (or.Length > 1) {
                            // 検索語が'|'を含む場合は、'|'で分割したいずれかの部分がカラムに含まれる場合に真を返す
                            foreach (var wd in or) {
                                if (target.Contains (wd)) { return true; }
                            }
                        } else {
                            if (target.Contains (word)) { return true; }
                        }
                    }
                }
            }
            return false;
        }
    }

}
