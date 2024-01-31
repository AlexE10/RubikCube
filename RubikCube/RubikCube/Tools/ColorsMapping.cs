using Emgu.CV.Structure;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RubikCube.Tools;

namespace RubikCube.Tools
{
    public class ColorsMapping
    {
        #region HueThresholding
        public static Image<Gray, byte> ApplyHueThresholding(Image<Hsv, byte> hsvImage, double lowerHueBound, double upperHueBound)
        {
            Image<Gray, byte> mask = new Image<Gray, byte>(hsvImage.Width, hsvImage.Height);

            for (int y = 0; y < hsvImage.Height; y++)
            {
                for (int x = 0; x < hsvImage.Width; x++)
                {
                    Hsv pixel = hsvImage[y, x];
                    if (pixel.Hue >= lowerHueBound && pixel.Hue <= upperHueBound)
                    {
                        mask[y, x] = new Gray(255);
                    }
                    else
                    {
                        mask[y, x] = new Gray(0);
                    }
                }
            }

            return mask;
        }
        #endregion

        #region ColorThresholding
        public static IDictionary<string, IDictionary<string, Image<Gray, byte>>> ApplyColorThresholdingToAllSides(
    IDictionary<string, Image<Bgr, byte>> sidesImages,
    IDictionary<string, (Hsv lower, Hsv upper)> colorHsvRanges)
        {
            var thresholdedImages = new Dictionary<string, IDictionary<string, Image<Gray, byte>>>();

            foreach (var sideImagePair in sidesImages)
            {
                IDictionary<string, Image<Gray, byte>> colorMasks = new Dictionary<string, Image<Gray, byte>>();
                Image<Hsv, byte> hsvImage = Tools.ConvertToHSV(sideImagePair.Value);

                foreach (var colorRange in colorHsvRanges)
                {
                    Image<Gray, byte> mask = ApplyColorThresholding(hsvImage, colorRange.Value.lower, colorRange.Value.upper);
                    colorMasks.Add(colorRange.Key, mask);
                }

                thresholdedImages.Add(sideImagePair.Key, colorMasks);
            }

            return thresholdedImages;
        }

        public static Image<Gray, byte> ApplyColorThresholding(Image<Hsv, byte> hsvImage, Hsv lowerBound, Hsv upperBound)
        {
            Image<Gray, byte> mask = new Image<Gray, byte>(hsvImage.Width, hsvImage.Height);

            for (int y = 0; y < hsvImage.Height; y++)
            {
                for (int x = 0; x < hsvImage.Width; x++)
                {
                    Hsv pixel = hsvImage[y, x];
                    if (IsInRange(pixel, lowerBound, upperBound))
                    {
                        mask[y, x] = new Gray(255);
                    }
                    else
                    {
                        mask[y, x] = new Gray(0);
                    }
                }
            }

            return mask;
        }

        public static bool IsInRange(Hsv color, Hsv lowerBound, Hsv upperBound)
        {
            return (color.Hue >= lowerBound.Hue && color.Hue <= upperBound.Hue &&
                    color.Satuation >= lowerBound.Satuation && color.Satuation <= upperBound.Satuation &&
                    color.Value >= lowerBound.Value && color.Value <= upperBound.Value);
        }
        #endregion

    }


}
