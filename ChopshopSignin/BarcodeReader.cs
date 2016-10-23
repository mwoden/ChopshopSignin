using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ChopshopSignin
{
    class BarcodeReader
    {
        private readonly Capture camera;
        private readonly ZXing.BarcodeReader reader;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceNumber">Camera device number</param>
        /// <param name="width">Video capture device width in pixels</param>
        /// <param name="height">Video capture device height in pixels</param>
        /// <param name="bpp">Bits per pixel</param>
        public BarcodeReader(int deviceNumber, int width, int height, int bpp)
        {
            camera = new Capture(deviceNumber, width, height, (short)bpp);
            reader = new ZXing.BarcodeReader();

            reader.Options.PossibleFormats = new[] {
                ZXing.BarcodeFormat.QR_CODE,
                ZXing.BarcodeFormat.CODE_128,
                ZXing.BarcodeFormat.CODE_39, };
        }

        public string Scan()
        {
            // Capture image from the camera
            var rawData = camera.Click();

            using (var bitmap = new Bitmap(camera.Width, camera.Height, camera.Stride, System.Drawing.Imaging.PixelFormat.Format24bppRgb, rawData))
            {
                var decodeResult = reader.Decode(bitmap);

                // Release the buffer
                if (rawData != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(rawData);

                return decodeResult?.Text;
            }
        }
    }
}
