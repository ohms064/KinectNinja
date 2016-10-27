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
        private TrainingData[] paths;
        private int pathIndex;
        private bool isEditingXML;
        private GenderClassifier _genderClassifier;
        private const string IMAGES_PATH = "TrainingData\\data.xml";
        public const string TRAINING_PATH = "TrainingData\\training_faces.xml";
        private const int MAX_RANGE = 200;
        int male = 0, female = 0;

        public TrainingWindow() {
            InitializeComponent();
            isEditingXML = false;
            listdata = TrainingData.Deserialize(IMAGES_PATH);
            CBInterpolation.ItemsSource = Enum.GetValues(typeof(Emgu.CV.CvEnum.Inter));
            CBInterpolation.SelectedIndex = 0;
            CBGender.ItemsSource = Enum.GetValues(typeof(GenderEnum));
            CBGender.SelectedIndex = 0;
            foreach(TrainingData data in listdata) {
                switch( data.label ) {
                    case GenderEnum.MALE:
                        male++;
                        break;
                    case GenderEnum.FEMALE:
                        female++;
                        break;
                    default:
                        break;
                }
            }
            TBGenderCount.Text = string.Format("Male: {0} Female: {1}", male, female);
            currentData = new TrainingData();
            Photo.Stretch = Stretch.Fill;
            pathIndex = 0;
            FaceRecognizer fr = new EigenFaceRecognizer();
            if( !File.Exists(TRAINING_PATH) && currentData.filePath == null && !File.Exists(currentData.filePath) ) {
                fr.Load(TRAINING_PATH);
            }
            _genderClassifier = new GenderClassifier {
                classifier = new CascadeClassifier("Classifiers\\haarcascade_frontalface_alt2.xml"),
                faceRecognizer = fr,
                recognizerHeight = MainWindow.HEIGHT,
                recognizerWidth = MainWindow.WIDTH,
                threshold = 100
            };            
        }

        private void SaveData(object sender, RoutedEventArgs e) {
            if( currentData.filePath == "" || currentData.filePath == null)
                return;
            if(SetAll.IsChecked == true && pathIndex == 0 && !isEditingXML) {
                GenderEnum parsedLabel = (GenderEnum)CBGender.SelectedItem;
                foreach(TrainingData path in paths ) {
                    currentData = new TrainingData { label = parsedLabel, filePath = path.filePath };
                    listdata.Add(currentData);
                }
                pathIndex = paths.Length - 1;
                LabelCurrent.Text = string.Format("{0}/{1}", pathIndex + 1, paths.Length);
            }
            else {
                currentData.label = (GenderEnum) CBGender.SelectedItem;
                if( isEditingXML ) {
                    listdata[pathIndex] = currentData;
                    switch( currentData.label ) {
                        case GenderEnum.FEMALE:
                            male--;
                            female++;
                            break;
                        case GenderEnum.MALE:
                            male++;
                            female--;
                            break;
                    }
                }
                else {
                    listdata.Add(currentData);
                    switch( currentData.label ) {
                        case GenderEnum.FEMALE:
                            female++;
                            break;
                        case GenderEnum.MALE:
                            male++;
                            break;
                    }
                }
                currentData = new TrainingData();
                pathIndex++;
                if( pathIndex < paths.Length ) {
                    LabelCurrent.Text = string.Format("{0}/{1}", pathIndex + 1, paths.Length);
                    SetPathToWindow(paths[pathIndex]);
                }
            }
            Photo.Source = null;
            TrainingData.Serialize(IMAGES_PATH, listdata);
            TBGenderCount.Text = string.Format("Male: {0} Female: {1}", male, female);
        }

        private void OpenFile(object sender, RoutedEventArgs e) {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = "*.pgm";
            //dlg.Filter = "PGM Files (*.pgm)|*.pgm|JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif";
            dlg.Filter = "Image Files (*.pgm, *.jpeg, *.png, *.jpg, *.gif)|*.pgm; *.jpeg; *.png; *.jpg; *.gif";
            dlg.Multiselect = true;
            bool? result = dlg.ShowDialog();
            if( result == true ) {
                paths = new TrainingData[dlg.FileNames.Length];
                for(int i = 0; i < paths.Length; i++ ) {
                    paths[i] = new TrainingData { filePath = dlg.FileNames[i], label = 0};
                }
                pathIndex = 0;
                LabelCurrent.Text = string.Format("{0}/{1}", pathIndex + 1, paths.Length);
                SetPathToWindow(paths[pathIndex]);
                isEditingXML = false;
                BDelete.IsEnabled = false;
            }
        }

        private void SetPathToWindow(TrainingData path) {
            FilePathValue.Text = path.filePath;
            currentData.filePath = path.filePath;
            CBGender.SelectedValue = path.label;
            if( currentData.filePath.EndsWith("pgm") ) {
                Bitmap bmp = PNM.ReadPNM(currentData.filePath) as Bitmap;
                ColorImage imgDetec = new ColorImage(bmp);
                List<Rectangle> faces = _genderClassifier.GetFaces(imgDetec, MainWindow.areaOfInterest);
                foreach(Rectangle face in faces ) {
                    imgDetec.Draw(face, new Emgu.CV.Structure.Bgr(System.Drawing.Color.AliceBlue), 1);
                }
                TBFaces.Text = string.Format("Faces: {0}", faces.Count);
                bmp = imgDetec.ToBitmap();
                Photo.Source = bmp .ToBitmapSource();
                TBDimen.Text = string.Format("W: {0} H: {1}", bmp.Width, bmp.Height);
            }
            else {
                BitmapImage bi3 = new BitmapImage();
                bi3.BeginInit();
                bi3.UriSource = new Uri(currentData.filePath, UriKind.Absolute);
                bi3.EndInit();
                ColorImage imgDetec = new ColorImage(bi3.ToBitmap());
                List<Rectangle> faces = _genderClassifier.GetFaces(imgDetec, MainWindow.areaOfInterest);
                foreach( Rectangle face in faces ) {
                    imgDetec.Draw(face, new Emgu.CV.Structure.Bgr(System.Drawing.Color.AliceBlue), 1);
                }
                TBFaces.Text = string.Format("Faces: {0}", faces.Count);
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
                    labels.Add((int)listdata[pathIndex].label);
                    pathIndex++;
                }
                _genderClassifier.faceRecognizer.Train(trainingImgs.ToArray(), labels.ToArray());
            }
            _genderClassifier.faceRecognizer.Save(TRAINING_PATH);
            TBStatus.Text = "Trained!";
        }

        private void BPrevious_Click(object sender, RoutedEventArgs e) {
            if( paths != null && pathIndex > 0 ) {
                pathIndex--;
                SetPathToWindow(paths[pathIndex]);
                LabelCurrent.Text = string.Format("{0}/{1}", pathIndex + 1, paths.Length);
            }
        }

        private void BXML_Click(object sender, RoutedEventArgs e) {
            paths = listdata.ToArray();
            pathIndex = 0;
            LabelCurrent.Text = string.Format("{0}/{1}", pathIndex + 1, paths.Length);
            SetPathToWindow(paths[pathIndex]);
            isEditingXML = true;
            BDelete.IsEnabled = true;
        }

        private void BPredict_Click(object sender, RoutedEventArgs e) {
            GrayImage sample = new GrayImage(currentData.filePath);
            sample = sample.Resize(MainWindow.WIDTH, MainWindow.HEIGHT, (Emgu.CV.CvEnum.Inter) CBInterpolation.SelectedItem);
            FaceRecognizer.PredictionResult prediction = _genderClassifier.faceRecognizer.Predict(sample);
            TBStatus.Text = string.Format("Res: {0}\nD: {1:0.##}", prediction.Label.ToString(), prediction.Distance);
            switch( prediction.Label ) {
                case 0:
                    TBStatus.Foreground = System.Windows.Media.Brushes.Blue;
                    break;
                case 1:
                    TBStatus.Foreground = System.Windows.Media.Brushes.Crimson;
                    break;
                default:
                    TBStatus.Foreground = System.Windows.Media.Brushes.Black;
                    break;
            }
            

        }

        private void BDelete_Click(object sender, RoutedEventArgs e) {
            if( !isEditingXML )
                return;
            listdata.RemoveAt(pathIndex);
            paths = listdata.ToArray();
            if(pathIndex >= paths.Length ) {
                pathIndex = paths.Length - 1;
            }
            currentData = paths[pathIndex];
            SetPathToWindow(currentData);
            TrainingData.Serialize(IMAGES_PATH, listdata);
        }
    }
}
