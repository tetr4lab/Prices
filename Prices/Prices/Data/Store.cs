using Prices.Services;
using PetaPoco;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using static System.Reflection.Metadata.BlobBuilder;
using System.Data;

namespace Prices.Data;

[TableName ("stores")]
public class Store : BaseModel<Store>, IBaseModel {
    /// <inheritdoc/>
    public static string TableLabel => "店舗";

    /// <inheritdoc/>
    public static Dictionary<string, string> Label { get; } = new Dictionary<string, string> {
        { nameof (Id), "ID" },
        { nameof (Name), "店名" },
        { nameof (Remarks), "備考" },
    };

    /// <inheritdoc/>
    public static string BaseSelectSql {
        get {
            var table = PricesDataSet.GetSqlName<Store> ();
            return $"select {table}.* from {table} order by id;";
        }
    }

    /// <inheritdoc/>
    public static string UniqueKeysSql => "";

    [Column ("name"), StringLength (255), Required] public string Name { get; set; } = "";

    /// <inheritdoc/>
    public override int ReferenceCount (PricesDataSet set) => set.Prices.Count (i => i.StoreId == Id);

    /// <inheritdoc/>
    public override string? [] SearchTargets => [
        $"s{Id}.",
        Name,
        Remarks
    ];

    /// <inheritdoc/>
    public override Store Clone () {
        var item = base.Clone ();
        item.Name = Name;
        return item;
    }

    /// <inheritdoc/>
    public override Store CopyTo (Store destination) {
        destination.Name = Name;
        return base.CopyTo (destination);
    }

    /// <inheritdoc/>
    public override bool Equals (Store? other) =>
        other != null
        && Id == other.Id
        && Name == other.Name
        && Remarks == other.Remarks
    ;

    /// <inheritdoc/>
    public override int GetHashCode () => HashCode.Combine (Id, Name, Remarks);

    /// <inheritdoc/>
    public override string ToString () => $"{TableLabel} {Id}: {Name} \"{Remarks}\"";

}

