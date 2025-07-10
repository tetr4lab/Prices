using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;
using MySqlConnector;
using PetaPoco;
using Prices.Data;
using Tetr4lab;

namespace Prices.Services;

/// <summary></summary>
public sealed class PricesDataSet : MySqlDataSet {

    /// <summary>コンストラクタ</summary>
    public PricesDataSet (Database database) : base (database) { }

    /// <summary>ロード済みのモデルインスタンス</summary>
    public List<Category> Categories => GetList <Category> ();

    /// <summary>ロード済みのモデルインスタンス</summary>
    public List<Product> Products => GetList<Product> ();

    /// <summary>ロード済みのモデルインスタンス</summary>
    public List<Store> Stores => GetList<Store> ();

    /// <summary>ロード済みのモデルインスタンス</summary>
    public List<Price> Prices => GetList<Price> ();

    /// <summary>有効性の検証</summary>
    public bool Valid
        => IsReady
        && ListSet.ContainsKey (typeof (Category)) && ListSet [typeof (Category)] is List<Category>
        && ListSet.ContainsKey (typeof (Product)) && ListSet [typeof (Product)] is List<Product>
        && ListSet.ContainsKey (typeof (Store)) && ListSet [typeof (Store)] is List<Store>
        && ListSet.ContainsKey (typeof (Price)) && ListSet [typeof (Price)] is List<Price>
        ;

    /// <summary>一覧セットをアトミックに取得</summary>
    public override async Task<Result<bool>> GetListSetAsync () {
        var result = await ProcessAndCommitAsync<bool> (async () => {
            var categories = await database.FetchAsync<Category> (Category.BaseSelectSql);
            var products = await database.FetchAsync<Product> (Product.BaseSelectSql);
            var stores = await database.FetchAsync<Store> (Store.BaseSelectSql);
            var prices = await database.FetchAsync<Price> (Price.BaseSelectSql);
            if (categories is not null && products is not null && stores is not null && prices is not null) {
                ListSet [typeof (Category)] = categories;
                ListSet [typeof (Product)] = products;
                ListSet [typeof (Store)] = stores;
                ListSet [typeof (Price)] = prices;
                return true;
            }
            ListSet.Remove (typeof (Category));
            ListSet.Remove (typeof (Product));
            ListSet.Remove (typeof (Store));
            ListSet.Remove (typeof (Price));
            return false;
        });
        if (result.IsSuccess && !result.Value) {
            result.Status = Status.Unknown;
        }
        return result;
    }

}
