using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Emgu.CV;
using Emgu.CV.Structure;

using static RubikCube.Utilities.DataProvider;
using static RubikCube.Utilities.FileHelper;
using static RubikCube.Converters.ImageConverter;
using System.Windows.Controls;
using System.Windows;
using static RubikCube.Tools.Tools;
using System.Windows.Media.Imaging;
using System.Reflection.Metadata;
using System.IO;
using Emgu.CV.Aruco;
using RubikCube.Tools;
using Point = System.Drawing.Point;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using RubikCube.Images;
using System.ComponentModel;

namespace RubikCube.ViewModel.Commands
{
    public class LoadImageCommand : BaseVM
    {
        private readonly MainVM _mainVM;
        IDictionary<string, Image<Bgr, byte>> sidesImages = new Dictionary<string, Image<Bgr, byte>>();


        public LoadImageCommand(MainVM mainVM)
        {
            _mainVM = mainVM;
        }

        private ImageSource LoadImage
        {
            get => _mainVM.LoadImage;
            set => _mainVM.LoadImage = value;
        }
        private ImageSource CurrentImage
        {
            get => _mainVM.CurrentImage;
            set => _mainVM.CurrentImage = value;
        }

        private double ScaleValue
        {
            get => _mainVM.ScaleValue;
            set => _mainVM.ScaleValue = value;
        }

        private ICommand _copyImageCommand;
        public ICommand CopyImageCommand
        {
            get
            {
                if (_copyImageCommand == null)
                    _copyImageCommand = new RelayCommand(CopyImage);
                return _copyImageCommand;
            }
        }

        private void CopyImage(object parameter)
        {
            CopyImageToButton(parameter as Button);
        }

        public void CopyImageToButton(Button button)
        {
            Image img = new Image();
            img.Source = LoadImage;

            StackPanel stackPnl = new StackPanel();
            stackPnl.Orientation = Orientation.Horizontal;
            stackPnl.Margin = new Thickness(10);
            stackPnl.Children.Add(img);

            button.Content = stackPnl;
            button.IsEnabled = false;
        }

        private ICommand loadColorImageCommand;
        public ICommand LoadColorImageCommand
        {
            get
            {
                if (loadColorImageCommand == null)
                    loadColorImageCommand = new RelayCommand(LoadColorImage);
                return loadColorImageCommand;
            }
        }

        private void LoadColorImage(object parameter)
        {
            //Clear(parameter);
            string fileName = LoadFileDialog("Select a color picture");
            if (fileName != null)
            {
                ColorInitialImage = new Image<Bgr, byte>(fileName);
                LoadImage = Convert(ColorInitialImage);
            }
        }

        private ICommand resetCubeFaces;

        public ICommand ResetCubeFaces
        {
            get
            {
                if (resetCubeFaces == null)
                    resetCubeFaces = new RelayCommand(ResetFaces);
                return resetCubeFaces;
            }
        }

        private void ResetFaces(object parameter)
        {
            Reset(parameter as Panel);
        }

        private void Reset(Panel panel)
        {
            foreach (UIElement element in panel.Children)
            {
                if (element is Button button)
                {
                    button.Content = null;
                    button.IsEnabled = true;
                }
                else if (element is Panel childPanel)
                {
                    Reset(childPanel);
                }
            }
        }

        private ICommand submitCommand;

        public ICommand SubmitCommand
        {
            get
            {
                if (submitCommand == null)
                    submitCommand = new RelayCommand(SubmitImg);
                return submitCommand;
            }
        }


        private void SubmitImg(object parameter)
        {
            Panel panel = parameter as Panel;
            Submit(panel);
            IDictionary<string, Image<Bgr, byte>> processedImages = new Dictionary<string, Image<Bgr, byte>>();
            IDictionary<string, IDictionary<string, Image<Gray, byte>>> thresholdedImagesBySide = new Dictionary<string, IDictionary<string, Image<Gray, byte>>>();
            string stepsSavePath = "../../../Images/output/stepsOutput";

            if (sidesImages.Count() == 6)
            {
                var progressWindow = new ProgressWindow();
                progressWindow.Show();

                BackgroundWorker worker = new BackgroundWorker();
                worker.WorkerReportsProgress = true;

                int totalImages = sidesImages.Count();
                int processedImagesCount = 0;

                worker.DoWork += (s, args) =>
                {

                    foreach ((var key, var image) in sidesImages)
                    {
                        var auxImage = Tools.HighPassFilters.ApplyGaussianBlur(image, 7, 1);

                        var processedImage = Tools.HighPassFilters.ApplyCannyEdgeDetectorForColor(auxImage, 170, 250);

                        string cannyFolderPath = Path.Combine(stepsSavePath, "canny");

                        if (!Directory.Exists(cannyFolderPath))
                        {
                            Directory.CreateDirectory(cannyFolderPath);
                        }
                        processedImage.Save(Path.Combine(cannyFolderPath, key + ".png"));

                        double k = 0.2;
                        double threshold = 10000000;

                        var corners = Segmentation.DetectHarrisCorners(processedImage, k, threshold);

                        foreach (var point in corners)
                        {
                            CvInvoke.Circle(processedImage, point, 3, new MCvScalar(255, 255, 255), 2);
                        }
                        string harrisFolderPath = Path.Combine(stepsSavePath, "harris");

                        if (!Directory.Exists(harrisFolderPath))
                        {
                            Directory.CreateDirectory(harrisFolderPath);
                        }
                        processedImage.Save(Path.Combine(harrisFolderPath, key + ".png"));

                        var finalImage = Segmentation.ExtractAndCorrectCubeFace(image, corners);
                        processedImages.Add(key, finalImage);

                        processedImagesCount++;
                        int progressPercentage = (processedImagesCount * 100) / totalImages;
                        worker.ReportProgress(progressPercentage);
                    }


                    string saveDirectory = "../../../Images/output/croppedCubeFaces";
                    if (!Directory.Exists(saveDirectory))
                    {
                        Directory.CreateDirectory(saveDirectory);
                    }

                    foreach (var item in processedImages)
                    {
                        string savePath = Path.Combine(saveDirectory, item.Key + ".png");
                        item.Value.Save(savePath);
                    }

                    IDictionary<string, (Hsv lower, Hsv upper)> colorHsvRanges = new Dictionary<string, (Hsv lower, Hsv upper)>
                {
                    { "Red1", (new Hsv(0, 50, 50), new Hsv(3, 255, 255)) }, // Lower end of Red
                    { "Orange", (new Hsv(5, 50, 50), new Hsv(20, 255, 255)) }, // Orange
                    { "Yellow", (new Hsv(20, 50, 50), new Hsv(35, 255, 255)) }, // Yellow
                    { "Green", (new Hsv(40, 50, 50), new Hsv(75, 255, 255)) }, // Green
                    { "Blue", (new Hsv(100, 50, 50), new Hsv(130, 255, 255)) }, // Blue
                    { "Red2", (new Hsv(160, 50, 50), new Hsv(180, 255, 255)) }, // Higher end of Red

                    { "White", (new Hsv(0, 0, 80), new Hsv(180, 30, 255)) } // White
                };

                    string thresholdSaveDirectory = "../../../Images/output/threshold";
                    if (!Directory.Exists(thresholdSaveDirectory))
                    {
                        Directory.CreateDirectory(thresholdSaveDirectory);
                    }
                    thresholdedImagesBySide = ColorsMapping.ApplyColorThresholdingToAllSides(processedImages, colorHsvRanges);
                    foreach ((var key, var item) in thresholdedImagesBySide)
                    {
                        string newFolderPath = Path.Combine(thresholdSaveDirectory, key);
                        if (!Directory.Exists(newFolderPath))
                        {
                            Directory.CreateDirectory(newFolderPath);
                        }
                        foreach (var color in item)
                        {
                            string savePath = Path.Combine(newFolderPath, color.Key + ".png");
                            color.Value.Save(savePath);
                        }

                    }

                    MessageBox.Show("Processing and saving completed! Have fun solving!");
                
                string stringCube = ConvertImagesToString.ConvertCubeToString();

                File.WriteAllText("../../../Solver/CubeString.txt", stringCube);
                string processPath = "../../../Solver/RubikCubeApplication/RubikCubeUi.exe";
                if (System.IO.File.Exists(processPath))
                {
                    Process.Start(processPath);
                }
                };

                worker.ProgressChanged += (s, args) =>
                {
                    progressWindow.UpdateProgress(args.ProgressPercentage);
                };

                worker.RunWorkerCompleted += (s, args) =>
                {
                    // Task completed. Close the progress window
                    progressWindow.Close();
                };

                worker.RunWorkerAsync();
            }
            else
            {
                MessageBox.Show("Not all images loaded!");
                sidesImages.Clear();
            }

        }

        private void Submit(Panel panel)
        {

            foreach (UIElement element in panel.Children)
            {
                if (element is Button button)
                {
                    if (button.Content is StackPanel)
                    {
                        StackPanel stackPanel = (StackPanel)button.Content;
                        if (stackPanel.Children[0] != null)
                        {
                            Image img = stackPanel.Children[0] as Image;
                            Image<Bgr, byte> emguImg = ConvertToEmguImage(img);
                            sidesImages.Add(button.Name, emguImg);
                        }
                    }
                }
                else if (element is Panel childPanel)
                {
                    Submit(childPanel);
                }
            }

        }
    }
}
