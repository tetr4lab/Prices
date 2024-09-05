using Prices.Services;
using PetaPoco;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;

namespace Prices.Data;

/// <summary>モデルに必要な静的プロパティ</summary>
public interface IBaseModel {
    /// <summary>テーブル名</summary>
    public static abstract string TableLabel { get; }
    /// <summary>列の名前</summary>
    public static abstract Dictionary<string, string> Label { get; }
}

/// <summary>基底モデル</summary>
[PrimaryKey ("id", AutoIncrement = true), ExplicitColumns]
public abstract class BaseModel<T> : IEquatable<T> where T : BaseModel<T>, new() {
    /// <summary>識別子</summary>
    [Column ("id"), Required] public long Id { get; set; }
    /// <summary>バージョン</summary>
    [Column ("version"), Required] public int Version { get; set; }
    /// <summary>生成日時</summary>
    [Column ("created"), VirtualColumn] public DateTime Created { get; set; }
    /// <summary>更新日時</summary>
    [Column ("modefied"), VirtualColumn] public DateTime Modefied { get; set; }
    /// <summary>備考</summary>
    [Column ("remarks")] public string? Remarks { get; set; }

    /// <summary>カラムの最大文字列長</summary>
    /// <remarks>設定がなければ0を返す</remarks>
    public int GetMaxLength (string name)
        => GetType ().GetProperty (name)?.GetCustomAttribute<StringLengthAttribute> ()?.MaximumLength ?? 0;

    /// <summary>参照数</summary>
    public abstract int ReferenceCount (PricesDataSet set);

    /// <summary>クローン</summary>
    public virtual T Clone ()
        => new T {
            Id = Id,
            Version = Version,
            Created = Created,
            Modefied = Modefied,
            Remarks = Remarks,
        };

    /// <summary>値のコピー</summary>
    public virtual T CopyTo (T destination) {
        destination.Id = Id;
        destination.Version = Version;
        destination.Created = Created;
        destination.Modefied = Modefied;
        destination.Remarks = Remarks;
        return destination;
    }

    /// <summary>内容の比較</summary>
    public abstract bool Equals (T? other);

    /// <summary>内容の比較</summary>
    public override bool Equals (object? obj) => Equals (obj as T);

    /// <summary>ハッシュコードの取得</summary>
    public abstract override int GetHashCode ();
}

/// <summary>モデルに必要な静的プロパティ</summary>
public interface IPricesModel {
    /// <summary>テーブル名</summary>
    public static abstract string TableLabel { get; }
    /// <summary>単位</summary>
    public static abstract string Unit { get; }
    /// <summary>列の名前</summary>
    public static abstract Dictionary<string, string> Label { get; }
    /// <summary>関係リスト名</summary>
    public static abstract string RelatedListName { get; }
    /// <summary>ユニーク識別子群のSQL表現</summary>
    public static abstract string UniqueKeysSql { get; }
}

/// <summary>基底モデル</summary>
/// <typeparam name="T1">自身</typeparam>
/// <typeparam name="T2">関係先</typeparam>
/// <remarks>派生先で必要に応じて`[TableName ("~")]`を加える</remarks>
[PrimaryKey ("Id", AutoIncrement = true), ExplicitColumns]
public abstract class PricesBaseModel<T1, T2> : IEquatable<T1>
    where T1 : PricesBaseModel<T1, T2>, new()
    where T2 : PricesBaseModel<T2, T1>, new() {

    /// <summary>ストアURL</summary>
    public virtual string StoreURL => "https://www.amazon.co.jp/gp/search/ref=nb_ss_gw/?__mk_ja_JP=%E3%82%AB%E3%82%BF%E3%82%AB%E3%83%8A&url=search-alias%3Dstripbooks&field-keywords=";

    /// <summary>行の名前 (代表的なカラムを参照)</summary>
    public abstract string? RowLabel { get; set; }

    /// <summary>検索対象 (複数のカラムを参照)</summary>
    public abstract string? [] SearchTargets { get; }

    /// <summary>ユニーク識別子</summary>
    public string UniqueKey => string.Join ('-', UniqueKeys);

    /// <summary>ユニーク識別子群</summary>
    public abstract string [] UniqueKeys { get; }

    /// <summary>識別子</summary>
    [Column] public long Id { get; set; }

    /// <summary>バージョン</summary>
    [Column] public int Version { get; set; }

    /// <summary>文字列によるIdリスト</summary>
    [Column, VirtualColumn]
    public string? _relatedIds { get; set; }

    /// <summary>数値によるIdリスト</summary>
    public List<long> RelatedIds {
        get => (__relatedIds ??= string.IsNullOrEmpty (_relatedIds)? new () : _relatedIds.Split (',').ToList ().ConvertAll (id => long.TryParse (id, out var Id) ? Id : 0)) ?? new ();
        set {
            _relatedIds = value == null ? null : string.Join (',', value);
            __relatedIds = default;
            __relatedItems = default;
        }
    }
    protected List<long>? __relatedIds { get; set; }

    /// <summary>関係先インスタンス</summary>
    /// <remarks>RelatedIdsがnullなら_relatedItemsもnullになって、次回に再度生成を試みる。nullでも空のリストを返す。</remarks>
    public List<T2> RelatedItems {
        get => (__relatedItems ??= DataSet == null ? null : RelatedIds.ConvertAll (id => DataSet.GetAll<T2> ().Find (item => item.Id == id) ?? new ())) ?? new ();
        set {
            _relatedIds = string.Join (',', value.ConvertAll (item => item.Id));
            __relatedIds = default;
            __relatedItems = default;
        }
    }
    protected List<T2>? __relatedItems { get; set; }

    /// <summary>所属するデータセット</summary>
    public PricesDataSet DataSet { get; set; } = default!;

    /// <summary>カラムの最大文字列長</summary>
    /// <remarks>設定がなければ0を返す</remarks>
    public int GetMaxLength (string name)
        => GetType ().GetProperty (name)?.GetCustomAttribute<StringLengthAttribute> ()?.MaximumLength ?? 0;

    /// <summary>クローン</summary>
    public virtual T1 Clone ()
        => new T1 {
            DataSet = DataSet,
            Id = Id,
            Version = Version,
            _relatedIds = new (_relatedIds),
            __relatedIds = default,
            __relatedItems = default,
        };

    /// <summary>値のコピー</summary>
    public virtual T1 CopyTo (T1 destination) {
        destination.DataSet = DataSet;
        destination.Id = Id;
        destination.Version = Version;
        destination._relatedIds = new (_relatedIds);
        destination.__relatedIds = default;
        destination.__relatedItems = default;
        return destination;
    }

    /// <summary>内容の比較</summary>
    public abstract bool Equals (T1? other);

    /// <summary>内容の比較</summary>
    public override bool Equals (object? obj) => Equals (obj as T1);

    /// <summary>ハッシュコードの取得</summary>
    public abstract override int GetHashCode ();

}

public static class PricesModelHelper {
    /// <summary>リスト内容の比較</summary>
    public static bool ContainsEquals<T> (this IEnumerable<T> items, IEnumerable<T> others) {
        if (items == null || others == null || items.Count () != others.Count ()) {
            return false;
        }
        foreach (var item in items) {
            if (!others.Contains (item)) { return false; }
        }
        return true;
    }
}
