﻿using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;
using MySqlConnector;
using PetaPoco;
using Prices.Data;

namespace Prices.Services;

/// <summary></summary>
public sealed class PricesDataSet {

    /// <summary>待機間隔</summary>
    public const int WaitInterval = 1000 / 60;

    /// <summary>ロードでエラーした場合の最大試行回数</summary>
    private const int MaxRetryCount = 10;

    /// <summary>ロードでエラーした場合のリトライ間隔</summary>
    private const int RetryInterval = 1000 / 30;

    /// <summary>PetaPocoをDI</summary>
    private Database database { get; set; }

    /// <summary>データベース名</summary>
    private string databaseName;

    /// <summary>コンストラクタ</summary>
    public PricesDataSet (Database database) {
        this.database = database;
        var words = database.ConnectionString.Split (['=', ';']);
        var index = Array.IndexOf (words, "database");
        if (index < 0 || index > words.Length) {
            throw new InvalidOperationException ("The database name could not be determined.");
        }
        databaseName = words [index + 1];
    }

    /// <summary>初期化</summary>
    public async Task InitializeAsync() {
        if (!IsInitialized) {
            try {
                await LoadAsync ();
                IsInitialized = true;
            }
            catch (Exception e) {
                System.Diagnostics.Debug.WriteLine (e);
                isLoading = false;
                IsUnavailable = true;
            }
        }
    }

    /// <summary>初期化済み</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>初期化が済み、ロード中でない</summary>
    public bool IsReady => IsInitialized && !isLoading;

    /// <summary>初期化に失敗</summary>
    public bool IsUnavailable { get; private set; }

    /// <summary>表示と編集を抜きで行う (通常は税込みで行う)</summary>
    /// <remarks>本来は、独立したサービスとして構成すべきセッション毎の設定</remarks>
    public bool TaxExcluded { get; set; }

    /// <summary>(再)読み込み</summary>
    /// <remarks>既に読み込み中なら単に完了を待って戻る、再読み込み中でも以前のデータが有効</remarks>
    public async Task LoadAsync () {
        if (isLoading) {
            while (isLoading) {
                await Task.Delay (WaitInterval);
            }
            return;
        }
        isLoading = true;
        for (var i = 0; i < MaxRetryCount; i++) {
            var result = await GetSetAsync ();
            if (result.IsSuccess) {
                (Categories, Products, Stores, Prices) = result.Value;
                isLoading = false;
                return;
            }
            await Task.Delay (RetryInterval);
        }
        throw new TimeoutException ("The maximum number of retries for LoadAsync was exceeded.");
    }
    private bool isLoading;

    /// <summary>指定クラスのモデルインスタンスを取得</summary>
    public List<T> GetAll<T>() where T : class => (
            typeof (T) == typeof (Category) ? Categories as List<T> :
            typeof (T) == typeof (Product) ? Products as List<T> :
            typeof (T) == typeof (Store) ? Stores as List<T> :
            typeof (T) == typeof (Price) ? Prices as List<T> :
            null
        ) ?? new();

    /// <summary>ロード済みのモデルインスタンス</summary>
    public List<Category> Categories { get; private set; } = new ();

    /// <summary>ロード済みのモデルインスタンス</summary>
    public List<Product> Products { get; private set; } = new ();

    /// <summary>ロード済みのモデルインスタンス</summary>
    public List<Store> Stores { get; private set; } = new ();

    /// <summary>ロード済みのモデルインスタンス</summary>
    public List<Price> Prices { get; private set; } = new ();

    /// <summary>有効性の検証</summary>
    public bool Valid
        => IsReady
        && Categories != null && !Categories.Exists (i => i.Id <= 0)
        && Products != null && !Products.Exists (i => i.Id <= 0)
        && Stores != null && !Stores.Exists (i => i.Id <= 0)
        && Prices != null && !Prices.Exists (i => i.Id <= 0)
        ;

    /// <summary>一覧セットをアトミックに取得</summary>
    private async Task<Result<(List<Category>, List<Product>, List<Store>, List<Price>)>> GetSetAsync () {
        return await ProcessAndCommitAsync<(List<Category>, List<Product>, List<Store>, List<Price>)> (async () => {
            var categories = await database.FetchAsync<Category> (Category.BaseSelectSql);
            var products = await database.FetchAsync<Product> (Product.BaseSelectSql);
            var stores = await database.FetchAsync<Store> (Store.BaseSelectSql);
            var prices = await database.FetchAsync<Price> (Price.BaseSelectSql);
            return (categories, products, stores, prices);
        });
    }

    /// <summary>単一アイテムを取得 (Idで特定) 【注意】総リストとは別オブジェクトになる</summary>
    public async Task<Result<T?>> GetItemByIdAsync<T> (T item) where T : BaseModel<T>, new() {
        var table = GetSqlName<T> ();
        return await ProcessAndCommitAsync (async () => (await database.FetchAsync<T?> (
            $"select {table}.* from {table} where {table}.Id = @Id;",
            item
        )).Single ());
    }

    /// <summary>アイテムの更新サブ処理 トランザクション内専用</summary>
    private async Task<int> UpdateItemAsync<T> (T item) where T : BaseModel<T>, new() {
        var table = GetSqlName<T> ();
        return await database.ExecuteAsync (
            @$"update {table} set {GetSettingSql<T> ()} where {table}.Id = @Id",
            item
        );
    }

    /// <summary>アイテムの更新</summary>
    public async Task<Result<int>> UpdateAsync<T> (T item) where T : BaseModel<T>, new() {
        var result = await ProcessAndCommitAsync (async () => {
            item.Version++;
            return await UpdateItemAsync<T> (item);
        });
        if (result.IsSuccess && result.Value <= 0) {
            result.Status = Status.MissingEntry;
        }
        return result;
    }

    /// <summary>単一アイテムの追加</summary>
    public async Task<Result<T>> AddAsync<T> (T item) where T : BaseModel<T>, new() {
        var result = await ProcessAndCommitAsync (async () => {
            item.Id = await database.ExecuteScalarAsync<int> (
                @$"insert into {GetSqlName<T> ()} ({GetColumnsSql<T> ()}) values ({GetValuesSql<T> ()});
                select LAST_INSERT_ID();",
                item
            );
            return item.Id > 0 ? 1 : 0;
        });
        if (result.IsSuccess && result.Value <= 0) {
            result.Status = Status.MissingEntry;
        }
        if (result.IsFailure) {
            item.Id = default;
        } else {
            // ロード済みに追加
            GetAll<T> ().Add (item);
        }
        return new (result.Status, item);
    }

    /// <summary>単一アイテムの削除</summary>
    public async Task<Result<int>> RemoveAsync<T> (T item) where T : BaseModel<T>, new() {
        var result = await ProcessAndCommitAsync (async () => {
            var original = await GetItemByIdAsync<T> (item);
            if (original.IsSuccess && original.Value != null) {
                if (item.Version == original.Value.Version) {
                    return await database.ExecuteAsync (
                        $"delete from {GetSqlName<T> ()} where Id = @Id",
                        item
                    );
                } else {
                    throw new MyDataSetException ($"Version mismatch between {item.Version} and {original.Value.Version}");
                }
            }
            return 0;
        });
        if (result.IsSuccess && result.Value <= 0) {
            result.Status = Status.MissingEntry;
        }
        // ロード済みから除去
        GetAll<T> ().Remove (item);
        return result;
    }

    /// <summary>SQLで使用するテーブル名またはカラム名を得る</summary>
    /// <param name="name">プロパティ名</param>
    /// <returns>テーブル名またはカラム名</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string GetSqlName<T> (string? name = null) where T : class {
        var type = typeof (T);
        if (name == null) {
            return type.GetCustomAttribute<PetaPoco.TableNameAttribute> ()?.Value ?? type.Name;
        } else {
            return type.GetProperty (name)?.GetCustomAttribute<PetaPoco.ColumnAttribute> ()?.Name ?? name;
        }
    }

    /// <summary>更新用カラム&値SQL</summary>
    /// <remarks>ColumnでありかつVirtualColumnでないプロパティだけを対象とする</remarks>
    private string GetSettingSql<T> (bool withId = false) where T : class {
        var result = string.Empty;
        var properties = typeof (T).GetProperties (BindingFlags.Instance | BindingFlags.Public);
        if (properties != null) {
            result = string.Join (',', Array.ConvertAll (properties, property => {
                var @virtual = property.GetCustomAttribute<VirtualColumnAttribute> ();
                var attribute = property.GetCustomAttribute<ColumnAttribute> ();
                return @virtual == null && attribute != null && (withId || (attribute.Name ?? property.Name) != "Id")
                    ? $"{attribute.Name ?? property.Name}=@{property.Name}"
                    : "";
            }).ToList ().FindAll (i => i != ""));
        }
        return result;
    }

    /// <summary>追加用値SQL</summary>
    /// <remarks>ColumnでありかつVirtualColumnでないプロパティだけを対象とする</remarks>
    public static string GetValuesSql<T> (int index = -1, bool withId = false) where T : class {
        var result = string.Empty;
        var properties = typeof (T).GetProperties (BindingFlags.Instance | BindingFlags.Public);
        if (properties != null) {
            result = string.Join (',', Array.ConvertAll (properties, property => {
                var @virtual = property.GetCustomAttribute<VirtualColumnAttribute> ();
                var attribute = property.GetCustomAttribute<ColumnAttribute> ();
                return @virtual == null && attribute != null && (withId || (attribute.Name ?? property.Name) != "Id")
                    ? $"@{property.Name}{(index >= 0 ? $"_{index}" : "")}"
                    : "";
            }).ToList ().FindAll (i => i != ""));
        }
        return result;
    }

    /// <summary>追加用カラムSQL</summary>
    /// <remarks>ColumnでありかつVirtualColumnでないプロパティだけを対象とする</remarks>
    public static string GetColumnsSql<T> (bool withId = false) where T : class {
        var result = string.Empty;
        var properties = typeof (T).GetProperties (BindingFlags.Instance | BindingFlags.Public);
        if (properties != null) {
            result = string.Join (',', Array.ConvertAll (properties, property => {
                var @virtual = property.GetCustomAttribute<VirtualColumnAttribute> ();
                var attribute = property.GetCustomAttribute<ColumnAttribute> ();
                return @virtual == null && attribute != null && (withId || (attribute.Name ?? property.Name) != "Id")
                    ? attribute.Name ?? property.Name
                    : "";
            }).ToList ().FindAll (i => i != ""));
        }
        return result;
    }

    /// <summary>必須項目のチェック</summary>
    public static bool EntityIsValid<T> (T? item, bool withId = false) where T : BaseModel<T>, new () {
        if (item == null || withId && (item.Id <= 0 || item.Modefied == default)) { return false; }
        var properties = new List<PropertyInfo> ();
        foreach (var property in typeof (T).GetProperties (BindingFlags.Instance | BindingFlags.Public) ?? []) {
            var required = property.GetCustomAttribute<RequiredAttribute> ();
            var attribute = property.GetCustomAttribute<ColumnAttribute> ();
            if (attribute != null && required != null) {
                properties.Add (property);
            }
        }
        foreach (var property in properties) {
            var value = property.GetValue (item);
            var type = value?.GetType ();
            if (type == typeof (string)) {
                if (string.IsNullOrEmpty (property.GetValue (item) as string)) {
                    return false;
                }
            } else if (type?.IsClass == true) {
                if (property.GetValue (item) == null) {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>アイテムリストから辞書型パラメータを生成する</summary>
    private Dictionary<string, object?> GetParamDictionary<T> (IEnumerable<T> values, bool withId = false) {
        var parameters = new Dictionary<string, object?> ();
        var properties = new List<PropertyInfo> ();
        foreach (var property in typeof (T).GetProperties (BindingFlags.Instance | BindingFlags.Public) ?? []) {
            var attribute = property.GetCustomAttribute<ColumnAttribute> ();
            if (attribute != null && property.GetCustomAttribute<VirtualColumnAttribute> () == null
                && (withId || (attribute.Name ?? property.Name) != "Id")) {
                properties.Add (property);
            }
        }
        var i = 0;
        foreach (var value in values) {
            foreach (var property in properties) {
                parameters.Add ($"{property.Name}_{i}", property.GetValue (value));
            }
            i++;
        }
        return parameters;
    }

    /// <summary>処理を実行しコミットする、例外またはエラーがあればロールバックする</summary>
    /// <typeparam name="T">返す値の型</typeparam>
    /// <param name="process">処理</param>
    /// <returns>成功またはエラーの状態と値のセット</returns>
    public async Task<Result<T>> ProcessAndCommitAsync<T> (Func<Task<T>> process) {
        var result = default (T)!;
        await database.BeginTransactionAsync ();
        try {
            result = await process ();
            await database.CompleteTransactionAsync ();
            return new (Status.Success, result);
        }
        catch (Exception ex) when (ex.IsDeadLock ()) {
            await database.AbortTransactionAsync ();
            // デッドロックならエスカレート
            throw;
        }
        catch (Exception ex) when (ex.TryGetStatus (out var status)) {
            // エラー扱いの例外
            await database.AbortTransactionAsync ();
            System.Diagnostics.Debug.WriteLine (ex);
            if (status == Status.CommandTimeout) {
                // タイムアウトならエスカレート
                throw;
            }
            return new (status, result);
        }
        catch (Exception) {
            await database.AbortTransactionAsync ();
            throw;
        }
    }

    /// <summary>テーブルの次の自動更新値を得る</summary>
    /// <remarks>MySQL/MariaDBに依存</remarks>
    public async Task<long> GetAutoIncremantValueAsync<T> () where T : class {
        // 開始Idを取得
        var Id = 0L;
        try {
            // 待避と設定 (SQLに勝手に'SELECT'を挿入しない)
            var enableAutoSelectBackup = database.EnableAutoSelect;
            database.EnableAutoSelect = false;
            try {
                try {
                    // 設定 (情報テーブルの即時更新を設定)
                    await database.ExecuteAsync ("set session information_schema_stats_expiry=1;");
                }
                catch (MySqlException ex) when (ex.Message.StartsWith ("Unknown system variable")) {
                    // MariaDBはこの変数をサポートしていない
                    System.Diagnostics.Debug.WriteLine ($"Server not supported 'information_schema_stats_expiry'\n{ex}");
                }
                // 次の自動更新値の取得
                Id = await database.SingleAsync<long> (
                    $"select AUTO_INCREMENT from information_schema.tables where TABLE_SCHEMA='{databaseName}' and TABLE_NAME='{GetSqlName<T> ()}';"
                );
            }
            finally {
                // 設定の復旧
                database.EnableAutoSelect = enableAutoSelectBackup;
            }
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine ($"Get auto_increment number\n{ex}");
        }
        if (Id <= 0) {
            // 開始Idの取得に失敗
            throw new NotSupportedException ("Failed to get auto_increment value.");
        }
        return Id;
    }

}

public static class ExceptionToErrorHelper {
    /// <summary>例外メッセージからエラーへの変換</summary>
    internal static readonly Dictionary<(Type type, string message), Status> ExceptionToErrorDictionary = new () {
        { (typeof (MyDataSetException), "Missing entry"), Status.MissingEntry },
        { (typeof (MyDataSetException), "Duplicate entry"), Status.DuplicateEntry },
        { (typeof (MyDataSetException), "The Command Timeout expired"), Status.CommandTimeout },
        { (typeof (MyDataSetException), "Version mismatch"), Status.VersionMismatch },
        { (typeof (MyDataSetException), "Cannot add or update a child row: a foreign key constraint fails"), Status.ForeignKeyConstraintFails },
        { (typeof (MyDataSetException), "Deadlock found"), Status.DeadlockFound },
        { (typeof (MySqlException), "Duplicate entry"), Status.DuplicateEntry },
        { (typeof (MySqlException), "The Command Timeout expired"), Status.CommandTimeout },
        { (typeof (MySqlException), "Version mismatch"), Status.VersionMismatch },
        { (typeof (MySqlException), "Cannot add or update a child row: a foreign key constraint fails"), Status.ForeignKeyConstraintFails },
        { (typeof (MySqlException), "Deadlock found"), Status.DeadlockFound },
        { (typeof (MySqlException), "Data too long for column"), Status.DataTooLong },
    };
    /// <summary>例外がエラーか判定して該当するエラー状態を出力する</summary>
    public static bool TryGetStatus (this Exception ex, out Status status) {
        foreach (var pair in ExceptionToErrorDictionary) {
            if (ex.GetType () == pair.Key.type && ex.Message.StartsWith (pair.Key.message, StringComparison.CurrentCultureIgnoreCase)) {
                status = pair.Value;
                return true;
            }
        }
        status = Status.Unknown;
        return false;
    }
    /// <summary>例外はデッドロックである</summary>
    public static bool IsDeadLock (this Exception ex) => ex is MySqlException && ex.Message.StartsWith ("Deadlock found");
    /// <summary>逆引き</summary>
    public static Exception GetException (this Status status) {
        if (ExceptionToErrorDictionary.ContainsValue (status)) {
            return new MyDataSetException (ExceptionToErrorDictionary.First (p => p.Value == status).Key.message);
        }
        return new Exception ("Unknown exception");
    }
}

public static class StatusHelper {
    /// <summary>結果の状態の名前</summary>
    private static readonly Dictionary<Status, string> StatusNameDictionary;
    /// <summary>コンストラクタ</summary>
    static StatusHelper () {
        StatusNameDictionary = new () {
            { Status.Success, "成功" },
            { Status.Unknown, "不詳の失敗" },
            { Status.MissingEntry, "エントリの消失" },
            { Status.DuplicateEntry, "エントリの重複" },
            { Status.CommandTimeout, "タイムアウト" },
            { Status.VersionMismatch, "バージョンの不整合" },
            { Status.ForeignKeyConstraintFails, "外部キー制約の違反" },
            { Status.DeadlockFound, "デッドロック" },
            { Status.DataTooLong, "サイズ超過" },
        };
    }
    /// <summary>結果の状態の名前</summary>
    public static string GetName (this Status status)
        => StatusNameDictionary .ContainsKey (status)
        ? StatusNameDictionary [status]
        : throw new ArgumentOutOfRangeException ($"Invalid status value {status}.");
    /// <summary>結果の一覧から最初に見つかった失敗状態を返す、失敗がなければ成功を返す</summary>
    public static Status FirstFailedState<T> (this List<Result<T>> results)
        => results.Find (r => r.IsFatal)?.Status ?? results.Find (r => r.IsFailure)?.Status ?? Status.Success;
}

/// <summary>結果の状態</summary>
public enum Status {
    /// <summary>成功</summary>
    Success = default,
    /// <summary>不詳の失敗</summary>
    Unknown,
    /// <summary>エントリの消失</summary>
    MissingEntry,
    /// <summary>エントリの重複</summary>
    DuplicateEntry,
    /// <summary>タイムアウト</summary>
    CommandTimeout,
    /// <summary>バージョンの不整合</summary>
    VersionMismatch,
    /// <summary>外部キー制約の違反</summary>
    ForeignKeyConstraintFails,
    /// <summary>デッドロック</summary>
    DeadlockFound,
    /// <summary>サイズ超過</summary>
    DataTooLong,
}

/// <summary>結果の状態と値</summary>
public class Result<T> {
    public Status Status { get; internal set; }
    public T Value { get; init; } = default!;
    public string StatusName => Status.GetName ();
    internal Result () { }
    internal Result (Status status, T value) { Status = status; Value = value; }
    /// <summary>成功である</summary>
    public bool IsSuccess => Status == Status.Success;
    /// <summary>失敗である</summary>
    public bool IsFailure => Status != Status.Success;
    /// <summary>致命的である</summary>
    public bool IsFatal => Status == Status.CommandTimeout || Status == Status.DeadlockFound;
    /// <summary>成功なら値、失敗なら例外</summary>
    public T ValueOrThrow => IsSuccess ? Value : throw new NotSupportedException (Status.ToString ());
    /// <summary>逆引き</summary>
    public Exception Exception => Status.GetException ();
    /// <summary>文字列化</summary>
    public override string ToString () => $"{{{Status}: {Value}}}";
}

/// <summary>内部で使用する例外</summary>
[Serializable]
internal class MyDataSetException : Exception {
    internal MyDataSetException () : base () { }
    internal MyDataSetException (string message) : base (message) { }
    internal MyDataSetException (string message, Exception innerException) : base (message, innerException) { }
}

/// <summary>仮想カラム属性</summary>
/// <remarks>計算列から(PetaPocoにマッピングさせて)取り込むが、フィールドが実在しないので書き出さない</remarks>
[AttributeUsage (AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public class VirtualColumnAttribute : Attribute {
    public VirtualColumnAttribute () { }
}
