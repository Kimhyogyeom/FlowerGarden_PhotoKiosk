using UnityEngine;
using ZXing;
using ZXing.QrCode;

public static class QrCodeGenerator_ZXing
{
    public static Texture2D Generate(string text, int size = 512, int margin = 1)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = size,
                Width = size,
                Margin = margin
            }
        };

        var pixelData = writer.Write(text);
        var tex = new Texture2D(pixelData.Width, pixelData.Height, TextureFormat.RGBA32, false);
        tex.LoadRawTextureData(pixelData.Pixels);
        tex.Apply(false);
        return tex;
    }
}
