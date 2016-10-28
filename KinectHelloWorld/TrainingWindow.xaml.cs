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
using System.Configuration;

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
        List<Rectangle> faces;
        int male = 0, female = 0;

        public TrainingWindow() {
            InitializeComponent();
            BDelete.IsEnabled = false;
            BUpdate.IsEnabled = false;
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
            FaceRecognizer fr = new EigenFaceRecognizer(16, 123);
            if( File.Exists(TRAINING_PATH) ) {
                fr.Load(TRAINING_PATH);
            }
            _genderClassifier = new GenderClassifier {
                classifier = new CascadeClassifier("Classifiers\\haarcascade_frontalface_alt2.xml"),
                faceRecognizer = fr,
                recognizerHeight = MainWindow.height,
                recognizerWidth = MainWindow.width,
                threshold = 100
            };            
        }

        private void SaveData(object sender, RoutedEventArgs e) {
            if( currentData.filePath == "" || currentData.filePath == null)
                return;
            if(CBAllSet.IsChecked == true && pathIndex == 0 && !isEditingXML) {
                GenderEnum parsedLabel = (GenderEnum)CBGender.SelectedItem;
                foreach(TrainingData path in paths ) {
                    currentData = new TrainingData { label = parsedLabel, filePath = path.filePath };
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
                pathIndex = paths.Length - 1;
                LabelCurrent.Text = string.Format("{0}/{1}", pathIndex + 1, paths.Length);
                Photo.Source = null;
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
                }else {
                    Photo.Source = null;
                }
            }
            TrainingData.Serialize(IMAGES_PATH, listdata);
            TBGenderCount.Text = string.Format("Male: {0} Female: {1}", male, female);
            TBStatus.Text = "Nuevo dato(s)!";
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
                BUpdate.IsEnabled = false;
            }
        }

        private void SetPathToWindow(TrainingData path) {
            FilePathValue.Text = path.filePath.Substring(path.filePath.LastIndexOf('\\') + 1);
            currentData.filePath = path.filePath;
            CBGender.SelectedValue = path.label;
            Bitmap source;
            if( currentData.filePath.EndsWith("pgm") ) {
                Bitmap bmp = PNM.ReadPNM(currentData.filePath) as Bitmap;
                ColorImage imgDetec = new ColorImage(bmp);
                faces = _genderClassifier.GetFaces(imgDetec);
                foreach(Rectangle face in faces ) {
                    imgDetec.Draw(face, new Emgu.CV.Structure.Bgr(System.Drawing.Color.AliceBlue), 1);
                }
                TBFaces.Text = string.Format("Faces: {0}", faces.Count);
                source = imgDetec.ToBitmap();
            }
            else {
                BitmapImage bi3 = new BitmapImage();
                bi3.BeginInit();
                bi3.UriSource = new Uri(currentData.filePath, UriKind.Absolute);
                bi3.EndInit();
                ColorImage imgDetec = new ColorImage(bi3.ToBitmap());
                faces = _genderClassifier.GetFaces(imgDetec);
                foreach( Rectangle face in faces ) {
                    imgDetec.Draw(face, new Emgu.CV.Structure.Bgr(System.Drawing.Color.AliceBlue), 1);
                }
                TBFaces.Text = string.Format("Faces: {0}", faces.Count);
                source = imgDetec.ToBitmap();
            }
            TBDimen.Text = string.Format("W: {0} H: {1}", (int) source.Width, (int) source.Height);
            if( CBTrainer.IsChecked == true ) {
                GrayImage img = new GrayImage(source);
                img.Processing();
                source = img.ToBitmap();
            }
            Photo.Source = source.ToBitmapImage();
        }

        private void BNext_Click(object sender, RoutedEventArgs e) {
            if( paths != null && pathIndex < paths.Length - 1 ) {
                pathIndex++;
                currentData = paths[pathIndex];
                SetPathToWindow(currentData);
                LabelCurrent.Text = string.Format("{0}/{1}", pathIndex + 1, paths.Length);
            }
        }


        private void BTrain_Click(object sender, RoutedEventArgs e) {
            List<GrayImage> trainingImgs = new List<GrayImage>();
            List<int> labels = new List<int>();
            pathIndex = 0;
            Bitmap bmp = ReadBitmap(listdata[pathIndex].filePath);
            _genderClassifier.recognizerWidth = bmp.Width;
            _genderClassifier.recognizerHeight = bmp.Height;
            while( pathIndex < listdata.Count ) {
                for( int i = 1; i < MAX_RANGE && pathIndex < listdata.Count; i++ ) {
                    bmp = ReadBitmap(listdata[pathIndex].filePath);
                    if( _genderClassifier.recognizerWidth != bmp.Width || _genderClassifier.recognizerHeight != bmp.Height ) {
                        TBStatus.Text = string.Format("No se completó el entrenamiento {0}. Las imágenes no tienen el mismo tamaño", i);
                        _genderClassifier.faceRecognizer.Load(TRAINING_PATH);
                        return;
                    }
                    trainingImgs.Add( new GrayImage(bmp));
                    labels.Add((int)listdata[pathIndex].label);
                    pathIndex++;
                }
                _genderClassifier.faceRecognizer.Train(trainingImgs.ToArray(), labels.ToArray());
            }
            Configuration confg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            
            if( confg.AppSettings.Settings["Width"]  != null ) {
                confg.AppSettings.Settings["Width"].Value = _genderClassifier.recognizerWidth.ToString();
            }else {
                confg.AppSettings.Settings.Add(new KeyValueConfigurationElement("Width", _genderClassifier.recognizerWidth.ToString()));
            }

            if( confg.AppSettings.Settings["Height"] != null ) {
                confg.AppSettings.Settings["Height"].Value = _genderClassifier.recognizerHeight.ToString();
            }
            else {
                confg.AppSettings.Settings.Add(new KeyValueConfigurationElement("Height", _genderClassifier.recognizerHeight.ToString()));
            }
            
            confg.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
            _genderClassifier.faceRecognizer.Save(TRAINING_PATH);
            TBStatus.Text = string.Format("Trained! For W: {0}, H: {1}", _genderClassifier.recognizerWidth, _genderClassifier.recognizerHeight);
        }

        private Bitmap ReadBitmap(string path) {
            if( path.EndsWith("pgm") ) {
                return PNM.ReadPNM(listdata[pathIndex].filePath) as Bitmap;
            }
            else {
                return new Bitmap(listdata[pathIndex].filePath);
            }
        }

        private void BPrevious_Click(object sender, RoutedEventArgs e) {
            if( paths != null && pathIndex > 0 ) {
                pathIndex--;
                currentData = paths[pathIndex];
                SetPathToWindow(currentData);
                LabelCurrent.Text = string.Format("{0}/{1}", pathIndex + 1, paths.Length);
            }
        }

        private void BXML_Click(object sender, RoutedEventArgs e) {
            if( listdata.Count == 0 ) {
                TBStatus.Text = "No hay datos!";
                return;
            }
            paths = listdata.ToArray();
            pathIndex = 0;
            LabelCurrent.Text = string.Format("{0}/{1}", pathIndex + 1, paths.Length);
            SetPathToWindow(paths[pathIndex]);
            isEditingXML = true;
            BDelete.IsEnabled = true;
            BUpdate.IsEnabled = true;
        }

        private void BPredict_Click(object sender, RoutedEventArgs e) {
            if(currentData == null || currentData.filePath == "" || currentData.filePath == null ) {
                return;
            }
            GrayImage sample = new GrayImage(currentData.filePath);
            if(faces.Count != 0 ) {
                ImageTools.OrderRectanglesByArea(ref faces);
                ImageTools.RemoveInnerRectangles(ref faces);
                sample.ROI = faces[faces.Count - 1];
                sample = sample.Copy();
            }
            sample = sample.Resize(_genderClassifier.recognizerWidth, _genderClassifier.recognizerHeight, (Emgu.CV.CvEnum.Inter) CBInterpolation.SelectedItem);
            sample._EqualizeHist();
            Photo.Source = sample.ToBitmap().ToBitmapImage();
            FaceRecognizer.PredictionResult prediction;
            try {
                prediction = _genderClassifier.Predict(sample);
            }
            catch (Exception ex){
                TBStatus.Text = ex.Message;
                return;
            }
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

        private void BUpdate_Click(object sender, RoutedEventArgs e) {
            if( !isEditingXML )
                return;
            listdata[pathIndex].filePath = FilePathValue.Text;
            listdata[pathIndex].label = (GenderEnum) CBGender.SelectedItem;
            TrainingData.Serialize(IMAGES_PATH, listdata);
        }

        private void BDelete_Click(object sender, RoutedEventArgs e) {
            if( !isEditingXML )
                return;
            TrainingData toDelete = listdata[pathIndex];
            switch( toDelete.label ) {
                case GenderEnum.FEMALE:
                    female--;
                    break;
                case GenderEnum.MALE:
                    male--;
                    break;
            }
            listdata.RemoveAt(pathIndex);
            paths = listdata.ToArray();
            pathIndex++;
            if(pathIndex >= paths.Length ) {
                pathIndex = paths.Length - 1;
            }
            if( pathIndex >= 0 ) {
                currentData = paths[pathIndex];
                SetPathToWindow(currentData);
            }else {
                Photo.Source = null;
            }
            if(listdata.Count == 0 ) {
                BDelete.IsEnabled = false;
                BUpdate.IsEnabled = false;
            }
            TrainingData.Serialize(IMAGES_PATH, listdata);
            LabelCurrent.Text = string.Format("{0}/{1}", pathIndex + 1, paths.Length);
            TBGenderCount.Text = string.Format("Male: {0} Female: {1}", male, female);
        }
    }
}
