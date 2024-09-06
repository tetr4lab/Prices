using Prices.Services;
using PetaPoco;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using MudBlazor;

namespace Prices.Data;

[TableName ("categories")]
public class Category : BaseModel<Category>, IBaseModel {
    /// <summary>食品の税率</summary>
    public static readonly float TaxRateForFood = 0.08f;

    /// <summary>非食品の税率</summary>
    public static readonly float TaxRateForNonFood = 0.10f;

    /// <inheritdoc/>
    public static string TableLabel => "カテゴリ";

    /// <inheritdoc/>
    public static Dictionary<string, string> Label { get; } = new Dictionary<string, string> {
        { nameof (Id), "ID" },
        { nameof (Name), "カテゴリ名" },
        { nameof (IsFood), "食品" },
        { nameof (TaxRate), "税率"},
        { nameof (Remarks), "備考" },
    };

    /// <inheritdoc/>
    public static string BaseSelectSql {
        get {
            var table = PricesDataSet.GetSqlName<Category> ();
            return $"select {table}.* from {table} order by id;";
        }
    }

    /// <inheritdoc/>
    public static string UniqueKeysSql => "";

    [Column ("name"), StringLength (255), Required] public string Name { get; set; } = "";
    [Column ("is_food"), Required] public bool IsFood { get; set; } = false;
    [Column ("tax_rate"), Required] public float TaxRate { get; set; } = TaxRateForNonFood;

    /// <summary>税率(%)</summary>
    public int TaxPercentage {
        get => (int) (TaxRate * 100);
        set => TaxRate = value / 100f;
    }

    /// <inheritdoc/>
    public override int ReferenceCount (PricesDataSet set) => set.Products.Count (i => i.CategoryId == Id);

    /// <inheritdoc/>
    public override Category Clone () {
        var item = base.Clone ();
        item.Name = Name;
        item.IsFood = IsFood;
        item.TaxRate = TaxRate;
        return item;
    }

    /// <inheritdoc/>
    public override Category CopyTo (Category destination) {
        destination.Name = Name;
        destination.IsFood = IsFood;
        destination.TaxRate = TaxRate;
        return base.CopyTo (destination);
    }

    /// <inheritdoc/>
    public override bool Equals (Category? other) =>
        other != null
        && Id == other.Id
        && Name == other.Name
        && IsFood == other.IsFood
        && TaxRate == other.TaxRate
        && Remarks == other.Remarks
    ;

    /// <inheritdoc/>
    public override int GetHashCode () => HashCode.Combine (Id, Name, IsFood, TaxRate, Remarks);

    /// <inheritdoc/>
    public override string ToString () => $"{TableLabel} {Id}: {Name}{(IsFood ? " (食品)" : "")} 税{TaxRate * 100:F2}% \"{Remarks}\"";

}
