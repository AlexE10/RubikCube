using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.Structure;
using Emgu.CV;

namespace RubikCube.Tools
{
    public class ConvertImagesToString
    {
        private static readonly string imageFolderPath = "../../../Images/output/threshold";
        private static readonly List<string> orderedFaces = new List<string>() { "mid", "bot", "back", "top", "right", "left" };

        public static string ConvertCubeToString()
        {
            //SolveRedCase();

            string[] cubeFaces = GetAllSubfolders(imageFolderPath);
            string cubeString = InstantiateCubeString();

            foreach (string cubeFace in cubeFaces)
            {
                string currentFaceString = ConvertFaceToString(cubeFace);

                int test = orderedFaces.IndexOf(cubeFace.Split("\\").Last());
                cubeString = InsertInCubeString(cubeString, currentFaceString, orderedFaces.IndexOf(cubeFace.Split("\\").Last()));
            }

            return cubeString.ToLower();
        }

        private static string InsertInCubeString(string cubeString, string stringToInsert, int faceIndex)
        {
            int positionToReplace = faceIndex * 9;
            string result = "";

            if(positionToReplace + 9 < cubeString.Length)
            {
                result = cubeString.Substring(0, positionToReplace) + stringToInsert + cubeString.Substring(positionToReplace + 9);
            }
            else
            {
                result = cubeString.Substring(0, positionToReplace) + stringToInsert;
            }

            return result;
        }

        private static string InstantiateCubeString()
        {
            string result = "";

            for (int i = 0; i < 54; i++)
            {
                result = result + "n";
            }

            return result;
        }
        private static string ConvertFaceToString(string faceName)
        {
            string[] imagesForCurrentFace = LoadImagesFromFolder(faceName);
            string currentFaceString = "NNNnnnNNN";

            foreach (string image in imagesForCurrentFace)
            {
                string currentImageName = "" + image.Split("\\").Last().Split(".")[0][0];

                if (IsImageFile(image))
                {
                    Image<Gray, byte> currentImage = new Image<Gray, byte>(image);

                    currentFaceString = ConvertImageToString(currentImage, currentFaceString, currentImageName);
                }
            }

            return currentFaceString;
        }

        private static string ConvertImageToString(Image<Gray, byte> imageToConvert, string currentFaceString, string currentColorName)
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
            string result = "";

            if(positionToReplace + 1 < currentString.Length)
            {
                result = currentString.Substring(0, positionToReplace) + stringToReplace + currentString.Substring(positionToReplace + 1);
            }
            else
            {
                result = currentString.Substring(0, positionToReplace) + stringToReplace;
            }

            return result;
        }

        #region Utilities
        private static string[] LoadImagesFromFolder(string folderPath)
        {
            return Directory.GetFiles(folderPath);
        }

        private static string[] GetAllSubfolders(string folderPath)
        {
            return Directory.GetDirectories(folderPath);
        }

        private static bool IsImageFile(string fileName)
        {
            string[] extensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
            string extension = Path.GetExtension(fileName).ToLower();
            return Array.Exists(extensions, ext => ext == extension);
        }

        private static void SolveRedCase()
        {
            string[] allSubdirectors = GetAllSubfolders(imageFolderPath);
            List<string> redCasesString = new List<string>() { "Red1", "Red2" };

            foreach (var subFolder in allSubdirectors)
            {
                List<Image<Gray, byte>> redCasesImages = new List<Image<Gray, byte>>();
                string[] currentFolderImages = LoadImagesFromFolder(Path.Combine(imageFolderPath, subFolder));

                foreach (var image in currentFolderImages)
                {
                    string test = image.Split("\\").Last().Split(".")[0];
                    if (redCasesString.Contains(test))
                    {
                        redCasesImages.Add(new Image<Gray, byte>(image));
                        File.Delete(image);
                    }
                }

                Image<Gray, byte> combinedImage = CombineTwoImage(redCasesImages[0], redCasesImages[1]);

                string savePath = Path.Combine(imageFolderPath, subFolder);
                combinedImage.Save(Path.Combine(savePath, "red" + ".png"));
            }
        }

        private static Image<Gray, byte> CombineTwoImage(Image<Gray, byte> firstImage, Image<Gray, byte> secondImage)
        {
            Image<Gray, byte> combinedImage = new Image<Gray, byte>(firstImage.Size);

            for (int i = 0; i < combinedImage.Height; i++)
            {
                for (int j = 0; j < combinedImage.Width; j++)
                {
                    if (firstImage.Data[i, j, 0] == 255)
                    {
                        combinedImage.Data[i, j, 0] = firstImage.Data[i, j, 0];
                    }
                    else
                    {
                        if (secondImage.Data[i, j, 0] == 255)
                        {
                            combinedImage.Data[i, j, 0] = secondImage.Data[i, j, 0];
                        }
                        else
                        {
                            combinedImage.Data[i, j, 0] = 0;
                        }
                    }
                }
            }

            return combinedImage;
        }
        #endregion
    }
}
