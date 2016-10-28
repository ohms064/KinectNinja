using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.Interaction;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using static Emgu.CV.Face.FaceRecognizer;
using ColorImage = Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte>;
using GrayImage = Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>;

namespace KinectHelloWorld.SupportClasses {
    public static class ImageTools {
        /// <summary>
        /// For an ImageFrame returns an Image.
        /// </summary>
        /// <param name="colorFrame"></param>
        /// <returns>The corresponding image.</returns>
        public static ColorImage GetColorImage(ColorImageFrame colorFrame) {
            byte[] imgBytes = new byte[colorFrame.PixelDataLength];
            colorFrame.CopyPixelDataTo(imgBytes);
            Bitmap bmp = new Bitmap(colorFrame.Width, colorFrame.Height, PixelFormat.Format32bppRgb);
            BitmapData bmpData = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.WriteOnly,
                bmp.PixelFormat);
            Marshal.Copy(imgBytes, 0, bmpData.Scan0, imgBytes.Length);
            bmp.UnlockBits(bmpData);

            return new ColorImage(bmp);
        }

        /// <summary>
        /// Crops an image.
        /// </summary>
        /// <typeparam name="TColor"></typeparam>
        /// <typeparam name="TDepth"></typeparam>
        /// <param name="img"></param>
        /// <param name="area"></param>
        /// <returns>The image of the selected area.</returns>
        public static Image<TColor, TDepth> Crop<TColor, TDepth> (this Image<TColor, TDepth> img, Rectangle area) 
            where TColor : struct, IColor where TDepth : new() {
            Rectangle previous = img.ROI;
            try {
                img.ROI = area;
                return img.Copy();
                
            }
            catch( Exception e ) {
                throw e;
            }
            finally {
                img.ROI = previous;
            }
        }

        /// <summary>
        /// Extension method which will process the image for the Classifier.
        /// </summary>
        /// <typeparam name="TColor"></typeparam>
        /// <typeparam name="TDepth"></typeparam>
        /// <param name="img"></param>
        public static void Processing<TColor, TDepth>(this Image<TColor, TDepth> img)
            where TColor : struct, IColor where TDepth : new() {
            try {
                img._EqualizeHist();
                img.Sobel(1, 1, 5);
                img._SmoothGaussian(5);
            }
            catch( Exception e ) {
                throw e;
            }
        }

        /// <summary>
        /// Orders a list of rectnagles in descending order.
        /// </summary>
        /// <param name="rects"></param>
        public static void OrderRectanglesByArea(ref List<Rectangle> rects) {
            rects.Sort(delegate (Rectangle first, Rectangle second) {
                float firstArea = first.Height * first.Width;
                float secondArea = second.Height * second.Width;
                if( firstArea > secondArea ) {
                    return -1;
                }
                else {
                    return 1;
                }
            });
        }

        /// <summary>
        /// For a sorted list of rectnagles by area, removes any rectangle that contains another.
        /// </summary>
        /// <param name="rects"></param>
        public static void RemoveInnerRectangles(ref List<Rectangle> rects) {
            for( int i = 0; i < rects.Count - 1; i++ ) {
                for( int j = i + 1; j < rects.Count; j++ ) {
                    if( rects[i].Contains(rects[j]) ) {
                        rects.RemoveAt(i);
                        break;
                    }
                }
            }
        }

    }
}
