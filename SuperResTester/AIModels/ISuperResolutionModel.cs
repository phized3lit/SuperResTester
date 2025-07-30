using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperResTester.AIModels
{
    public interface ISuperResolutionModel
    {
        string ModelPath { get; }

        Mat Upscale(Mat input);
    }
}
