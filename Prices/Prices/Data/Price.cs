using Prices.Services;
using PetaPoco;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using MudBlazor;
using System.Xml.Linq;

namespace Prices.Data;

[TableName ("prices")]
public class Price : BaseModel<Price> {
    /// <inheritdoc/>
    public static string TableLabel => "価格";

    /// <inheritdoc/>
    public static Dictionary<string, string> Label { get; } = new Dictionary<string, string> {
        { nameof (Id), "ID" },
        { nameof (PriceWithTax), "価格(税込)" },
        { nameof (Quantity), "数量" },
        { nameof (UnitPrice), "単価" },
        { nameof (TaxRate), "税率" },
        { nameof (ProductId), "製品" },
        { nameof (StoreId), "店舗" },
        { nameof (Confirmed), "確認日時" },
        { nameof (Remarks), "備考" },
    };

    /// <inheritdoc/>
    public static string UniqueKeysSql => "";

    [Column ("price")] public float PriceWithTax { get; set; }
    [Column ("quantity")] public float Quantity { get; set; }
    [Column ("unit_price"), VirtualColumn] public float UnitPrice { get; set; }
    [Column ("tax_rate"), Required] public float TaxRate { get; set; } = 0;
    [Column ("product_id"), Required] public long ProductId { get; set; } = 0;
    [Column ("store_id"), Required] public long StoreId { get; set; } = 0;
    [Column ("confirmed"), Required] public DateTime Confirmed { get; set; }

    /// <summary>製品</summary>
    public Product? Product (PricesDataSet set) => set.Products.Find (i => i.Id == ProductId);

    /// <summary>カテゴリ</summary>
    public Category? Category (PricesDataSet set) => set.Categories.Find (i => i.Id == Product (set)?.Id);

    /// <summary>店舗</summary>
    public Store? Store (PricesDataSet set) => set.Stores.Find (i => i.Id == StoreId);

    /// 同じ製品の価格数
    public override int ReferenceCount (PricesDataSet set) => set.Prices.Count (i => i.ProductId == ProductId);

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
