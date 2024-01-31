using Emgu.CV.Structure;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RubikCube.Tools
{
    public class HighPassFilters
    {

        public static Image<Gray, byte> ApplyCannyEdgeDetectorForColor(Image<Bgr, byte> colorImage, double t1, double t2)
        {
            var blurredImage = ApplyGaussianBlur(colorImage, 3, 1);

            var gradients = CalculateGradients(blurredImage);

            var suppressedGradients = NonMaximumSuppression(gradients);

            var edgeImage = HysteresisThresholding(suppressedGradients, t1, t2);

            return edgeImage;
        }

        #region Gaussian Blur
        public static double[,] GenerateGaussianKernel(int kernelSize, double sigma)
        {
            double[,] kernel = new double[kernelSize, kernelSize];
            double mean = kernelSize / 2;
            double sum = 0.0;

            for (int x = 0; x < kernelSize; ++x)
            {
                for (int y = 0; y < kernelSize; ++y)
                {
                    kernel[x, y] = Math.Exp(-0.5 * (Math.Pow((x - mean) / sigma, 2.0) + Math.Pow((y - mean) / sigma, 2.0)))
                                   / (2 * Math.PI * sigma * sigma);

                    sum += kernel[x, y];
                }
            }

            // Normalize the kernel
            for (int x = 0; x < kernelSize; ++x)
                for (int y = 0; y < kernelSize; ++y)
                    kernel[x, y] /= sum;

            return kernel;
        }
        public static Image<Bgr, byte> ApplyGaussianBlur(Image<Bgr, byte> image, int kernelSize, double sigma)
        {
            int width = image.Width;
            int height = image.Height;
            Image<Bgr, byte> blurredImage = new Image<Bgr, byte>(width, height);

            double[,] kernel = GenerateGaussianKernel(kernelSize, sigma);

            int kernelRadius = kernelSize / 2;

            for (int channel = 0; channel < 3; channel++)
            {
                for (int y = kernelRadius; y < height - kernelRadius; y++)
                {
                    for (int x = kernelRadius; x < width - kernelRadius; x++)
                    {
                        double sum = 0.0;

                        for (int ky = -kernelRadius; ky <= kernelRadius; ky++)
                        {
                            for (int kx = -kernelRadius; kx <= kernelRadius; kx++)
                            {
                                int pixelValue = (int)image.Data[y + ky, x + kx, channel];
                                sum += pixelValue * kernel[ky + kernelRadius, kx + kernelRadius];
                            }
                        }

                        blurredImage.Data[y, x, channel] = (byte)Math.Min(Math.Max(sum, 0), 255);
                    }
                }
            }

            return blurredImage;
        }
        #endregion

        #region Gradient

        private static double[,] sobelX = new double[,]
{
    { -1, 0, 1 },
    { -2, 0, 2 },
    { -1, 0, 1 }
};

        private static double[,] sobelY = new double[,]
        {
    { -1, -2, -1 },
    {  0,  0,  0 },
    {  1,  2,  1 }
        };
        
        public class Gradient
        {
            public double Magnitude { get; set; }
            public double Direction { get; set; }
        }

        public static Gradient[,] CalculateGradients(Image<Bgr, byte> image)
        {
            int width = image.Width;
            int height = image.Height;
            Gradient[,] gradients = new Gradient[height, width];

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    double gradientX = 0;
                    double gradientY = 0;

                    for (int channel = 0; channel < 3; channel++)
                    {
                        for (int ky = -1; ky <= 1; ky++)
                        {
                            for (int kx = -1; kx <= 1; kx++)
                            {
                                int pixelValue = (int)image.Data[y + ky, x + kx, channel];
                                gradientX += pixelValue * sobelX[ky + 1, kx + 1];
                                gradientY += pixelValue * sobelY[ky + 1, kx + 1];
                            }
                        }
                    }

                    double magnitude = Math.Sqrt(gradientX * gradientX + gradientY * gradientY);
                    double direction = Math.Atan2(gradientY, gradientX) * (180 / Math.PI);

                    gradients[y, x] = new Gradient { Magnitude = magnitude, Direction = direction };
                }
            }

            return gradients;
        }
        #endregion

        #region NonMaximumSuppretion
        private static double Interpolate(double grad1, double grad2, double weight)
        {
            return grad1 * (1 - weight) + grad2 * weight;
        }
        
        
        public static Image<Gray, byte> NonMaximumSuppression(Gradient[,] gradients)
        {
            int width = gradients.GetLength(1);
            int height = gradients.GetLength(0);
            Image<Gray, byte> suppressedImage = new Image<Gray, byte>(width, height);

            for (int y = 0; y <= height - 1; y++)
            {
                for (int x = 0; x <= width - 1; x++)
                {
                    if ((y >= 0 && y<=10)|| (y <= height - 1&&y>=height-10) || (x >= 0&&x<=10) || (x <= width - 1 && x>=width-10))
                    {
                        suppressedImage.Data[y, x, 0] = 0;
                    }
                    else
                    {
                        double gradientMagnitude = gradients[y, x].Magnitude;
                        double gradientDirection = gradients[y, x].Direction;

                        double grad1 = 0;
                        double grad2 = 0;
                        double weight = Math.Abs(Math.Tan(gradientDirection * Math.PI / 180));

                        if (gradientDirection >= -22.5 && gradientDirection < 22.5 ||
                            gradientDirection >= 157.5 || gradientDirection < -157.5)
                        {
                            // Horizontal edge
                            grad1 = (x > 1) ? gradients[y, x - 1].Magnitude : 0;
                            grad2 = (x < width - 2) ? gradients[y, x + 1].Magnitude : 0;
                        }
                        else if (gradientDirection >= 22.5 && gradientDirection < 67.5 ||
                                 gradientDirection >= -157.5 && gradientDirection < -112.5)
                        {
                            // +45 Degree edge
                            grad1 = (y > 1 && x < width - 2) ? gradients[y - 1, x + 1].Magnitude : 0;
                            grad2 = (y < height - 2 && x > 1) ? gradients[y + 1, x - 1].Magnitude : 0;
                        }
                        else if (gradientDirection >= 67.5 && gradientDirection < 112.5 ||
                                 gradientDirection >= -112.5 && gradientDirection < -67.5)
                        {
                            // Vertical edge
                            grad1 = (y > 1) ? gradients[y - 1, x].Magnitude : 0;
                            grad2 = (y < height - 2) ? gradients[y + 1, x].Magnitude : 0;
                        }
                        else if (gradientDirection >= 112.5 && gradientDirection < 157.5 ||
                                 gradientDirection >= -67.5 && gradientDirection < -22.5)
                        {
                            // -45 Degree edge
                            grad1 = (y > 1 && x > 1) ? gradients[y - 1, x - 1].Magnitude : 0;
                            grad2 = (y < height - 2 && x < width - 2) ? gradients[y + 1, x + 1].Magnitude : 0;
                        }

                        // Interpolate and compare
                        double gradInterpolated = Interpolate(grad1, grad2, weight);
                        if (gradientMagnitude < gradInterpolated)
                        {
                            suppressedImage.Data[y, x, 0] = 0;
                        }
                        else
                        {
                            suppressedImage.Data[y, x, 0] = (byte)Math.Min(Math.Max(gradientMagnitude, 0), 255);
                        }
                    }
                }
            }

            return suppressedImage;
        }

        #endregion

        #region Hysteresis Thresholding
        public static Image<Gray, byte> HysteresisThresholding(Image<Gray, byte> suppressedImage, double lowThreshold, double highThreshold)
        {
            int width = suppressedImage.Width;
            int height = suppressedImage.Height;
            Image<Gray, byte> edgeImage = new Image<Gray, byte>(width, height);

            byte strong = 255;
            byte weak = 25;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double gradientMagnitude = suppressedImage.Data[y, x, 0];

                    if (gradientMagnitude >= highThreshold)
                    {
                        edgeImage.Data[y, x, 0] = strong;
                    }
                    else if (gradientMagnitude >= lowThreshold)
                    {
                        edgeImage.Data[y, x, 0] = weak;
                    }
                    else
                    {
                        edgeImage.Data[y, x, 0] = 0;
                    }
                }
            }

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    if (edgeImage.Data[y, x, 0] == weak)
                    {
                        // Check if one of the neighbors is a strong edge
                        if (IsStrongEdgeNeighbor(edgeImage, x, y, strong))
                        {
                            edgeImage.Data[y, x, 0] = strong;
                        }
                        else
                        {
                            edgeImage.Data[y, x, 0] = 0;
                        }
                    }
                }
            }

            return edgeImage;
        }

        private static bool IsStrongEdgeNeighbor(Image<Gray, byte> image, int x, int y, byte strongValue)
        {
            for (int ky = -1; ky <= 1; ky++)
            {
                for (int kx = -1; kx <= 1; kx++)
                {
                    if (image.Data[y + ky, x + kx, 0] == strongValue)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion
    }

}
