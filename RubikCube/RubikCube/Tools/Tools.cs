using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace RubikCube.Tools
{
    public class Tools
    {
        #region Conversions
        public static Image<Bgr, byte> Copy(Image<Bgr, byte> inputImage)
        {
            Image<Bgr, byte> result = inputImage.Clone();
            return result;
        }

        public static Image<Gray, byte> ConvertToGrayscale(Image<Bgr, byte> image)
        {
            var grayImage = new Image<Gray, byte>(image.Width, image.Height);

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    List<byte> pixel = new List<byte>();
                    pixel.Add(image.Data[y, x, 0]);
                    pixel.Add(image.Data[y, x, 1]);
                    pixel.Add(image.Data[y, x, 2]);

                    byte grayValue = (byte)(0.299 * pixel[2] + 0.587 * pixel[1] + 0.114 * pixel[0]);

                    grayImage.Data[y, x, 0] = grayValue;
                }
            }

            return grayImage;
        }

        public static Image<Hsv, byte> ConvertToHSV(Image<Bgr, byte> inputImage)
        {
            Image<Hsv, byte> outputImage = new Image<Hsv, byte>(inputImage.Size);

            for (int y = 0; y < inputImage.Height; y++)
            {
                for (int x = 0; x < inputImage.Width; x++)
                {
                    Bgr inputPixel = inputImage[y, x];

                    double red = inputPixel.Red;
                    double green = inputPixel.Green;
                    double blue = inputPixel.Blue;

                    double redNormalized = red / 255.0;
                    double greenNormalized = green / 255.0;
                    double blueNormalized = blue / 255.0;

                    double cmax = Math.Max(redNormalized, Math.Max(greenNormalized, blueNormalized));
                    double cmin = Math.Min(redNormalized, Math.Min(greenNormalized, blueNormalized));
                    double diff = cmax - cmin;

                    double hue = -1;

                    if (cmax == cmin)
                        hue = 0;
                    else if (cmax == redNormalized)
                        hue = (60 * ((greenNormalized - blueNormalized) / diff) + 360) % 360;
                    else if (cmax == greenNormalized)
                        hue = (60 * ((blueNormalized - redNormalized) / diff) + 120) % 360;
                    else if (cmax == blueNormalized)
                        hue = (60 * ((redNormalized - greenNormalized) / diff) + 240) % 360;

                    double satuation;
                    if (cmax == 0)
                        satuation = 0;
                    else
                        satuation = (diff / cmax) * 100;

                    double value = cmax * 100;

                    byte scaledHue = (byte)(hue / 2);
                    byte scaledSatuation = (byte)(satuation * 2.55);
                    byte scaledValue = (byte)(value * 2.55);

                    outputImage[y, x] = new Hsv(scaledHue, scaledSatuation, scaledValue);
                }
            }

            outputImage.Save("output2_hsv_image.jpg");

            return outputImage;
        }

        #endregion

        #region AdaptiveThresholding
        public static Image<Gray, byte> AdaptiveThresholding(Image<Gray, byte> grayInitialImage, int dim)
        {
            double b = 0.85;
            int width = grayInitialImage.Width;
            int height = grayInitialImage.Height;

            Image<Gray, byte> resultImage = new Image<Gray, byte>(width, height);
            Image<Gray, float> integralImage = new Image<Gray, float>(width, height);


            double[,] integral = new double[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double sum = grayInitialImage.Data[y, x, 0];

                    if (x > 0)
                        sum += integral[y, x - 1];

                    if (y > 0)
                        sum += integral[y - 1, x];

                    if (x > 0 && y > 0)
                        sum -= integral[y - 1, x - 1];

                    integral[y, x] = sum;
                }
            }


            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int x0 = Math.Max(0, x - dim / 2);
                    int x1 = Math.Min(width - 1, x + dim / 2);
                    int y0 = Math.Max(0, y - dim / 2);
                    int y1 = Math.Min(height - 1, y + dim / 2);


                    double sum = integral[y1, x1] - integral[y0, x1] - integral[y1, x0] + integral[y0, x0];
                    double mean = sum / ((x1 - x0 + 1) * (y1 - y0 + 1));


                    byte threshold = (byte)(b * mean);


                    if (grayInitialImage.Data[y, x, 0] > threshold)
                    {
                        resultImage.Data[y, x, 0] = 255;
                    }
                    else
                    {
                        resultImage.Data[y, x, 0] = 0;
                    }
                }
            }

            return resultImage;
        }

        public static Image<Gray, byte> ApplyCustomAdaptiveThreshold(Image<Gray, byte> grayImage, int windowSize, double C)
        {
            var thresholdedImage = new Image<Gray, byte>(grayImage.Width, grayImage.Height);

            int border = windowSize / 2;
            double threshold;

            for (int y = border; y < grayImage.Height - border; y++)
            {
                for (int x = border; x < grayImage.Width - border; x++)
                {
                    double sum = 0;
                    for (int dy = -border; dy <= border; dy++)
                    {
                        for (int dx = -border; dx <= border; dx++)
                        {
                            sum += grayImage.Data[y + dy, x + dx, 0];
                        }
                    }
                    threshold = sum / (windowSize * windowSize);
                    threshold -= C;
                    thresholdedImage.Data[y, x, 0] = (grayImage.Data[y, x, 0] > threshold) ? (byte)255 : (byte)0;
                }
            }

            return thresholdedImage;
        }
        #endregion

        #region Sobel
        public static Image<Gray, byte> Sobel(Image<Gray, byte> grayInitialImage, double T)
        {
            Image<Gray, byte> result = new Image<Gray, byte>(grayInitialImage.Size);

            for (int y = 1; y < grayInitialImage.Height - 1; y++)
            {
                for (int x = 1; x < grayInitialImage.Width - 1; x++)
                {
                    double Sx = grayInitialImage.Data[y - 1, x + 1, 0] - grayInitialImage.Data[y - 1, x - 1, 0] + 2 * grayInitialImage.Data[y, x + 1, 0] - 2 * grayInitialImage.Data[y, x - 1, 0] + grayInitialImage.Data[y + 1, x + 1, 0] - grayInitialImage.Data[y + 1, x - 1, 0];
                    double Sy = grayInitialImage.Data[y + 1, x - 1, 0] - grayInitialImage.Data[y - 1, x - 1, 0] + 2 * grayInitialImage.Data[y + 1, x, 0] - 2 * grayInitialImage.Data[y - 1, x, 0] + grayInitialImage.Data[y + 1, x + 1, 0] - grayInitialImage.Data[y - 1, x + 1, 0];
                    double angle = Math.Atan2(Sy, Sx);
                    double angleDegrees = angle * (180.0 / Math.PI);
                    double grad = Math.Sqrt(Sx * Sx + Sy * Sy);

                    if (IsApproximatelyDiagonal(angleDegrees) && grad >= T)
                    {
                        result.Data[y, x, 0] = 255;
                    }

                }
            }

            return result;
        }

        static bool IsApproximatelyDiagonal(double angle)
        {
            double lowerBound1 = -67.5;
            double upperBound1 = -22.5;
            double lowerBound2 = 157.5;
            double upperBound2 = 112.5;

            if ((angle >= lowerBound1 && angle <= upperBound1) || (angle <= lowerBound2 && angle >= upperBound2))
            {
                return true;
            }

            return false;

        }
        #endregion

        #region Image Format Handling
        public static Bitmap BitmapImageToBitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                return new Bitmap(outStream);
            }
        }

        public static Image<Bgr, byte> ConvertToEmguImage(System.Windows.Controls.Image wpfImage)
        {
            if (wpfImage.Source is BitmapSource bitmapSource)
            {
                Bitmap bitmap = BitmapFromBitmapSource(bitmapSource);
                return bitmap.ToImage<Bgr, byte>();
            }
            else
            {
                throw new InvalidOperationException("Unsupported image source type.");
            }
        }

        public static Bitmap BitmapFromBitmapSource(BitmapSource bitmapSource)
        {
            Bitmap bitmap;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapSource));
                enc.Save(outStream);
                bitmap = new Bitmap(outStream);
            }
            return bitmap;
        }
        #endregion

    }
}

