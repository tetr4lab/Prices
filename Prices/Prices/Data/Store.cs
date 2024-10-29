using Prices.Services;
using PetaPoco;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using static System.Reflection.Metadata.BlobBuilder;
using System.Data;

namespace Prices.Data;

[TableName ("stores")]
public class Store : PricesBaseModel<Store>, IPricesBaseModel {
    /// <inheritdoc/>
    public static string TableLabel => "店舗";

    /// <inheritdoc/>
    public static Dictionary<string, string> Label { get; } = new () {
        { nameof (Id), "ID" },
        { nameof (Created), "生成日時" },
        { nameof (Modified), "更新日時" },
        { nameof (Name), "店名" },
        { nameof (Remarks), "備考" },
        { nameof (Priority), "優先順" },
    };

    /// <inheritdoc/>
    public static string BaseSelectSql => "select stores.* from stores order by priority desc, name;";

    /// <inheritdoc/>
    public static string UniqueKeysSql => "";

    [Column ("name"), StringLength (255), Required] public string Name { get; set; } = "";
    [Column ("priority")] public int? Priority { get; set; } = null;

    /// <inheritdoc/>
    public override int ReferenceCount (PricesDataSet set) => set.Prices.Count (i => i.StoreId == Id);

    /// <inheritdoc/>
    public override string? [] SearchTargets => [
        $"y{Priority}.",
        $"s{Id}.",
        Name,
        Remarks
    ];

    /// <inheritdoc/>
    public override Store Clone () {
        var item = base.Clone ();
        item.Name = Name;
        item.Priority = Priority;
        return item;
    }

    /// <inheritdoc/>
    public override Store CopyTo (Store destination) {
        destination.Name = Name;
        destination.Priority = Priority;
        return base.CopyTo (destination);
    }

    /// <inheritdoc/>
    public override bool Equals (Store? other) =>
        other != null
        && Id == other.Id
        && Name == other.Name
        && Remarks == other.Remarks
        && Priority == other.Priority
    ;

    /// <inheritdoc/>
    public override int GetHashCode () => HashCode.Combine (base.GetHashCode (), Name, Priority);

    /// <inheritdoc/>
    public override string ToString () => $"{TableLabel} {Id}: {Name} \"{Remarks}\"";

}

