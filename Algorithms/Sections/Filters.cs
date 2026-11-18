using Emgu.CV;
using Emgu.CV.Structure;
using System;

namespace Algorithms.Sections
{
    public class Filters
    {
        #region Apply filter
        public static Image<TColor, TDepth> ApplyFilter<TColor, TDepth>(Image<TColor, TDepth> inputImage, double[,] mask)
            where TColor : struct, IColor
            where TDepth : new()
        {
            Image<TColor, TDepth> resultImage = new Image<TColor, TDepth>(inputImage.Size);

            int h = mask.GetLength(0);
            int w = mask.GetLength(1);
            int h_offset = h / 2, w_offset = w / 2;
            var borderImage = new Image<TColor, TDepth>(inputImage.Width + 2 * w_offset, inputImage.Height + 2 * h_offset);

            CvInvoke.CopyMakeBorder(inputImage, borderImage, h_offset, h_offset, w_offset, w_offset, Emgu.CV.CvEnum.BorderType.Reflect101);

            for (int y = h_offset; y < borderImage.Height - h_offset; ++y)
                for (int x = w_offset; x < borderImage.Width - w_offset; ++x)
                    for (int channel = 0; channel < borderImage.NumberOfChannels; ++channel)
                    {
                        double sum = 0.0;

                        for (int i = -h_offset; i <= h_offset; i++)
                            for (int j = -w_offset; j <= w_offset; j++)
                            {
                                double pixel = Convert.ToDouble(borderImage.Data[y + i, x + j, channel]);
                                sum += mask[i + h_offset, j + w_offset] * pixel;
                            }

                        if (typeof(TDepth) == typeof(byte))
                        {
                            byte clampedSum = (byte)Math.Max(0, Math.Min(255, sum + 0.5));
                            resultImage.Data[y - h_offset, x - w_offset, channel] = (TDepth)Convert.ChangeType(clampedSum, typeof(TDepth));
                        }
                        else
                        {
                            resultImage.Data[y - h_offset, x - w_offset, channel] = (TDepth)Convert.ChangeType(sum, typeof(TDepth));
                        }
                    }

            return resultImage;
        }

        #endregion

        #region Separable filter
        public static Image<TColor, TDepth> SeparableFilter<TColor, TDepth>(Image<TColor, TDepth> inputImage, double[] v)
            where TColor : struct, IColor
            where TDepth : new()
        {
            if (inputImage == null)
                throw new ArgumentNullException(nameof(inputImage));

            if (v == null)
                throw new ArgumentNullException(nameof(v));

            if (v.Length == 0 || v.Length % 2 == 0)
                throw new ArgumentException("Length must be odd and not null.", nameof(v));

            int length = v.Length;

            double[,] horizontalMask = new double[1, length];
            double[,] verticalMask = new double[length, 1];

            for (int i = 0; i < length; i++)
            {
                horizontalMask[0, i] = v[i];
                verticalMask[i, 0] = v[i];
            }

            var horizontal = ApplyFilter(inputImage, horizontalMask);

            var result = ApplyFilter(horizontal, verticalMask);

            return result;
        }

        #endregion

        #region Gauss filter
        private static double[,] GaussMaskX(double sigma)
        {
            int L = (int)Math.Ceiling(4 * sigma);
            if (L % 2 == 0)
                L++;

            double[,] mask = new double[1, L];

            double coef = 1 / (Math.Sqrt(2 * Math.PI) * sigma);
            double sum = 0;

            for (int i = 0; i < L; i++)
            {
                int z = i - L / 2;
                mask[0, i] = Math.Exp(-z * z / (2 * sigma * sigma)) * coef;
                sum += mask[0, i];
            }

            for (int i = 0; i < L; i++)
            {
                mask[0, i] /= sum;
            }

            return mask;
        }

        private static double[,] GaussMaskY(double sigma)
        {
            int L = (int)Math.Ceiling(4 * sigma);
            if (L % 2 == 0)
                L++;

            double[,] mask = new double[L, 1];

            double coef = 1 / (Math.Sqrt(2 * Math.PI) * sigma);
            double sum = 0;

            for (int i = 0; i < L; i++)
            {
                int z = i - L / 2;
                mask[i, 0] = Math.Exp(-z * z / (2 * sigma * sigma)) * coef;
                sum += mask[i, 0];
            }

            for (int i = 0; i < L; i++)
            {
                mask[i, 0] /= sum;
            }

            return mask;
        }

        public static Image<Gray, byte> GaussFilter(Image<Gray, byte> inputImage, double sigmaX, double sigmaY)
        {
            double[,] maskX = GaussMaskX(sigmaX);
            double[,] maskY = GaussMaskY(sigmaY);

            Image<Gray, byte> auxImage = ApplyFilter(inputImage, maskX);
            Image<Gray, byte> resultImage = ApplyFilter(auxImage, maskY);

            return resultImage;
        }

        public static Image<Bgr, byte> GaussFilter(Image<Bgr, byte> inputImage, double sigmaX, double sigmaY)
        {
            double[,] maskX = GaussMaskX(sigmaX);
            double[,] maskY = GaussMaskY(sigmaY);

            Image<Bgr, byte> auxImage = ApplyFilter(inputImage, maskX);
            Image<Bgr, byte> resultImage = ApplyFilter(auxImage, maskY);

            return resultImage;
        }

        #endregion

        #region Median filter
        public static byte Median(int[] H, int l)
        {
            int n = (l * l) / 2;
            int sum = 0;

            for (int k = 0; k < 256; k++)
            {
                sum += H[k];

                if (sum > n)
                    return (byte)k;
            }

            throw new ArgumentException("The histogram doesn't have enough pixels", nameof(H));
        }

        public static Image<Gray, byte> MedianFiltering(Image<Gray, byte> inputImage, int l)
        {
            if (l % 2 == 0)
                l++;

            int r = l / 2;
            int[] H = new int[256];

            var B = new Image<Gray, byte>(inputImage.Width + 2 * r, inputImage.Height + 2 * r);

            CvInvoke.CopyMakeBorder(inputImage, B, r, r, r, r, Emgu.CV.CvEnum.BorderType.Reflect101);

            var result = new Image<Gray, byte>(inputImage.Size);

            for (int y = r; y < B.Height - r; y++)
            {
                Array.Clear(H, 0, H.Length);

                int x = r;

                for (int i = -r; i <= r; i++)
                    for (int j = -r; j <= r; j++)
                    {
                        byte value = B.Data[y + i, x + j, 0];
                        H[value]++;
                    }

                result.Data[y - r, x - r, 0] = Median(H, l);

                for (x = r + 1; x < B.Width - r; x++)
                {
                    for (int i = -r; i <= r; i++)
                    {
                        byte leaving = B.Data[y + i, x - r - 1, 0];

                        byte entering = B.Data[y + i, x + r, 0];

                        H[leaving]--;
                        H[entering]++;
                    }

                    result.Data[y - r, x - r, 0] = Median(H, l);
                }
            }

            return result;
        }

        #endregion


        #region Prewitt color gradient

        public static Image<Bgr, byte> PrewittColor(Image<Bgr, byte> inputImage, double threshold)
        {
            if (inputImage == null)
                throw new ArgumentNullException(nameof(inputImage));

            if (threshold < 0 || double.IsNaN(threshold) ||
                double.IsInfinity(threshold))
                throw new ArgumentOutOfRangeException(nameof(threshold));

            double[,] Px =
            {
                { -1,  0,  1 },
                { -1,  0,  1 },
                { -1,  0,  1 }
            };

            double[,] Py =
            {
                { -1, -1, -1 },
                {  0,  0,  1 },
                {  1,  1,  1 }
            };

            int offset = 1;

            Image<Bgr, byte> borderImage =
                new Image<Bgr, byte>(
                    inputImage.Width + 2,
                    inputImage.Height + 2);

            CvInvoke.CopyMakeBorder(
                inputImage,
                borderImage,
                offset,
                offset,
                offset,
                offset,
                Emgu.CV.CvEnum.BorderType.Reflect101);

            Image<Bgr, byte> result =
                new Image<Bgr, byte>(inputImage.Size);

            for (int y = offset; y < borderImage.Height - offset; y++)
            {
                for (int x = offset; x < borderImage.Width - offset; x++)
                {
                    double[] Ex = new double[3];
                    double[] Ey = new double[3];

                    for (int channel = 0; channel < 3; channel++)
                    {
                        double sumX = 0;
                        double sumY = 0;

                        for (int i = -1; i <= 1; i++)
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                double pixel = borderImage.Data[y + i, x + j, channel];

                                sumX += Px[i + 1, j + 1] * pixel;
                                sumY += Py[i + 1, j + 1] * pixel;
                            }
                        }

                        Ex[channel] = sumX;
                        Ey[channel] = sumY;
                    }

                    double EB = Math.Sqrt(
                        Ex[0] * Ex[0] + Ey[0] * Ey[0]);

                    double EG = Math.Sqrt(
                        Ex[1] * Ex[1] + Ey[1] * Ey[1]);

                    double ER = Math.Sqrt(
                        Ex[2] * Ex[2] + Ey[2] * Ey[2]);

                    
                    double sum = ER + EG + EB;

                    if (sum > threshold)
                    {
                        result.Data[y - offset, x - offset, 0] =
                            (byte)Math.Min(255, EB);

                        result.Data[y - offset, x - offset, 1] =
                            (byte)Math.Min(255, EG);

                        result.Data[y - offset, x - offset, 2] =
                            (byte)Math.Min(255, ER);
                    }
                    else
                    {
                        result.Data[y - offset, x - offset, 0] = 0;
                        result.Data[y - offset, x - offset, 1] = 0;
                        result.Data[y - offset, x - offset, 2] = 0;
                    }
                }
            }

            return result;
        }

        #endregion

    }

}
