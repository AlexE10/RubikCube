using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using RubikCube.ViewModel.Commands;

namespace RubikCube.ViewModel
{
    public class MainVM : BaseVM
    {
        public LoadImageCommand LoadImageCommandLocal { get; set; }
        public MainVM() 
        {
            LoadImageCommandLocal = new LoadImageCommand(this);
            ScaleValue = 1;
        }

        private ImageSource currentImage;

        public ImageSource CurrentImage
        {
            get
            {
                return currentImage;
            }
            set
            {
                currentImage = value;
            }
        }

        private ImageSource loadImage;
        public ImageSource LoadImage
        {
            get
            {
                return loadImage;
            }
            set
            {
                loadImage = value;

                if (loadImage != null)
                {
                    LoadImageCanvasWidth = LoadImage.Width * ScaleValue;
                    LoadImageCanvasHeight = LoadImage.Height * ScaleValue;
                }
                else
                {
                    LoadImageCanvasWidth = 0;
                    LoadImageCanvasHeight = 0;
                }

                NotifyPropertyChanged(nameof(LoadImage));
            }
        }
        private double scaleValue;
        public double ScaleValue
        {
            get => scaleValue;
            set
            {
                scaleValue = value;
                NotifyPropertyChanged(nameof(ScaleValue));
            }
        }

        public double loadImageCanvasWidth;
        public double LoadImageCanvasWidth
        {
            get => loadImageCanvasWidth;
            set
            {
                loadImageCanvasWidth = value;
                NotifyPropertyChanged(nameof(LoadImageCanvasWidth));
            }
        }

        public double loadImageCanvasHeight;
        public double LoadImageCanvasHeight
        {
            get => loadImageCanvasHeight;
            set
            {
                loadImageCanvasHeight = value;
                NotifyPropertyChanged(nameof(LoadImageCanvasHeight));
            }
        }
    }
}
