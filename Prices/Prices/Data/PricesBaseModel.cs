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
    /// <summary>カラムの最大文字列長</summary>
    /// <remarks>設定がなければ0を返す</remarks>
    public int GetMaxLength (string name)
        => GetType ().GetProperty (name)?.GetCustomAttribute<StringLengthAttribute> ()?.MaximumLength ?? 0;

    /// <summary>参照数</summary>
    public abstract int ReferenceCount (PricesDataSet set);
}
