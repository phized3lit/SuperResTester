using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.DnnSuperres;
using SuperResTester.AIModels;
using SuperResTester.Globals;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;

namespace SuperResTester
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty] private ISuperResolutionModel? _superResolutionModel;

        [ObservableProperty] private BitmapImage? _originalImage;
        [ObservableProperty] private BitmapImage? _upscaledImage;
        [ObservableProperty] private string? _statusMessage;

        public MainWindowViewModel()
        {
        }

        [RelayCommand]
        private void Upscale()
        {
            if (SuperResolutionModel is null)
            {
                Debug.WriteLine("모델 경로가 설정되지 않았습니다.");
                return;
            }

            if (OriginalImage is null)
            {
                Debug.WriteLine("원본 이미지가 설정되지 않았습니다.");
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                Mat srcMat;
                if (OriginalImage.UriSource != null)
                {
                    // 파일 기반 이미지
                    var uri = new Uri(OriginalImage.UriSource.AbsoluteUri);
                    byte[] imageBytes = File.ReadAllBytes(uri.LocalPath);
                    srcMat = Cv2.ImDecode(imageBytes, ImreadModes.Color);
                }
                else
                {
                    // 클립보드, 스트림 기반 이미지
                    srcMat = ConverterFacade.BitmapSourceToMat(OriginalImage);
                    Cv2.CvtColor(srcMat, srcMat, ColorConversionCodes.BGRA2BGR); // BGRA → BGR 변환
                }

                using var resizedMat = ResizeToAligned(srcMat, 128, 128);
                var result = SuperResolutionModel.Upscale(resizedMat);
                UpscaledImage = ConverterFacade.MatToBitmapImage(result);
            }
            catch (Exception ex)
            {
                // 디버깅 시 메시지 출력 또는 로그
                Debug.WriteLine(ex);
            }

            StatusMessage = $"소요시간 {stopwatch.ElapsedMilliseconds}ms";
            stopwatch.Stop();
        }

        private Mat ResizeToAligned(Mat input, int width, int height)
        {
            if (input.Width == width && input.Height == height)
                return input;

            var resized = new Mat();
            Cv2.Resize(input, resized, new Size(width, height));
            return resized;
        }

        [RelayCommand]
        private void SelectModel()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Model Files (*.pb, *.onnx)|*.pb;*.onnx"
            };
            if (dlg.ShowDialog() is true && File.Exists(dlg.FileName))
            {
                var ext = Path.GetExtension(dlg.FileName).ToLowerInvariant();

                SuperResolutionModel = ext switch
                {
                    ".onnx" => new OnnxSuperResolutionModel(dlg.FileName),
                    ".pb" => new PbSuperResolutionModel(dlg.FileName),
                    _ => throw new NotSupportedException($"지원하지 않는 모델 형식입니다: {ext}")
                };
            }
        }

        [RelayCommand]
        private void LoadImage()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
            };

            if (dlg.ShowDialog() is true && File.Exists(dlg.FileName))
            {
                OriginalImage = new BitmapImage(new Uri(dlg.FileName));
                UpscaledImage = null;
            }
        }

        [RelayCommand]
        private void SaveImage()
        {
            if (UpscaledImage is null)
            {
                Debug.WriteLine("업스케일된 이미지가 없습니다.");
                return;
            }

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg|Bitmap Image|*.bmp",
                FileName = "upscaled_image",
                DefaultExt = ".png"
            };

            if (dlg.ShowDialog() is true)
            {
                try
                {
                    using var fileStream = new FileStream(dlg.FileName, FileMode.Create);
                    BitmapEncoder encoder = dlg.FilterIndex switch
                    {
                        2 => new JpegBitmapEncoder(),
                        3 => new BmpBitmapEncoder(),
                        _ => new PngBitmapEncoder(),
                    };

                    encoder.Frames.Add(BitmapFrame.Create(UpscaledImage));
                    encoder.Save(fileStream);

                    Debug.WriteLine($"저장 완료: {dlg.FileName}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"이미지 저장 실패: {ex.Message}");
                }
            }
        }
    }
}