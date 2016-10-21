using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectHelloWorld.SupportClasses {
    public static class BitmapTools {

        /// <summary>
        /// From http://stackoverflow.com/questions/6484357/converting-bitmapimage-to-bitmap-and-vice-versa
        /// by Sascha Hennig edited by Aric Fowler
        /// </summary>
        /// <param name="bitmapImage"></param>
        /// <returns></returns>
        public static Bitmap ToBitmap(this BitmapImage bitmapImage) {
            // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

            using( MemoryStream outStream = new MemoryStream() ) {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }

        /// <summary>
        /// From http://stackoverflow.com/questions/6484357/converting-bitmapimage-to-bitmap-and-vice-versa
        /// by Sascha Hennig edited by Aric Fowler
        /// </summary>
        /// <param name="hObject"></param>
        /// <returns></returns>
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        public static BitmapImage ToBitmapImage(this Bitmap bitmap) {
            using( System.IO.MemoryStream memory = new System.IO.MemoryStream() ) {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }

        /// <summary>
        /// Converts a Bitmap to BitmapSource to use in WPF.
        /// From http://stackoverflow.com/questions/26260654/wpf-converting-bitmap-to-imagesource 
        /// by Mike Strobel
        /// </summary>
        /// <param name="bitmap">The bitmap to convert.</param>
        /// <returns>The covnerted Bitmap.</returns>
        public static BitmapSource ToBitmapSource(this Bitmap bitmap) {
            if( bitmap == null )
                throw new ArgumentNullException("bitmap");

            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

            var bitmapData = bitmap.LockBits(
                rect,
                ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            try {
                var size = ( rect.Width * rect.Height ) * 4;

                return BitmapSource.Create(
                    bitmap.Width,
                    bitmap.Height,
                    bitmap.HorizontalResolution,
                    bitmap.VerticalResolution,
                    PixelFormats.Bgra32,
                    null,
                    bitmapData.Scan0,
                    size,
                    bitmapData.Stride);
            }
            finally {
                bitmap.UnlockBits(bitmapData);
            }
        }
    }
}
