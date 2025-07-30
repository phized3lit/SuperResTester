using OpenCvSharp;
using OpenCvSharp.DnnSuperres;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SuperResTester.AIModels
{
    public class PbSuperResolutionModel : ISuperResolutionModel
    {
        public string ModelPath { get; }
        public string ModelName { get; }
        public int Scale { get; }

        public PbSuperResolutionModel(string modelPath)
        {
            ModelPath = modelPath;
            var name = Path.GetFileNameWithoutExtension(modelPath);
            var match = Regex.Match(name, @"^(?<model>\w+)_x(?<scale>\d+)$");
            if (!match.Success) throw new FormatException("모델 이름 형식 오류");

            ModelName = match.Groups["model"].Value.ToLowerInvariant();
            Scale = int.Parse(match.Groups["scale"].Value);
        }
        public Mat Upscale(Mat input)
        {
            var sr = new DnnSuperResImpl();
            sr.ReadModel(ModelPath);
            sr.SetModel(ModelName, Scale);

            var output = new Mat();
            sr.Upsample(input, output);
            return output;
        }
    }
}
