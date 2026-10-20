using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System;

namespace Algorithms.Sections
{
    public class PointwiseOperations
    {
        #region Contrast and brightness
        public static byte[] ContrastAndBrightness(double a, double b)
        {
            byte[] LUT = new byte[256];
            for (int r = 0; r < 256; r++)
            {
                double s = a * r + b;
                s = Math.Min(255, Math.Max(0, s));
                s = Math.Round(s);
                LUT[r] = (byte)s;
            }
            return LUT;
        }

        public static Image<Gray, byte> ApplyLUT(Image<Gray, byte> inputImage, byte[] LUT)
        {
            Image<Gray, byte> result = new Image<Gray, byte>(inputImage.Size);

            for (int y = 0; y < inputImage.Height; ++y)
            {
                for (int x = 0; x < inputImage.Width; ++x)
                {
                    result.Data[y, x, 0] = LUT[inputImage.Data[y, x, 0]];
                }
            }
            return result;
        }

        public static Image<Bgr, byte> ApplyLUT(Image<Bgr, byte> inputImage, byte[] LUT)
        {
            Image<Bgr, byte> result =
                new Image<Bgr, byte>(inputImage.Size);

            for (int y = 0; y < inputImage.Height; ++y)
            {
                for (int x = 0; x < inputImage.Width; ++x)
                {
                    for (int c = 0; c < 3; ++c)
                    {
                        result.Data[y, x, c] =
                            LUT[inputImage.Data[y, x, c]];
                    }
                }
            }

            return result;
        }
        #endregion

        #region Gamma operator

        public static byte[] Gamma(double gamma)
        {
            if (gamma <= 0 || double.IsNaN(gamma) || double.IsInfinity(gamma))
                throw new ArgumentException("Gamma must be positive.");

            byte[] LUT = new byte[256];

            double c = Math.Pow(255, 1 - gamma);

            for (int r = 0; r < 256; ++r)
            {
                double s = c * Math.Pow(r, gamma);

                s = Math.Min(255, Math.Max(0, s));
                s = Math.Round(s);

                LUT[r] = (byte)s;
            }

            return LUT;
        }

        #endregion

        #region Normalized Histogram

        public static double[] NormalizedHistogram(Image<Gray, byte> inputImage)
        {
            double[] histogram = new double[256];

            for (int y = 0; y < inputImage.Height; ++y)
                for (int x = 0; x < inputImage.Width; ++x)
                {
                    byte intensity = inputImage.Data[y, x, 0];
                    histogram[intensity]++;
                }

            int totalPixels = inputImage.Width * inputImage.Height;

            for (int i = 0; i < 256; ++i)
                histogram[i] /= totalPixels;

            return histogram;
        }

        public static double[,] NormalizedHistogram(Image<Bgr, byte> inputImage)
        {
            double[,] histogram = new double[3, 256];

            for (int y = 0; y < inputImage.Height; ++y)
                for (int x = 0; x < inputImage.Width; ++x)
                    for (int c = 0; c < 3; c++)
                    {
                        byte intensity = inputImage.Data[y, x, c];
                        histogram[c, intensity]++;
                    }

            int totalPixels = inputImage.Width * inputImage.Height;

            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < 256; i++)
                {
                    histogram[c, i] /= totalPixels;
                }
            }

            return histogram;
        }

        #endregion
    }
}