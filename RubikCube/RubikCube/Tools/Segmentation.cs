using Emgu.CV.Structure;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace RubikCube.Tools
{
    internal class Segmentation
    {
        #region Harris Operator
        public static List<Point> DetectHarrisCorners(Image<Gray, byte> image, double k, double threshold)
        {
            // Calculate gradients Ix and Iy
            var gradients = CalculateGradients(image);
            var Ix = gradients.Item1;
            var Iy = gradients.Item2;

            // Produs derivate
            var gradientProducts = CalculateGradientProducts(Ix, Iy);
            var Ix2 = gradientProducts.Item1;
            var Iy2 = gradientProducts.Item2;
            var IxIy = gradientProducts.Item3;

            int kernelSize = 5;
            double sigma = 1.5;
            var Ix2Smoothed = ApplyGaussianSmoothing(Ix2, kernelSize, sigma);
            var Iy2Smoothed = ApplyGaussianSmoothing(Iy2, kernelSize, sigma);
            var IxIySmoothed = ApplyGaussianSmoothing(IxIy, kernelSize, sigma);

            var harrisResponse = CalculateHarrisResponse(Ix2Smoothed, Iy2Smoothed, IxIySmoothed, k);

            var corners = ApplyThresholdingAndNonMaximumSuppression(harrisResponse, threshold, 20);

            return corners;
        }

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

        public static (Image<Gray, float>, Image<Gray, float>) CalculateGradients(Image<Gray, byte> image)
        {
            var Ix = new Image<Gray, float>(image.Width, image.Height);
            var Iy = new Image<Gray, float>(image.Width, image.Height);

            // Sobel kernels
            for (int y = 1; y < image.Height - 1; y++)
            {
                for (int x = 1; x < image.Width - 1; x++)
                {
                    float gradientX = 0;
                    float gradientY = 0;

                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            byte pixelValue = image.Data[y + ky, x + kx, 0];
                            gradientX += pixelValue * (float)sobelX[ky + 1, kx + 1];
                            gradientY += pixelValue * (float)sobelY[ky + 1, kx + 1];
                        }
                    }

                    Ix.Data[y, x, 0] = gradientX;
                    Iy.Data[y, x, 0] = gradientY;
                }
            }

            return (Ix, Iy);
        }

        public static (Image<Gray, float>, Image<Gray, float>, Image<Gray, float>) CalculateGradientProducts(Image<Gray, float> Ix, Image<Gray, float> Iy)
        {
            var Ix2 = new Image<Gray, float>(Ix.Width, Ix.Height);
            var Iy2 = new Image<Gray, float>(Iy.Width, Iy.Height);
            var IxIy = new Image<Gray, float>(Ix.Width, Ix.Height);

            for (int y = 0; y < Ix.Height; y++)
            {
                for (int x = 0; x < Ix.Width; x++)
                {
                    float ix = Ix.Data[y, x, 0];
                    float iy = Iy.Data[y, x, 0];

                    Ix2.Data[y, x, 0] = ix * ix;
                    Iy2.Data[y, x, 0] = iy * iy;
                    IxIy.Data[y, x, 0] = ix * iy;
                }
            }

            return (Ix2, Iy2, IxIy);
        }
        public static Image<Gray, float> ApplyGaussianSmoothing(Image<Gray, float> image, int kernelSize, double sigma)
        {
            var smoothed = new Image<Gray, float>(image.Width, image.Height);
            double[,] kernel = HighPassFilters.GenerateGaussianKernel(kernelSize, sigma);
            int radius = kernelSize / 2;

            for (int y = radius; y < image.Height - radius; y++)
            {
                for (int x = radius; x < image.Width - radius; x++)
                {
                    double sum = 0.0;

                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            sum += kernel[ky + radius, kx + radius] * image.Data[y + ky, x + kx, 0];
                        }
                    }

                    smoothed.Data[y, x, 0] = (float)sum;
                }
            }

            return smoothed;
        }
        public static Image<Gray, float> CalculateHarrisResponse(Image<Gray, float> Ix2, Image<Gray, float> Iy2, Image<Gray, float> IxIy, double k)
        {
            int width = Ix2.Width;
            int height = Ix2.Height;
            Image<Gray, float> harrisResponse = new Image<Gray, float>(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float ix2 = Ix2.Data[y, x, 0];
                    float iy2 = Iy2.Data[y, x, 0];
                    float ixIy = IxIy.Data[y, x, 0];

                    //determinant and trace (urma)
                    float detM = ix2 * iy2 - ixIy * ixIy;
                    float traceM = ix2 + iy2;

                    harrisResponse.Data[y, x, 0] = (float)(detM - k * traceM * traceM);
                }
            }

            return harrisResponse;
        }
        public static List<Point> ApplyThresholdingAndNonMaximumSuppression(Image<Gray, float> harrisResponse, double threshold, int minDistance)
        {
            List<Point> corners = new List<Point>();
            int width = harrisResponse.Width;
            int height = harrisResponse.Height;

            for (int y = 10; y < height - 10; y++)
            {
                for (int x = 10; x < width - 10; x++)
                {
                    float response = harrisResponse.Data[y, x, 0];

                    if (response > threshold && IsLocalMaximum(harrisResponse, x, y))
                    {
                        if (IsStrongestResponseWithinDistance(harrisResponse, x, y, minDistance))
                        {
                            corners.Add(new Point(x, y));
                        }
                    }
                }
            }

            return corners;
        }

        private static bool IsStrongestResponseWithinDistance(Image<Gray, float> image, int x, int y, int minDistance)
        {
            float currentValue = image.Data[y, x, 0];
            for (int dy = -minDistance; dy <= minDistance; dy++)
            {
                for (int dx = -minDistance; dx <= minDistance; dx++)
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < image.Width && ny >= 0 && ny < image.Height)
                    {
                        if (image.Data[ny, nx, 0] > currentValue)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private static bool IsLocalMaximum(Image<Gray, float> image, int x, int y)
        {
            float currentValue = image.Data[y, x, 0];
            for (int ky = -1; ky <= 1; ky++)
            {
                for (int kx = -1; kx <= 1; kx++)
                {
                    if (image.Data[y + ky, x + kx, 0] > currentValue)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        #endregion

        #region Cube Face Cropping and Perspective Correction
        public static Image<Bgr, byte> ExtractAndCorrectCubeFace(Image<Bgr, byte> colorImage, List<Point> detectedCorners)
        {
            var cubeCorners = FilterCubeFaceCorners(detectedCorners);

            var orderedCorners = OrderCorners(cubeCorners);

            int cubeSize = 200; 
            var srcPoints = orderedCorners.Select(p => new PointF(p.X, p.Y)).ToArray();
            List<Point> destPoints = new List<Point>() 
            {
                new Point(1, 1),
                new Point(colorImage.Width, 1),
                new Point(colorImage.Width, colorImage.Height),
                new Point(1, colorImage.Height)
            };

            List<Point> startPoints = new List<Point>() 
            {
                new Point(orderedCorners[0].X, orderedCorners[0].Y),
                new Point(orderedCorners[1].X, orderedCorners[1].Y),
                new Point(orderedCorners[2].X, orderedCorners[2].Y),
                new Point(orderedCorners[3].X, orderedCorners[3].Y)
            };



            Image<Bgr, byte> transformedCubeFace = Preprocessing.ProjectionTransformation(colorImage, startPoints, destPoints);
            transformedCubeFace = transformedCubeFace.Resize(200, 200, Emgu.CV.CvEnum.Inter.Linear);

            return transformedCubeFace;
        }

        private static List<Point> FilterCubeFaceCorners(List<Point> corners)
        {
            double maxArea = 0;
                                                      
            if (corners.Count < 4)
                return corners;


            List<Point> bestQuadrilateral = null;
            double bestScore = double.MaxValue; 

            foreach (var quadrilateral in GetCombinations(corners, 4))
            {
                var quadrilateral_aux = OrderCorners(quadrilateral);
                double area = CalculateQuadrilateralArea(quadrilateral_aux);
                double aspectRatio = CalculateAspectRatio(quadrilateral_aux);
                
                double angleConsistency = CalculateAngleConsistency(quadrilateral_aux);

                double score = Math.Abs(1-aspectRatio) + angleConsistency/100; 
                
                if (area > maxArea)
                    if (score < bestScore)
                    {

                        bestScore = score;
                        bestQuadrilateral = quadrilateral;
                        maxArea = area;
                    }
            }

            return bestQuadrilateral;
        }

        private static IEnumerable<List<Point>> GetCombinations(List<Point> corners, int combinationSize)
        {
            int count = corners.Count;
            if (combinationSize > count || combinationSize < 1)
                yield break;

            int[] indices = new int[combinationSize];
            for (int i = 0; i < combinationSize; i++)
                indices[i] = i;

            while (indices[0] < count - combinationSize)
            {
                yield return indices.Select(index => corners[index]).ToList();

                int incrementIndex = combinationSize - 1;
                while (incrementIndex >= 0 && indices[incrementIndex] == count - combinationSize + incrementIndex)
                    incrementIndex--;

                if (incrementIndex < 0) break;

                indices[incrementIndex]++;
                for (int j = incrementIndex + 1; j < combinationSize; j++)
                    indices[j] = indices[j - 1] + 1;
            }
        }


        private static double CalculateAspectRatio(List<Point> quadrilateral)
        {
            if (quadrilateral == null || quadrilateral.Count != 4)
            {
                throw new ArgumentException("Four points are required to calculate the aspect ratio of a quadrilateral.");
            }

            // Calculate the distances between opposing sides
            double width1 = DistanceBetweenPoints(quadrilateral[0], quadrilateral[1]);
            double width2 = DistanceBetweenPoints(quadrilateral[2], quadrilateral[3]);
            double height1 = DistanceBetweenPoints(quadrilateral[1], quadrilateral[2]);
            double height2 = DistanceBetweenPoints(quadrilateral[3], quadrilateral[0]);

            double avgWidth = (width1 + width2) / 2.0;
            double avgHeight = (height1 + height2) / 2.0;

            return avgWidth / avgHeight;
        }

        private static double DistanceBetweenPoints(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        private static double CalculateAngleConsistency(List<Point> quadrilateral)
        {
            if (quadrilateral == null || quadrilateral.Count != 4)
            {
                throw new ArgumentException("Four points are required to calculate the angle consistency of a quadrilateral.");
            }

            double totalDeviation = 0;
            for (int i = 0; i < 4; i++)
            {
                Point p1 = quadrilateral[i];
                Point p2 = quadrilateral[(i + 1) % 4];
                Point p3 = quadrilateral[(i + 2) % 4];

                double angle = CalculateAngle(p1, p2, p3);
                totalDeviation += Math.Abs(90 - angle);
            }

            return totalDeviation;
        }

        private static double CalculateAngle(Point p1, Point p2, Point p3)
        {
            double a = DistanceBetweenPoints(p1, p2);
            double b = DistanceBetweenPoints(p2, p3);
            double c = DistanceBetweenPoints(p1, p3);

            // Cosine rule: cos(C) = (a^2 + b^2 - c^2) / (2ab)
            double angleRad = Math.Acos((a * a + b * b - c * c) / (2 * a * b));
            return angleRad * (180 / Math.PI);
        }

        public static double CalculateQuadrilateralArea(List<Point> points)
        {
            if (points.Count != 4)
            {
                throw new ArgumentException("Four points are required to calculate the area of a quadrilateral.");
            }

            // Area = 0.5 * |x1*y2 + x2*y3 + x3*y4 + x4*y1 - x2*y1 - x3*y2 - x4*y3 - x1*y4|
            double area = 0;
            for (int i = 0; i < 4; i++)
            {
                int nextIndex = (i + 1) % 4;
                area += points[i].X * points[nextIndex].Y;
                area -= points[i].Y * points[nextIndex].X;
            }

            return Math.Abs(area / 2.0);
        }

        private static List<Point> OrderCorners(List<Point> corners)
        {
            if (corners == null || corners.Count != 4)
                throw new ArgumentException("There must be exactly 4 corners.");

            // center point of all corners
            var centerX = corners.Average(point => point.X);
            var centerY = corners.Average(point => point.Y);

            // Order corners: top-left, top-right, bottom-right, bottom-left
            Point topLeft = corners.OrderBy(point => Math.Atan2(point.Y - centerY, point.X - centerX)).First();
            Point bottomLeft = corners.OrderByDescending(point => Math.Atan2(point.Y - centerY, point.X - centerX)).First();
            Point topRight = corners.OrderBy(point => Math.Atan2(point.Y - centerY, centerX - point.X)).First();
            Point bottomRight = corners.OrderByDescending(point => Math.Atan2(point.Y - centerY, centerX - point.X)).First();

            return new List<Point> { topLeft, topRight, bottomRight, bottomLeft };
        }
        #endregion

    }
}
