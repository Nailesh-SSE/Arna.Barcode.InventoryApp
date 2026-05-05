using BlazorDownloadFile;
using Microsoft.JSInterop;

namespace InventoryManagement.Services.Download
{
    public class PrnDownloadService : IPrnDownloadService
    {
        private readonly IBlazorDownloadFileService _downloadService;
        private readonly IJSRuntime _js;

        public PrnDownloadService(IBlazorDownloadFileService downloadService, IJSRuntime js)
        {
            _downloadService = downloadService;
            _js = js;
        }
        public async Task DownloadAsync(Func<Task<byte[]>> prnGenerator, string fileName, string emptyMessage)
        {
            try
            {
                var prnBytes = await prnGenerator();

                if (prnBytes.Length > 0)
                {
                    await _downloadService.DownloadFile(
                        fileName,
                        prnBytes,
                        "application/octet-stream");
                }
                else
                {
                    await _js.InvokeVoidAsync("alert", emptyMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading PRN file: {ex.Message}");
                await _js.InvokeVoidAsync("alert", "Error downloading PRN file.");
            }
        }
    }
}
