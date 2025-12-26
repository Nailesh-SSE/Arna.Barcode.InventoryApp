namespace InventoryManagement.Services.Models;

public static class CommonUtils
{
    public static List<UnitModel> UnitList = new()
    {
        new UnitModel { Id = 1, Name = "PCS" },
        new UnitModel { Id = 2, Name = "BOX" }
    };
}
