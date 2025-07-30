using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System;
using System.IO;
using System.Linq;

namespace SuperResTester.AIModels
{
    public class OnnxSuperResolutionModel : ISuperResolutionModel
    {
        public string ModelPath { get; }

        private InferenceSession _session;

        public OnnxSuperResolutionModel(string modelPath, bool useGPU = true)
        {
            if (!File.Exists(modelPath))
                throw new FileNotFoundException("모델 파일을 찾을 수 없습니다.", modelPath);

            ModelPath = modelPath;

            if (useGPU)
            {
                var options = new SessionOptions();
                options.AppendExecutionProvider_CUDA();  // GPU 사용 명시
                _session = new InferenceSession(modelPath, options);
            }
            else
            {
                _session = new InferenceSession(modelPath);
            }
        }

        public Mat Upscale(Mat input)
        {
            // BGR → RGB, 정규화
            var rgb = input.CvtColor(ColorConversionCodes.BGR2RGB);
            rgb.ConvertTo(rgb, MatType.CV_32FC3, 1.0 / 255.0);

            int h = rgb.Rows;
            int w = rgb.Cols;

            // 입력 shape 확인
            var inputName = _session.InputMetadata.Keys.First();
            var inputMeta = _session.InputMetadata[inputName];
            var inputShape = inputMeta.Dimensions;

            bool inputHasBatch = inputShape.Length == 4;
            int channels = inputHasBatch ? inputShape[1] : inputShape[0];

            float[] chw = new float[channels * h * w];
            var indexer = rgb.GetGenericIndexer<Vec3f>();
            Parallel.For(0, h, y =>
            {
                for (int x = 0; x < w; x++)
                {
                    var px = indexer[y, x];
                    int baseIndex = y * w + x;
                    chw[baseIndex] = px.Item0;             // R
                    chw[baseIndex + h * w] = px.Item1;      // G
                    chw[baseIndex + 2 * h * w] = px.Item2;  // B
                }
            });

            var inputTensorShape = inputHasBatch ? new[] { 1, channels, h, w } : new[] { channels, h, w };
            var inputTensor = new DenseTensor<float>(chw, inputTensorShape);
            var inputs = new[] { NamedOnnxValue.CreateFromTensor(inputName, inputTensor) };

            // 추론
            var outputName = _session.OutputMetadata.Keys.First();
            using var results = _session.Run(inputs);
            var outputTensor = results.First(v => v.Name == outputName).AsTensor<float>();
            var outputShape = outputTensor.Dimensions;

            bool outputHasBatch = outputShape.Length == 4;
            int outC = outputHasBatch ? outputShape[1] : outputShape[0];
            int outH = outputHasBatch ? outputShape[2] : outputShape[1];
            int outW = outputHasBatch ? outputShape[3] : outputShape[2];

            var outputMat = new Mat(outH, outW, MatType.CV_32FC3);
            var outIndexer = outputMat.GetGenericIndexer<Vec3f>();
            Parallel.For(0, outH, y =>
            {
                for (int x = 0; x < outW; x++)
                {
                    float r = outputHasBatch ? outputTensor[0, 0, y, x] : outputTensor[0, y, x];
                    float g = outputHasBatch ? outputTensor[0, 1, y, x] : outputTensor[1, y, x];
                    float b = outputHasBatch ? outputTensor[0, 2, y, x] : outputTensor[2, y, x];
                    outIndexer[y, x] = new Vec3f(r, g, b);
                }
            });

            outputMat.ConvertTo(outputMat, MatType.CV_8UC3, 255.0);
            Cv2.CvtColor(outputMat, outputMat, ColorConversionCodes.RGB2BGR);
            return outputMat;
        }
    }
}
