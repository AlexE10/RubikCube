using Emgu.CV.Structure;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RubikCube.Tools
{
    public class MorphOps
    {
        public static Image<Gray, byte> Erosion(Image<Gray, byte> grayInitialImage, int size)
        {
            int width = grayInitialImage.Width;
            int height = grayInitialImage.Height;

            Image<Gray, byte> result = new Image<Gray, byte>(width, height);


            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte minValue = 255;

                    for (int ky = 0; ky < size; ky++)
                    {
                        for (int kx = 0; kx < size; kx++)
                        {
                            int pixelX = x + kx - size / 2;
                            int pixelY = y + ky - size / 2;

                            if (pixelX >= 0 && pixelX < width && pixelY >= 0 && pixelY < height)
                            {
                                byte pixelValue = grayInitialImage.Data[pixelY, pixelX, 0];
                                minValue = (byte)Math.Min(minValue, pixelValue);
                            }
                        }
                    }

                    result[y, x] = new Gray(minValue);
                }
            }

            return result;
        }

        public static Image<Gray, byte> Dilation(Image<Gray, byte> grayInitialImage, int size)
        {
            int width = grayInitialImage.Width;
            int height = grayInitialImage.Height;

            Image<Gray, byte> result = new Image<Gray, byte>(width, height);


            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte maxValue = 0;

                    for (int ky = 0; ky < size; ky++)
                    {
                        for (int kx = 0; kx < size; kx++)
                        {
                            int pixelX = x + kx - size / 2;
                            int pixelY = y + ky - size / 2;

                            if (pixelX >= 0 && pixelX < width && pixelY >= 0 && pixelY < height)
                            {
                                byte pixelValue = grayInitialImage.Data[pixelY, pixelX, 0];
                                maxValue = (byte)Math.Max(maxValue, pixelValue);
                            }
                        }
                    }

                    result[y, x] = new Gray(maxValue);
                }
            }

            return result;
        }
    }
}
