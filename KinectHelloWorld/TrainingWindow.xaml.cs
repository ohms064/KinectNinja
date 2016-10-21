using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KinectHelloWorld.SupportClasses;
using Microsoft.Win32;
using ShaniSoft.Drawing;
using System.Drawing;
using System.Drawing.Imaging;
using Emgu.CV.Face;
using System.IO;
using GrayImage = Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>;
using ColorImage = Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte>;
using Emgu.CV;

namespace KinectHelloWorld {
    /// <summary>
    /// Interaction logic for TraningWindow.xaml.
    /// </summary>
    public partial class TrainingWindow : Window {

        private List<TrainingData> listdata;
        private TrainingData currentData;
        private string[] paths;
        private int pathIndex;
        private CascadeClassifier classifier;
        private const string IMAGES_PATH = "TrainingData\\data.xml";
        public const string TRAINING_PATH = "TrainingData\\training_faces.xml";
        private const int MAX_RANGE = 200;

        public TrainingWindow() {
            InitializeComponent();
            listdata = TrainingData.Deserialize(IMAGES_PATH);
            currentData = new TrainingData();
            Photo.Stretch = Stretch.Fill;
            pathIndex = 0;
            classifier = new CascadeClassifier("Classifiers\\haarcascade_frontalface_alt2.xml");
        }

        private void SaveData(object sender, RoutedEventArgs e) {
            if( currentData.filePath == "" || currentData.filePath == null)
                return;
            if(SetAll.IsChecked == true && pathIndex == 0) {
                int parsedLabel = int.Parse(LabelValue.Text);
                foreach(string path in paths ) {
                    currentData = new TrainingData { label = parsedLabel, filePath = path };
                    listdata.Add(currentData);
                }
                pathIndex = paths.Length - 1;
                LabelCurrent.Text = string.Format("{0}/{1}", pathIndex + 1, paths.Length);
            }
            else {
                currentData.label = int.Parse(LabelValue.Text);
                listdata.Add(currentData);
                currentData = new TrainingData();
                pathIndex++;
                if( pathIndex < paths.Length ) {
                    LabelCurrent.Text = string.Format("{0}/{1}", pathIndex + 1, paths.Length);
                    SetPathToWindow(paths[pathIndex]);
                }
            }
            Photo.Source = null;
            TrainingData.Serialize(IMAGES_PATH, listdata);
        }

        private void OpenFile(object sender, RoutedEventArgs e) {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = "*.pgm";
            dlg.Filter = "PGM Files (*.pgm)|*.pgm|JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif";
            dlg.Multiselect = true;
            bool? result = dlg.ShowDialog();
            if( result == true ) {
                paths = dlg.FileNames;
                pathIndex = 0;
                LabelCurrent.Text = string.Format("{0}/{1}", pathIndex + 1, paths.Length);
                SetPathToWindow(paths[pathIndex]);
            }
        }

        private void SetPathToWindow(string path) {
            FilePathValue.Text = path;
            currentData.filePath = path;
            if( currentData.filePath.EndsWith("pgm") ) {
                Bitmap bmp = PNM.ReadPNM(currentData.filePath) as Bitmap;
                ColorImage imgDetec = new ColorImage(bmp);
                Rectangle[] faces = classifier.DetectMultiScale(imgDetec, 1.4, 4, new System.Drawing.Size(100, 100), new System.Drawing.Size(800, 800));
                foreach(Rectangle face in faces ) {
                    imgDetec.Draw(face, new Emgu.CV.Structure.Bgr(System.Drawing.Color.AliceBlue), 1);
                }
                TBFaces.Text = string.Format("Faces: {0}", faces.Length);
                bmp = imgDetec.ToBitmap();
                Photo.Source = bmp.ToBitmapSource();
                TBDimen.Text = string.Format("W: {0} H: {1}", bmp.Width, bmp.Height);
            }
            else {
                BitmapImage bi3 = new BitmapImage();
                bi3.BeginInit();
                bi3.UriSource = new Uri(currentData.filePath, UriKind.Absolute);
                bi3.EndInit();
                ColorImage imgDetec = new ColorImage(bi3.ToBitmap());
                Rectangle[] faces = classifier.DetectMultiScale(imgDetec, 1.4, 4, new System.Drawing.Size(100, 100), new System.Drawing.Size(800, 800));
                foreach( Rectangle face in faces ) {
                    imgDetec.Draw(face, new Emgu.CV.Structure.Bgr(System.Drawing.Color.AliceBlue), 1);
                }
                TBFaces.Text = string.Format("Faces: {0}", faces.Length);
                bi3 = imgDetec.ToBitmap().ToBitmapImage();
                Photo.Source = bi3;
                TBDimen.Text = string.Format("W: {0} H: {1}", (int) bi3.Width, (int) bi3.Height);
            }
        }

        private void BNext_Click(object sender, RoutedEventArgs e) {
            if( paths != null && pathIndex < paths.Length - 1 ) {
                pathIndex++;
                SetPathToWindow(paths[pathIndex]);
                LabelCurrent.Text = string.Format("{0}/{1}", pathIndex + 1, paths.Length);
            }
        }


        private void BTrain_Click(object sender, RoutedEventArgs e) {
            FaceRecognizer fr = new FisherFaceRecognizer(0, 3500);
            if( File.Exists(TRAINING_PATH) ){
                fr.Load(TRAINING_PATH);
            }
            List<GrayImage> trainingImgs = new List<GrayImage>();
            List<int> labels = new List<int>();
            pathIndex = 0;
            Bitmap bmp;
            while( pathIndex < listdata.Count ) {
                for( int i = 0; i < MAX_RANGE && pathIndex < listdata.Count; i++ ) {
                    if( listdata[pathIndex].filePath.EndsWith("pgm") ) {
                        bmp = PNM.ReadPNM(listdata[pathIndex].filePath) as Bitmap;
                    }
                    else {
                        bmp = new Bitmap(listdata[pathIndex].filePath);
                    }
                    trainingImgs.Add( new GrayImage(bmp));
                    labels.Add(listdata[pathIndex].label);
                    pathIndex++;
                }
                fr.Train(trainingImgs.ToArray(), labels.ToArray());
            }
            fr.Save(TRAINING_PATH);
        }
    }
}
