using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.CV.UI;
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
    class GenderClassifier {
        public CascadeClassifier classifier;
        public FaceRecognizer faceRecognizer;
        public int recognizerWidth, recognizerHeight;
        public double threshold;

        public ImageViewer viewer;

        public Bgr ColorChooser(PredictionResult pr) {
            if(pr.Distance > threshold ) {
                return new Bgr(Color.Black);
            }
            switch( (GenderEnum) pr.Label ) {
                case GenderEnum.MALE:
                    return new Bgr(Color.Blue);
                case GenderEnum.FEMALE:
                    return new Bgr(Color.Crimson);
                default:
                    return new Bgr(Color.Black);
            }
        }

        public List<Rectangle> GetFaces(ColorImage img, Rectangle areaOfInterest) {
            GrayImage grayScaleImage = img.Convert<Gray, byte>().Crop(areaOfInterest);
            grayScaleImage._EqualizeHist();

            viewer.Image = grayScaleImage;

            Rectangle[] faces = classifier.DetectMultiScale(grayScaleImage, 1.4, 4, new Size(50, 50), new Size(400, 400));
            List<PredictionResult> listPredict = new List<PredictionResult>();
            for( int i = 0; i < faces.Length; i++ ) {
                faces[i].X += areaOfInterest.X;
                faces[i].Y += areaOfInterest.Y;
                grayScaleImage.ROI = faces[i];
            }
            return new List<Rectangle>(faces);
        }

        public PredictionResult[] ClassifyByFaces(ColorImage img, Rectangle areaOfInterest, Rectangle[] faces) {
            GrayImage grayScaleImage = img.Convert<Gray, byte>().Crop(areaOfInterest);
            List<PredictionResult> listPredict = new List<PredictionResult>();
            for( int i = 0; i < faces.Length; i++ ) {
                faces[i].X -= areaOfInterest.X;
                faces[i].Y -= areaOfInterest.Y;
                GrayImage cropped = grayScaleImage.Crop(faces[i]);
                cropped = cropped.Resize(recognizerWidth, recognizerHeight, Emgu.CV.CvEnum.Inter.Linear);
                try {
                    listPredict.Add(Predict(cropped));
                }
                catch( Exception e ) {
                    throw e;
                }
            }
            return listPredict.ToArray();
        }

        public PredictionResult PredictFace(ColorImage img, Rectangle face){
            GrayImage grayScaleImage = img.Convert<Gray, byte>().Crop(face);
            try {
                return Predict(grayScaleImage);
            }
            catch( Exception e ) {
                throw e;
            }
        }

        public Rectangle GetFaceByJoint(ColorImage img, KinectSensor sensor, Joint head) {
            GrayImage grayScaleImage = img.Convert<Gray, byte>();
            ColorImagePoint headPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(head.Position, ColorImageFormat.RgbResolution640x480Fps30);

            return new Rectangle(
                (int) ( headPoint.X - ( recognizerWidth * 0.5f ) ),
                (int) ( headPoint.Y - ( recognizerHeight * 0.5f ) ),
                recognizerWidth,
                recognizerHeight);
        }

        private PredictionResult Predict(GrayImage grayScaleImage) {
            grayScaleImage._EqualizeHist();
            try {
                return faceRecognizer.Predict(grayScaleImage);
            }
            catch( Exception e ) {
                throw e;
            }
        }

    }
}
