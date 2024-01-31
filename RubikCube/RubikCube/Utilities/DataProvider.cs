using Emgu.CV;
using Emgu.CV.Structure;
using System.Linq;
using System.Windows;
using PointCollection = System.Windows.Media.PointCollection;

namespace RubikCube.Utilities
{
    class DataProvider
    {
        public static Image<Bgr, byte> ColorInitialImage { get; set; }
        public static Image<Bgr, byte> ColorCurrentImage { get; set; }
    }
}
