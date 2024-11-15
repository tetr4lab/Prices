using Prices.Services;
using PetaPoco;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using MudBlazor;

namespace Prices.Data;

[TableName ("products")]
public class Product : PricesBaseModel<Product>, IPricesBaseModel {
    /// <inheritdoc/>
    public static string TableLabel => "製品";

    /// <inheritdoc/>
    public static Dictionary<string, string> Label { get; } = new () {
        { nameof (Id), "ID" },
        { nameof (Created), "生成日時" },
        { nameof (Modified), "更新日時" },
        { nameof (Name), "製品名" },
        { nameof (CategoryId), "カテゴリ" },
        { nameof (Unit), "単位" },
        { nameof (Remarks), "備考" },
        { nameof (Priority), "優先順" },
        { nameof (Image), "画像" },
    };

    /// <inheritdoc/>
    public static string BaseSelectSql => "select products.* from products order by priority desc, name;";

    /// <inheritdoc/>
    public static string UniqueKeysSql => "";

    [Column ("name"), StringLength (255), Required] public string Name { get; set; } = "";
    [Column ("category_id"), Required] public long CategoryId { get; set; } = 0;
    [Column ("unit"), StringLength (50)] public string? Unit { get; set; }
    [Column ("priority")] public int? Priority { get; set; } = null;
    [Column ("image")] public byte []? Image { get; set; } = null;

    /// <summary>カテゴリ get</summary>
    public Category? Category (PricesDataSet dataSet) => dataSet.GetList<Category> ().Find (i => i.Id == CategoryId);

    /// <summary>カテゴリ set</summary>
    public Category Category (Category category) {
        CategoryId = category.Id;
        return category;
    }

    /// <inheritdoc/>
    public override int ReferenceCount (PricesDataSet set) => set.Prices.Count (i => i.ProductId == Id);

    /// <inheritdoc/>
    public override string? [] SearchTargets => [
        $"y{Priority}.",
        $"p{Id}.",
        Name,
        $"c{CategoryId}.",
        Unit,
        Remarks
    ];

    /// <inheritdoc/>
    public override Product Clone () {
        var item = base.Clone ();
        item.Name = Name;
        item.CategoryId = CategoryId;
        item.Unit = Unit;
        item.Priority = Priority;
        item.Image = Image;
        return item;
    }

    /// <inheritdoc/>
    public override Product CopyTo (Product destination) {
        destination.Name = Name;
        destination.CategoryId = CategoryId;
        destination.Unit = Unit;
        destination.Priority = Priority;
        destination.Image = Image;
        return base.CopyTo (destination);
    }

    /// <inheritdoc/>
    public override bool Equals (Product? other) =>
        other != null
        && Id == other.Id
        && Name == other.Name
        && CategoryId == other.CategoryId
        && Unit == other.Unit
        && Remarks == other.Remarks
        && Priority == other.Priority
        && Image == other.Image
    ;

    /// <inheritdoc/>
    public override int GetHashCode () => HashCode.Combine (base.GetHashCode (), Name, CategoryId, Unit, Priority, Image);

    /// <inheritdoc/>
    public override string ToString () => $"{TableLabel} {Id}: {Name} {CategoryId} {Unit} \"{Remarks}\"";

}
