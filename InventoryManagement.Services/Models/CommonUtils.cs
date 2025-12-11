namespace InventoryManagement.Services.Models;

public static class CommonUtils
{
    public static List<UnitModel> UnitList = new()
    {
        new UnitModel { Id = 1, Name = "PCS" },
        new UnitModel { Id = 2, Name = "BOX" }
    };

    public static List<TakeToStockModel> TaskeToList = new()
    {
        new TakeToStockModel{ Id=1,Name="TakeToStock"},
        new TakeToStockModel{Id=2, Name="Other"}

    };
}
