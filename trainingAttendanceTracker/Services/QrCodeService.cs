using QRCoder;
using System.Drawing.Imaging;
using System.Drawing;


namespace trainingAttendanceTracker.Services
{
    public class QrCodeService
    {
        public string GenerateQrCode(string text)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrData);

                var qrBytes = qrCode.GetGraphic(20);
                var base64 = Convert.ToBase64String(qrBytes);
                return $"data:image/png;base64,{base64}";
            }
        }
    }
}
