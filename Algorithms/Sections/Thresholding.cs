using Emgu.CV;
using Emgu.CV.Structure;
using System;

namespace Algorithms.Sections
{
    public class Thresholding
    {
        public static byte OtsuThreshold(Image<Gray, byte> inputImage)
        {
            if (inputImage == null)
                throw new ArgumentNullException(nameof(inputImage));

            if (inputImage.Width == 0 || inputImage.Height == 0)
                throw new ArgumentException("Imaginea este goala.");

            long[] histogram = new long[256];
            long totalPixels = (long)inputImage.Width * inputImage.Height;

            double totalIntensity = 0.0;

            for (int y = 0; y < inputImage.Height; y++)
            {
                for (int x = 0; x < inputImage.Width; x++)
                {
                    byte intensity = inputImage.Data[y, x, 0];

                    histogram[intensity]++;
                    totalIntensity += intensity;
                }
            }

            long backgroundPixels = 0;
            double backgroundIntensity = 0.0;


            double maxVariance = -1.0;
            long sumThresholds = 0;
            int countThresholds = 0;

            for (int t = 0; t < 256; t++)
            {
                backgroundPixels += histogram[t];
                backgroundIntensity += t * (double)histogram[t];

                long foregroundPixels = totalPixels - backgroundPixels;

                if (backgroundPixels == 0 || foregroundPixels == 0)
                    continue;

                double w1 = (double)backgroundPixels / totalPixels;
                double w2 = (double)foregroundPixels / totalPixels;

                double mean1 = backgroundIntensity / backgroundPixels;
                double mean2 =
                    (totalIntensity - backgroundIntensity) / foregroundPixels;

                double difference = mean1 - mean2;

                double variance = w1 * w2 * difference * difference;

                if (variance > maxVariance)
                {
                    maxVariance = variance;
                    sumThresholds = t;
                    countThresholds = 1;
                }
                else if (variance == maxVariance)
                {
                    sumThresholds += t;
                    countThresholds++;
                }
            }

            return countThresholds > 0
                ? (byte)Math.Round((double)sumThresholds / countThresholds)
                : (byte)0;
        }
    }
}