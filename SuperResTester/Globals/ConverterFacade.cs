using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices; // 추가

namespace SuperResTester.Globals
{
    public abstract class ConverterFacade
    {
        internal static BitmapImage MatToBitmapImage(Mat mat)
        {
            using var ms = mat.ToMemoryStream();  // OpenCvSharp.Extensions 사용
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = ms;
            image.EndInit();
            image.Freeze(); // WPF에서 UI 스레드 접근 허용
            return image;
        }
        public static Mat BitmapSourceToMat(BitmapSource bitmapSource)
        {
            // PixelFormat 변환 (Bgra32로 통일)
            var converted = new FormatConvertedBitmap(bitmapSource, System.Windows.Media.PixelFormats.Bgra32, null, 0);

            int width = converted.PixelWidth;
            int height = converted.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            converted.CopyPixels(pixels, stride, 0);

            // 빈 Mat 생성 후 픽셀 데이터 복사
            var mat = new Mat(height, width, MatType.CV_8UC4);
            Marshal.Copy(pixels, 0, mat.Data, pixels.Length);
            return mat;
        }
        public static BitmapImage BitmapSourceToBitmapImage(BitmapSource source)
        {
            if (source is BitmapImage bitmapImage)
                return bitmapImage;

            using (var ms = new System.IO.MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(source));
                encoder.Save(ms);
                ms.Position = 0;

                var img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.StreamSource = ms;
                img.EndInit();
                img.Freeze();
                return img;
            }
        }
    }
}
