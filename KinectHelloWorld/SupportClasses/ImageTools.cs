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
        public static Image<TColor, TDepth> Crop<TColor, TDepth> (this Image<TColor, TDepth> img, Rectangle area) 
            where TColor : struct, IColor where TDepth : new() {
            try {
                img.ROI = area;
                return img.Copy();
                
            }
            catch( Exception e ) {
                throw e;
            }
        }


    }
}
