using System.Linq;
using Microsoft.Win32;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RubikCube.Utilities
{
    internal class FileHelper
    {
        public static string LoadFileDialog(string title)
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Title = title,
                Filter = "Image files (*.jpg, *.jpeg, *.jfif, *.jpe, *.bmp, *.png) | *.jpg; *.jpeg; *.jfif; *.jpe; *.bmp; *.png"
            };

            if (fileDialog.ShowDialog() == false || fileDialog.FileName.CompareTo("") == 0)
                return null;

            return fileDialog.FileName;
        }
    }
}
