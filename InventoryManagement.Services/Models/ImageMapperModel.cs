namespace InventoryManagement.Services.Models;

public class ImageMapperModel : CommonModel
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    // Transient UI helper properties
    public string? PreviewBase64 { get; set; }
    public byte[]? CompressedBytes { get; set; }
    public bool IsMarkedForDeletion { get; set; }
    public bool IsNew { get; set; }
}
