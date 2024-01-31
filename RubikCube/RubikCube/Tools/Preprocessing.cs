using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Markup;
using Emgu.CV;
using Emgu.CV.Structure;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.WindowsForms;

using Matrix = System.Collections.Generic.List<System.Collections.Generic.List<double>>;

namespace RubikCube.Tools
{
    public class Preprocessing
    {
        #region Denoising
        public static Image<Bgr, byte> Denoising(Image<Bgr, byte> inputImage)
        {
            Image<Bgr, byte> resultImage = new Image<Bgr, byte>(inputImage.Size);


            for (int y = 2; y < inputImage.Height - 2; y++)
            {
                for (int x = 2; x < inputImage.Width - 2; x++)
                {
                    byte[] pixelWithMinDistance = CalculateNewColorForCurrentPixel(inputImage, x, y);

                    resultImage.Data[y, x, 0] = pixelWithMinDistance[0];
                    resultImage.Data[y, x, 1] = pixelWithMinDistance[1];
                    resultImage.Data[y, x, 2] = pixelWithMinDistance[2];
                }
            }

            return resultImage;
        }

        public static byte[] CalculateNewColorForCurrentPixel(Image<Bgr, byte> inputImage, int x, int y)
        {
            Image<Bgr, byte> regionImage = new Image<Bgr, byte>(5, 5);

            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    regionImage.Data[i, j, 0] = inputImage.Data[i + y - 2, j + x - 2, 0];
                    regionImage.Data[i, j, 1] = inputImage.Data[i + y - 2, j + x - 2, 1];
                    regionImage.Data[i, j, 2] = inputImage.Data[i + y - 2, j + x - 2, 2];
                }
            }

            float[][] distanceMatrix = CalculateDistanceForOnePixel(regionImage);

            int newColor = NewColor(distanceMatrix);

            byte[] resultPixel = new byte[] { regionImage.Data[newColor / 5, newColor % 5, 0], regionImage.Data[newColor / 5, newColor % 5, 1], regionImage.Data[newColor / 5, newColor % 5, 2] };

            return resultPixel;
        }

        private static float[][] CalculateDistanceForOnePixel(Image<Bgr, byte> regionImage)
        {
            float[][] distances = new float[25][];

            for (int i = 0; i < distances.Length; i++)
            {
                distances[i] = new float[distances.Length];
            }

            float distance;

            for (int i = 0; i < distances.Length; i++)
            {
                for (int j = 0; j < distances[i].Length; j++)
                {
                    if (i < j)
                    {
                        distance = (float)Math.Sqrt(Math.Pow(((float)regionImage.Data[i / 5, i % 5, 0] - (float)regionImage.Data[j / 5, j % 5, 0]), 2)
                        + Math.Pow(((float)regionImage.Data[i / 5, i % 5, 1] - (float)regionImage.Data[j / 5, j % 5, 1]), 2)
                        + Math.Pow(((float)regionImage.Data[i / 5, i % 5, 2] - (float)regionImage.Data[j / 5, j % 5, 2]), 2));

                        distances[i][j] = distance;
                        distances[j][i] = distance;
                    }
                }
            }

            return distances;
        }

        private static int NewColor(float[][] distances)
        {
            List<float> sumList = new List<float>();

            foreach (float[] vector in distances)
            {
                sumList.Add(vector.Sum());
            }

            return sumList.IndexOf(sumList.Min());
        }
        #endregion

        #region LightCorrection
        private void ComputeHistogramForImage(Image<Bgr, byte> inputImage)
        {

        }
        public static void PlotData(List<int> data, string imagePath)
        {
            var plotView = new PlotView();
            var plotModel = new PlotModel();

            var lineSeries = new LineSeries()
            {
                Color = OxyColors.Blue // Set the color of the line series
            };

            for (int i = 0; i < data.Count; i++)
            {
                lineSeries.Points.Add(new DataPoint(i, data[i]));
            }

            plotModel.Series.Add(lineSeries);
            plotView.Model = plotModel;

            //panel.Controls.Add(plotView);

            // Save the plot as an image
            OxyPlot.Wpf.PngExporter.Export(plotModel, imagePath, 1000, 400);
        }
        public static void ComputeHistogramForSpecificChannel(Image<Bgr, byte> inputImage)
        {
            List<int> histogramBlue = new List<int>(Enumerable.Repeat(0, 256));
            List<int> histogramGreen = new List<int>(Enumerable.Repeat(0, 256));
            List<int> histogramRed = new List<int>(Enumerable.Repeat(0, 256));


            for (int y = 0; y < inputImage.Height; y++)
            {
                for (int x = 0; x < inputImage.Width; x++)
                {
                    byte test = inputImage.Data[y, x, 0];
                    histogramBlue[inputImage.Data[y, x, 0]]++;
                    histogramGreen[inputImage.Data[y, x, 1]]++;
                    histogramRed[inputImage.Data[y, x, 2]]++;
                }
            }

            PlotData(histogramBlue, @"D:\Facultate\Anul_3\PID\Proiect\Rubik-Cube\RubikCube\RubikCube\ProcessedImages\plotBlue.png");
            PlotData(histogramGreen, @"D:\Facultate\Anul_3\PID\Proiect\Rubik-Cube\RubikCube\RubikCube\ProcessedImages\plotGreen.png");
            PlotData(histogramRed, @"D:\Facultate\Anul_3\PID\Proiect\Rubik-Cube\RubikCube\RubikCube\ProcessedImages\plotRed.png");
        }
        #endregion

        #region Equalization

        public static Image<Bgr, byte> EqualizeVComponent(Image<Bgr, byte> image)
        {
            var hsvImage = Tools.ConvertToHSV(image);
            Image<Gray, byte> valueChannel = hsvImage.Split()[2];

            // Compute histogram and cumulative histogram
            float[] histogram = ComputeRelativeHistogramValue(valueChannel);
            float[] cumulativeHistogram = ComputeCumulativeHistogramOnValue(histogram);

            // Define lower and upper limits for contrast stretching
            int lowerLimit = 0;
            int upperLimit = 255;
            while (cumulativeHistogram[lowerLimit] < 0.01) lowerLimit++;
            while (cumulativeHistogram[upperLimit] > 0.99) upperLimit--;

            byte[] lut = new byte[256];
            float scale = 255.0f / (upperLimit - lowerLimit);

            // Build LUT with contrast stretching
            for (int i = 0; i < 256; i++)
            {
                if (i < lowerLimit) lut[i] = 0;
                else if (i > upperLimit) lut[i] = 255;
                else lut[i] = (byte)((i - lowerLimit) * scale);
            }

            Image<Hsv, byte> equalizedImage = hsvImage.Clone();
            for (int y = 0; y < hsvImage.Height; y++)
            {
                for (int x = 0; x < hsvImage.Width; x++)
                {
                    byte valuePixel = valueChannel.Data[y, x, 0];
                    equalizedImage.Data[y, x, 2] = lut[valuePixel];
                }
            }
            Image<Bgr, byte> bgrImage = equalizedImage.Convert<Bgr, byte>();
            return bgrImage;
        }

        public static float[] ComputeRelativeHistogramValue(Image<Gray, byte> chanelValue)
        {
            float[] relativeHistogram = new float[256];

            for (int y = 0; y < chanelValue.Height; y++)
            {
                for (int x = 0; x < chanelValue.Width; x++)
                {
                    byte pixel = chanelValue.Data[y, x, 0];
                    relativeHistogram[pixel]++;
                }
            }

            int n = chanelValue.Width * chanelValue.Height;

            for (int i = 0; i < 256; i++)
            {
                relativeHistogram[i] = relativeHistogram[i] / n;
            }

            return relativeHistogram;
        }

        public static float[] ComputeCumulativeHistogramOnValue(float[] relativeHistogram)
        {
            float[] histogram = new float[256];

            for (int m = 0; m < 256; m++)
            {
                for (int n = 0; n <= m; n++)
                {
                    histogram[m] = histogram[m] + relativeHistogram[n];
                }
            }

            return histogram;
        }
        #endregion

        #region Projection Transformation

        public static Image<Bgr, byte> ProjectionTransformation(Image<Bgr, byte> inputImage, List<Point> inputPoints, List<Point> outputPoints)
        {
            //Matrix test = new Matrix() { new List<double> { 1, 2, 3 } , new List<double> { 1, 2, 3 } , new List<double> { 1, 2, 3 } };
            //Matrix test2 = new Matrix() { new List<double> { 1, 2, 3 } , new List<double> { 1, 2, 3 } , new List<double> { 1, 2, 3 } };

            //Matrix test4 = MultiplyMatrices(test, test2);
            //Matrix test3 = InvertMatrix(test);

            Image<Bgr, byte> result = new Image<Bgr, byte>(inputImage.Width, inputImage.Height);

            Matrix pMatrix = CalculatePMatrix(inputPoints);
            Matrix pSecondMatrix = CalculatePMatrix(outputPoints);

            Matrix bMatrix = CalculateBVector(pSecondMatrix, TransformPointToMatrix(outputPoints[3]));
            Matrix bSecondMatrix = CalculateBVector(pMatrix, TransformPointToMatrix(inputPoints[3]));

            Matrix aMatrix = CalculateAMatrix(bMatrix, bSecondMatrix, inputPoints, outputPoints);

            for (int y = 0; y < result.Height; y++)
            {
                for (int x = 0; x < result.Width; x++)
                {
                    Matrix currentMatrix = MultiplyMatrices(aMatrix, TransformPointToMatrix(new Point(x, y)));

                    double xC = currentMatrix[0][0] / currentMatrix[2][0];
                    double yC = currentMatrix[1][0] / currentMatrix[2][0];

                    int x0 = (int)xC;
                    int y0 = (int)yC;

                    int x1 = x0 + 1;
                    int y1 = y0 + 1;

                    double xr = xC - x0;
                    double yr = yC - y0;

                    if (x0 >= 0 && y0 >= 0 && x1 < result.Width && y1 < result.Height)
                    {
                        result.Data[y, x, 0] = (byte)(CalculateResultForOneChannel(x0, x1, y0, y1, xr, yr, inputImage, 0) + 0.5f);
                        result.Data[y, x, 1] = (byte)(CalculateResultForOneChannel(x0, x1, y0, y1, xr, yr, inputImage, 1) + 0.5f);
                        result.Data[y, x, 2] = (byte)(CalculateResultForOneChannel(x0, x1, y0, y1, xr, yr, inputImage, 2) + 0.5f);
                    }
                }
            }

            return result;
        }

        private static double CalculateResultForOneChannel(int x0, int x1, int y0, int y1, double xr, double yr, Image<Bgr, byte> inputImage, int channelNumber)
        {
            double inter0 = xr * inputImage.Data[y0, x1, channelNumber] + (1 - xr) * inputImage.Data[y0, x0, channelNumber];
            double inter1 = xr * inputImage.Data[y1, x1, channelNumber] + (1 - xr) * inputImage.Data[y1, x0, channelNumber];

            double result = yr * inter1 + (1 - yr) * inter0;

            return result;
        }

        private static Matrix CalculateAMatrix(Matrix bFirst, Matrix bSecond, List<Point> inputPoints, List<Point> outputPoints)
        {
            Matrix firstPart = MultiplyNumberWithMatrix(bSecond[0][0] / bFirst[0][0], TransformPointToMatrix(inputPoints[0]));
            Matrix secondPart = MultiplyNumberWithMatrix(bSecond[1][0] / bFirst[1][0], TransformPointToMatrix(inputPoints[1]));
            Matrix thirdPart = MultiplyNumberWithMatrix(bSecond[2][0] / bFirst[2][0], TransformPointToMatrix(inputPoints[2]));

            List<double> firstLine = new List<double>() { firstPart[0][0], secondPart[0][0], thirdPart[0][0] };
            List<double> secondLine = new List<double>() { firstPart[1][0], secondPart[1][0], thirdPart[1][0] };
            List<double> thirdLine = new List<double>() { firstPart[2][0], secondPart[2][0], thirdPart[2][0] };

            Matrix test = new Matrix() { firstLine, secondLine, thirdLine };

            Matrix result = MultiplyMatrices(test, InvertMatrix(CalculatePMatrix(outputPoints)));

            return result;
        }



        private static Matrix MultiplyNumberWithMatrix(double number, Matrix matrix)
        {
            Matrix result = matrix;

            for (int i = 0; i < result.Count; i++)
            {
                for (int j = 0; j < result[i].Count; j++)
                {
                    result[i][j] *= number;
                }
            }

            return result;
        }

        private static Matrix TransformPointToMatrix(Point inputPoint)
        {
            return new Matrix { new List<double>() { inputPoint.X }, new List<double>() { inputPoint.Y }, new List<double>() { 1 } };
        }

        private static Matrix CalculatePMatrix(List<Point> points)
        {
            List<double> xList = new List<double>();
            List<double> yList = new List<double>();
            List<double> oneList = new List<double>() { 1, 1, 1 };

            for (int i = 0; i < points.Count - 1; i++)
            {
                xList.Add(points[i].X);
                yList.Add(points[i].Y);
            }

            Matrix result = new Matrix() { xList, yList, oneList };

            return result;
        }

        private static Matrix CalculateBVector(Matrix firstMatrix, Matrix secondMatrix)
        {
            Matrix result = MultiplyMatrices(InvertMatrix(firstMatrix), secondMatrix);

            return result;
        }

        public static Matrix MultiplyMatrices(Matrix matrixA, Matrix matrixB)
        {
            // Get the dimensions of the matrices
            int aRows = matrixA.Count;
            int aCols = matrixA[0].Count;
            int bRows = matrixB.Count;
            int bCols = matrixB[0].Count;

            // Check if multiplication is possible
            if (aCols != bRows)
                throw new InvalidOperationException("The number of columns in the first matrix must equal the number of rows in the second matrix.");

            // Initialize the result matrix with zeros
            Matrix result = new Matrix(
                Enumerable.Range(0, aRows).Select(i => new List<double>(new double[bCols]))
            );

            // Perform the multiplication
            for (int i = 0; i < aRows; i++)
            {
                for (int j = 0; j < bCols; j++)
                {
                    for (int k = 0; k < aCols; k++)
                    {
                        result[i][j] += matrixA[i][k] * matrixB[k][j];
                    }
                }
            }

            return result;
        }

        public static Matrix InvertMatrix(Matrix matrix)
        {
            Console.WriteLine("Salut");
            int n = matrix.Count;
            var result = new Matrix();
            var augmented = new Matrix();

            for (int i = 0; i < n; i++)
            {
                List<double> currentList = new List<double>(new double[n]); // initialize with zeros
                List<double> currentList2 = new List<double>(new double[2 * n]); // initialize with zeros

                result.Add(currentList);
                augmented.Add(currentList2);
            }

            // Create an augmented matrix [matrix|I]
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    augmented[i][j] = matrix[i][j];
                    augmented[i][j + n] = (i == j) ? 1 : 0;
                }
            }

            // Perform row operations
            for (int i = 0; i < n; i++)
            {
                // Scale to make pivot 1
                double pivot = augmented[i][i];
                if (pivot == 0)
                {
                    throw new InvalidOperationException("Matrix is singular and cannot be inverted.");
                }
                for (int j = 0; j < 2 * n; j++)
                {
                    augmented[i][j] /= pivot;
                }

                // Make other rows zero
                for (int k = 0; k < n; k++)
                {
                    if (k != i)
                    {
                        double factor = augmented[k][i];
                        for (int j = 0; j < 2 * n; j++)
                        {
                            augmented[k][j] -= factor * augmented[i][j];
                        }
                    }
                }
            }

            // Extract inverse matrix
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    result[i][j] = augmented[i][j + n];
                }
            }

            return result;
        }


        public static string ConvertImageToString(Image<Gray, byte> imageToConvert, string currentFaceString, string currentColorName)
        {
            string result = currentFaceString;

            int incremenFactor = (int)(imageToConvert.Width / 3);

            for (int i = 0; i < imageToConvert.Height - incremenFactor + 1; i += incremenFactor)
            {
                for (int j = 0; j < imageToConvert.Width - incremenFactor + 1; j += incremenFactor)
                {
                    if (IsValidCurrentSquare(i, j, incremenFactor, imageToConvert))
                    {
                        result = InsertInString(result, i, j, incremenFactor, currentColorName);
                    }
                }
            }

            return result;
        }

        //pozitia i/incrementFactor

        private static bool IsValidCurrentSquare(int indexI, int indexJ, int incrementFactor, Image<Gray, byte> imageToConvert)
        {
            int numberOfWhitePixels = 0;
            int numberOfBlackPixels = 0;

            for (int i = indexI; i < indexI + incrementFactor; i++)
            {
                for (int j = indexJ; j < indexJ + incrementFactor; j++)
                {
                    if (imageToConvert.Data[i, j, 0] == 255)
                    {
                        numberOfWhitePixels++;
                    }
                    else
                    {
                        numberOfBlackPixels++;
                    }
                }
            }

            if (numberOfWhitePixels > numberOfBlackPixels)
            {
                return true;
            }

            return false;
        }

        private static string InsertInString(string currentString, int i, int j, int incrementFactor, string stringToReplace)
        {
            int positionToReplace = i / incrementFactor * 3 + j / incrementFactor;
            string result = currentString.Substring(0, positionToReplace) + stringToReplace + currentString.Substring(positionToReplace + 1);

            return result;
        }
    }

    #endregion
}
