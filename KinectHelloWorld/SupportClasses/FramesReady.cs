
using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.CvEnum;
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
    class FramesReady {
        private const double fontSize = 0.5D;
        private const int fontThickness = 1;
        public static bool DepthFrameReady(DepthImageFrame depthFrame, ref InteractionStream interactionStream) {
            try {
                interactionStream.ProcessDepth(depthFrame.GetRawPixelData(), depthFrame.Timestamp);
                return true;
            }
            catch( InvalidOperationException ) {
                return false;
                // DepthFrame functions may throw when the sensor gets
                // into a bad state.  Ignore the frame in that case.
            }
        }
        public static ColorImage ColorFrameReady(ColorImageFrame colorFrame, GenderClassifier genderClassifier, Rectangle areaOfInterest, KinectSensor sensor, Joint head) {

            ColorImage img = ImageTools.GetColorImage(colorFrame);
            List<Rectangle> facesResult = genderClassifier.GetFaces(img, areaOfInterest);
            Rectangle playerFace = genderClassifier.GetFaceByJoint(img, sensor, head);
            for( int i = 0; i < facesResult.Count; i++ ) {
                if( playerFace.IntersectsWith(facesResult[i]) ) {
                    facesResult.RemoveAt(i);
                }
            }
            PredictionResult[] predictions;
            PredictionResult playerPrediction;
            try {
                predictions = genderClassifier.ClassifyByFaces(img, areaOfInterest, facesResult.ToArray());
                playerPrediction = genderClassifier.PredictFace(img, playerFace);
            }
            catch {
                return null;
                //Para cualquier error ignoramos el frame.
            }
            Bgr color = genderClassifier.ColorChooser(playerPrediction);
            img.Draw(playerFace, color, 5);
            img.Draw(playerPrediction.Distance.ToString(), playerFace.Location, FontFace.HersheyDuplex, fontSize, color, fontThickness);
            for(int i = 0; i < facesResult.Count; i++ ) {
                color = genderClassifier.ColorChooser(predictions[i]);
                img.Draw(facesResult[i], color, 2);
                img.Draw(predictions[i].Distance.ToString(), facesResult[i].Location, FontFace.HersheyDuplex, fontSize, color, fontThickness);
            }
            return img;
        }
        public static ColorImage ColorFrameReady(ColorImageFrame colorFrame, GenderClassifier genderClassifier, Rectangle areaOfInterest) {

            ColorImage img = ImageTools.GetColorImage(colorFrame);
            List<Rectangle> facesResult = genderClassifier.GetFaces(img, areaOfInterest);
            if( facesResult.Count == 0) {
                return img;
            }
            PredictionResult[] predictions;
            try {
                predictions = genderClassifier.ClassifyByFaces(img, areaOfInterest, facesResult.ToArray());
            }
            catch {
                return img;
                //Para cualquier error ignoramos el frame.
            }
            for( int i = 0; i < facesResult.Count; i++ ) {
                Bgr color = genderClassifier.ColorChooser(predictions[i]);
                img.Draw(facesResult[i], color, 2);
                img.Draw(predictions[i].Distance.ToString(), facesResult[i].Location, FontFace.HersheyDuplex, fontSize, color, fontThickness);
                Console.WriteLine(string.Format("Location: {0} Distance: {1} Label: {2}", facesResult[i].Location, predictions[i].Distance, predictions[i].Label));
            }
            return img;
        }
    }
}
