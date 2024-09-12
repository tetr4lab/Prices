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
    /// <summary>データ取得SQL表現</summary>
    public static abstract string BaseSelectSql { get; }
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
    [Column ("modified"), VirtualColumn] public DateTime Modified { get; set; }
    /// <summary>備考</summary>
    [Column ("remarks")] public string? Remarks { get; set; }

    /// <summary>カラムの最大文字列長</summary>
    /// <remarks>設定がなければ0を返す</remarks>
    public int GetMaxLength (string name)
        => GetType ().GetProperty (name)?.GetCustomAttribute<StringLengthAttribute> ()?.MaximumLength ?? 0;

    /// <summary>参照数</summary>
    public abstract int ReferenceCount (PricesDataSet set);

    /// <summary>検索対象 (複数のカラムを参照)</summary>
    public abstract string? [] SearchTargets { get; }

    /// <summary>クローン</summary>
    public virtual T Clone ()
        => new T {
            Id = Id,
            Version = Version,
            Created = Created,
            Modified = Modified,
            Remarks = Remarks,
        };

    /// <summary>値のコピー</summary>
    public virtual T CopyTo (T destination) {
        destination.Id = Id;
        destination.Version = Version;
        destination.Created = Created;
        destination.Modified = Modified;
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
