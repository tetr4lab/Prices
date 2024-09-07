using Prices.Services;
using PetaPoco;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using MudBlazor;
using System.Xml.Linq;
using Prices.Components.Pages;
using System.Data;

namespace Prices.Data;

[TableName ("prices")]
public class Price : BaseModel<Price>, IBaseModel {
    /// <inheritdoc/>
    public static string TableLabel => "価格";

    /// <inheritdoc/>
    public static Dictionary<string, string> Label { get; } = new Dictionary<string, string> {
        { nameof (Id), "ID" },
        { nameof (PriceWithTax), "価格" },
        { nameof (Quantity), "数量" },
        { nameof (UnitPrice), "単価" },
        { nameof (TaxRate), "税率" },
        { nameof (Category), "カテゴリ" },
        { nameof (ProductId), "製品" },
        { nameof (StoreId), "店舗" },
        { nameof (Confirmed), "確認日時" },
        { nameof (Remarks), "備考" },
    };

    /// <inheritdoc/>
    public static string BaseSelectSql {
        get {
            var table = PricesDataSet.GetSqlName<Price> ();
            return $@"select {table}.* from {table}
                left join products on products.id = product_id
                order by products.category_id, product_id, unit_price
                ;";
        }
    }

    /// <inheritdoc/>
    public static string UniqueKeysSql => "";

    [Column ("price")] public float? PriceWithTax { get; set; } = null;
    [Column ("quantity")] public float? Quantity { get; set; } = null;
    [Column ("unit_price"), VirtualColumn] public float? UnitPrice { get; set; } = null;
    [Column ("tax_rate"), Required] public float TaxRate { get; set; } = 0;
    [Column ("product_id"), Required] public long ProductId { get; set; } = 0;
    [Column ("store_id"), Required] public long StoreId { get; set; } = 0;
    [Column ("confirmed")] public DateTime? Confirmed { get; set; } = null;

    /// <summary>製品</summary>
    public Product? Product (PricesDataSet set) => set.Products.Find (i => i.Id == ProductId);

    /// <summary>カテゴリ</summary>
    public Category? Category (PricesDataSet set) => set.Categories.Find (i => i.Id == Product (set)?.CategoryId);

    /// <summary>店舗</summary>
    public Store? Store (PricesDataSet set) => set.Stores.Find (i => i.Id == StoreId);

    /// <summary>税率(%)</summary>
    public int TaxPercentage {
        get => (int) (TaxRate * 100.0f);
        set => TaxRate = value / 100.0f;
    }

    /// <summary>税抜き価格</summary>
    public float? PriceWithoutTax {
        get => PriceWithTax / (1.0f + TaxRate);
        set => PriceWithTax = value * (1.0f + TaxRate);
    }

    /// 同じ製品の価格数が1(最後のひとつ)なら1、それ以外(まだ他にある)なら0
    public override int ReferenceCount (PricesDataSet set) => set.Prices.Count (i => i.ProductId == ProductId) == 1 ? 1 : 0;

    /// <inheritdoc/>
    public override string? [] SearchTargets => [
        $"#{Id}.",
        $"¥{PriceWithTax:#,0}.",
        $"¥{PriceWithoutTax:#,0}.",
        $"x{Quantity:#,0}.",
        $"{TaxPercentage}%",
        Confirmed?.ToShortDateString (),
        $"p{ProductId}.", 
        $"s{StoreId}.", 
        Remarks
    ];

    /// <summary>ノーマルコンストラクタ</summary>
    public Price () { }

    /// 指定の価格を生成
    public Price (PricesDataSet dataSet, long productId, long storeId = 0) {
        ProductId = productId;
        StoreId = storeId > 0 ? storeId : (dataSet.Stores.Count > 0 ? dataSet.Stores.Last ().Id : 0);
        var categoryId = dataSet.Products.Find (i => i.Id == productId)?.CategoryId;
        TaxRate = categoryId == null ? 0 : dataSet.Categories.Find (i => i.Id == categoryId)?.TaxRate ?? 0;
    }

    /// <inheritdoc/>
    public override Price Clone () {
        var item = base.Clone ();
        item.PriceWithTax = PriceWithTax;
        item.Quantity = Quantity;
        item.UnitPrice = UnitPrice;
        item.TaxRate = TaxRate;
        item.ProductId = ProductId;
        item.StoreId = StoreId;
        item.Confirmed = Confirmed;
        return item;
    }

    /// <inheritdoc/>
    public override Price CopyTo (Price destination) {
        destination.PriceWithTax = PriceWithTax;
        destination.Quantity = Quantity;
        destination.UnitPrice = UnitPrice;
        destination.TaxRate = TaxRate;
        destination.ProductId = ProductId;
        destination.StoreId = StoreId;
        destination.Confirmed = Confirmed;
        return base.CopyTo (destination);
    }

    /// <inheritdoc/>
    public override bool Equals (Price? other) =>
        other != null
        && Id == other.Id
        && PriceWithTax == other.PriceWithTax
        && Quantity == other.Quantity
        && UnitPrice == other.UnitPrice
        && TaxRate == other.TaxRate
        && ProductId == other.ProductId
        && StoreId == other.StoreId
        && Confirmed == other.Confirmed
        && Remarks == other.Remarks
    ;

    /// <inheritdoc/>
    public override int GetHashCode () => HashCode.Combine (Id, PriceWithTax, Quantity, UnitPrice, TaxRate, ProductId, StoreId, Confirmed);

    /// <inheritdoc/>
    public override string ToString () => $"{TableLabel} {Id}: {ProductId} {StoreId} ¥{PriceWithTax} {Quantity} @{UnitPrice} {TaxRate * 100:F2}% {Confirmed} \"{Remarks}\"";
}
