using Prices.Services;
using PetaPoco;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;
using Tetr4lab;

namespace Prices.Data;

/// <summary>モデルに必要な静的プロパティ</summary>
public interface IPricesBaseModel : IBaseModel { }

/// <summary>基底モデル</summary>
[PrimaryKey ("id", AutoIncrement = true), ExplicitColumns]
public abstract class PricesBaseModel<T> : BaseModel<T>, IEquatable<T> where T : PricesBaseModel<T>, new () {
    /// <summary>備考</summary>
    [Column ("remarks")] public string? Remarks { get; set; }

    /// <summary>カラムの最大文字列長</summary>
    /// <remarks>設定がなければ0を返す</remarks>
    public int GetMaxLength (string name)
        => GetType ().GetProperty (name)?.GetCustomAttribute<StringLengthAttribute> ()?.MaximumLength ?? 0;

    /// <summary>参照数</summary>
    public abstract int ReferenceCount (PricesDataSet set);

    /// <summary>クローン</summary>
    public virtual new T Clone () {
        var clone = base.Clone ();
        clone.Remarks = Remarks;
        return clone;
    }

    /// <summary>値のコピー</summary>
    public virtual new T CopyTo (T destination) {
        base.CopyTo (destination);
        destination.Remarks = Remarks;
        return destination;
    }
}
