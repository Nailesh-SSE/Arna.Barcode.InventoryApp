namespace InventoryManagement.Services.Download
{
    public interface IPrnDownloadService
    {
        Task DownloadAsync(Func<Task<byte[]>> prnGeneratorService, string fileName, string emptyMessage);
    }
}
