using InventoryManagement.Services.DTO;
using NReco.PdfGenerator;
using QRCoder;

namespace InventoryManagement.Services;

public class OutwardPdfService
{
    public byte[] GeneratePdf(OutwardPdfSummeryDto outward)
    {
        string qrContent = $"OutwardId={outward.Id}\r\nBrand:{outward.CompanyName}\r\nModel:{outward.TotalQuantity}";
        string qrBase64 = GenerateQrBase64(qrContent);

        var html = $@"
            <html>
        <head>
            <style>
                body {{ 
                    font-family: Arial, sans-serif; 
                    font-size: 14px;
                }}

                .label-container {{
                    display: flex;
                    width: 100%;
                }}

                .qr {{
                    width: 200px; 
                    height: 200px; 
                    margin-right: 20px;
                }}

                .details {{
                    line-height: 1.4;
                }}

                .details b {{
                    font-size: 16px;
                }}
            </style>
        </head>
        <body>

        <div class='label-container'>
            <div class='row align-items-center'>
                <!-- QR Code Image -->
                <div class='col-6 text-center'>
                    <img class='qr img-fluid' src='data:image/png;base64,{qrBase64}' alt='QR Code' />
                </div>

                <!-- Outward Details -->
                <div class='col-6 details'>
                    <div><strong>Batch No:</strong> {outward.Id}</div>
                    <div><strong>Brand:</strong> {outward.CompanyName}</div>
                    <div><strong>Quantity:</strong> {outward.TotalQuantity}</div>
                </div>
            </div>
        </div>


        </body>
        </html>";


        var converter = new HtmlToPdfConverter();
        return converter.GeneratePdf(html);
    }
    private string GenerateQrBase64(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new Base64QRCode(qrData);
        return qrCode.GetGraphic(20);
    }
}
