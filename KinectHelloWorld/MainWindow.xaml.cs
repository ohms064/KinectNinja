//#define ON_TOP //For debug only, prevents MainWindow from hiding.
//#define TRAINING //Opens TraningWindow
#define MOUSE_CONTROL //Makes use of the Kinect to control the mouse
#define VIEW_CAMERA //Make use of the camera input
//#define SHOW_CAMERA //Show the camera Input

//Because of the defines I don't recommend deleting any using.
using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using KinectHelloWorld.SupportClasses;
using KinectHelloWorld.SupportClasses.Excel;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.Controls;
using Microsoft.Kinect.Toolkit.Interaction;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using ColorImage = Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte>;
using GrayImage = Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>;

namespace KinectHelloWorld {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        public static bool isTakingPhoto = false;
        public static int recognitionWidth = 120, recognitionHeight = 120; //Size of the training of the photos.
        private const int CROPPED_WIDTH = 500, CROPPED_HEIGHT = 400, CROPPED_X = 30, CROPPED_Y = 50; //The definition for the areaOfInterest.
        public static Rectangle areaOfInterest; //The area where faces will be detected
        private const string CLASSIFIER_PATH = "Classifiers\\haarcascade_frontalface_alt2.xml"; //The XML file which defines how to detect faces.

        private KinectSensor sensor;
#if MOUSE_CONTROL
        private InteractionStream _interactionStream;
#endif
        private UserInfo[] _userInfos;
        private GenderClassifier _genderClassifier;
        private bool[] isClicks;
        private Skeleton activeSkeleton = null;
        private KinectMouseController mouseController;
        private MasterKiwiServerSocket connection;
        private Thread socketThread;
#if SHOW_CAMERA
        private ImageViewer imgViewer;
#endif
        private Dictionary<int, InteractionHandEventType> _lastLeftHandEvents = new Dictionary<int, InteractionHandEventType>();
        private Dictionary<int, InteractionHandEventType> _lastRightHandEvents = new Dictionary<int, InteractionHandEventType>();

        public MainWindow() {
            InitializeComponent();
            Loaded += MainWindowLoaded;

            Closing += MainWindowClosing;

            ExcelManager.CreateSingleton(ConfigurationManager.AppSettings["DemographicsLocation"]);

            Configuration confg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if( confg.AppSettings.Settings["Width"] == null || !int.TryParse(confg.AppSettings.Settings["Width"].Value, out recognitionWidth) ) {
                recognitionWidth = 120;
                confg.AppSettings.Settings.Add(new KeyValueConfigurationElement("Width", recognitionWidth.ToString()));
                confg.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            if( confg.AppSettings.Settings["Height"] == null || !int.TryParse(confg.AppSettings.Settings["Height"].Value, out recognitionHeight) ) {
                recognitionHeight = 120;
                confg.AppSettings.Settings.Add(new KeyValueConfigurationElement("Height", recognitionHeight.ToString()));
                confg.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            if( confg.AppSettings.Settings["DemographicsLocation"] == null ) {
                string path = @"..\demo.xlsx";
                ExcelManager.CreateSingleton(path);
                confg.AppSettings.Settings.Add(new KeyValueConfigurationElement("DemographicsLocation", path.ToString()));
                confg.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }else {
                ExcelManager.CreateSingleton(ConfigurationManager.AppSettings["DemographicsLocation"]);
            }

#if ON_TOP
            StatusValue.Text = "Debugging!";
            Activated += MainWindowActive;
            Deactivated += MainWindowHidden;
#else
            StatusValue.Text = "Release";
#endif
            mouseController = new KinectMouseController();

            FaceRecognizer fr = new EigenFaceRecognizer(14, 123);
            fr.Load(TrainingWindow.TRAINING_PATH);

#if SHOW_CAMERA
            imgViewer = new ImageViewer();
            imgViewer.Show();
#endif
            GenderClassifier.threshold = 50D;
            _genderClassifier = new GenderClassifier {
                classifier = new CascadeClassifier(CLASSIFIER_PATH),
                faceRecognizer = fr,
                recognizerWidth = recognitionWidth,
                recognizerHeight = recognitionHeight
            };
            
            areaOfInterest = new Rectangle(CROPPED_X, CROPPED_Y, CROPPED_WIDTH, CROPPED_HEIGHT);
        }

        private void MainWindowLoaded(object sender, RoutedEventArgs e) {
#if TRAINING
            TrainingWindow training = new TrainingWindow();
            training.Show();

            this.Close();
#else

            KinectSensorChooser kinectSensorChooser = new KinectSensorChooser();
            kinectSensorChooser.KinectChanged += KinectSensorChooserKinectChanged;
            kinectChooser.KinectSensorChooser = kinectSensorChooser;
            kinectSensorChooser.Start();

            _userInfos = new UserInfo[InteractionFrame.UserInfoArrayLength];
            isClicks = new bool[Enum.GetValues(typeof(HandType)).Length];

            connection = new MasterKiwiServerSocket(ActivateColorFrame);
            socketThread = new Thread(connection.StartListening);
            socketThread.IsBackground = true;
            socketThread.Start();
#endif
        }

        private void MainWindowClosing(object sender, CancelEventArgs args) {
            connection.isActive = false;
            ExcelManager.instance.Save();
            ExcelManager.instance.Close();
        }

#if ON_TOP
        private void MainWindowActive(object sender, EventArgs e) {
            this.Topmost = true;
        }

        private void MainWindowHidden(object sender, EventArgs e) {
            this.Topmost = true;
            Activate();
        }
#endif

        /// <summary>
        /// Función para inicializar el kinect, se ejecuta en el evento de que un Kinect se contecte o se inicie.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args">Objeto que tiene la referencia a los sensores (Kinect).</param>
        private void KinectSensorChooserKinectChanged(object sender, KinectChangedEventArgs args) {
            if( args.OldSensor != null ) {
                //Si se conecta un nuevo kinect o, al parecer, si se reinicia el kinect desactivamos el sensor 
                //que estaba anteriormente.
                try {
                    args.OldSensor.DepthStream.Range = DepthRange.Default;
                    args.OldSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    args.OldSensor.DepthStream.Disable();
                    args.OldSensor.SkeletonStream.Disable();
                }
                catch( InvalidOperationException ) {
                    //Ignoramos los errores
                }
            }

            if( args.NewSensor != null ) {
                try {
                    args.NewSensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                    args.NewSensor.SkeletonStream.Enable();

                    try {
                        args.NewSensor.DepthStream.Range = DepthRange.Near;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = true;
                        args.NewSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                        StatusValue.Text = "NearMode";

                    }
                    catch( InvalidOperationException ) {
                        // Si el Kinect no es compatible con "Near Mode" lo reestablecemos al default.
                        args.NewSensor.DepthStream.Range = DepthRange.Default;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = false;
                        StatusValue.Text = "DefaultMode";
                    }

                    sensor = args.NewSensor;

                    //Establece el suavizado del movimiento de los Joints.
                    TransformSmoothParameters smoothingParam = new TransformSmoothParameters();
                    {
                        smoothingParam.Smoothing = 0.5f;
                        smoothingParam.Correction = 0.1f;
                        smoothingParam.Prediction = 0.5f;
                        smoothingParam.JitterRadius = 0.1f;
                        smoothingParam.MaxDeviationRadius = 0.1f;
                    };
                    sensor.SkeletonStream.Enable(smoothingParam);
                    sensor.SkeletonFrameReady += KinectSkeletonFrameReady;
#if MOUSE_CONTROL
                    _interactionStream = new InteractionStream(sensor, new InteractionClient());
                    _interactionStream.InteractionFrameReady += KinectInteractionFrameReady;
                    sensor.DepthFrameReady += KinectDepthFrameReady;
#endif

#if VIEW_CAMERA
                    sensor.ColorStream.Enable();
                    sensor.ColorFrameReady += KinectColorFrameReady;
#endif

                    StatusValue.Text += " Connected";
                    SliderMotorAngle.Value = sensor.ElevationAngle;
                    KinectAngle.Text = string.Format("Ángulo: {0}", sensor.ElevationAngle.ToString());

                }
                catch( InvalidOperationException ) {
                    StatusValue.Text = "Error";
                    //Ignoramos los errores
                }
            }

        }

        #region KinectFramesReady
        private void KinectSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs args) {
            Skeleton[] skeletons = new Skeleton[0];
            using( SkeletonFrame skeletonFrame = args.OpenSkeletonFrame() ) {
                if( skeletonFrame != null ) {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    try {
                        skeletonFrame.CopySkeletonDataTo(skeletons);
#if MOUSE_CONTROL
                        Vector4 accelReading = sensor.AccelerometerGetCurrentReading();
                        _interactionStream.ProcessSkeleton(skeletons, accelReading, skeletonFrame.Timestamp);
#endif
                    }
                    catch( InvalidOperationException ) {
                        //Ignoramos el frame
                        activeSkeleton = null;
                        return;
                    }
                }
            }

            if( skeletons.Length == 0 ) {
                activeSkeleton = null;
                return;
            }

            //Retorna el primer jugador que tenga tracking del Kinect.
            Skeleton[] trackedSkeletons = ( from s in skeletons where s.TrackingState == SkeletonTrackingState.Tracked select s ).ToArray();
            if( trackedSkeletons.Length == 0 ) {
                activeSkeleton = null;
                return;
            }

            //Para este punto ya tenemos por lo menos un candidato para activeSkeleton
            activeSkeleton = trackedSkeletons[0];
            if( trackedSkeletons.Length > 1 ) {
                for( int i = 1; i < trackedSkeletons.Length; i++ ) {
                    if( KinectDistanceTools.FirstSkeletonIsCloserToSensor(ref trackedSkeletons[i], ref activeSkeleton, JointType.HipCenter) ) {
                        activeSkeleton = trackedSkeletons[i];
                    }
                }
            }

#if MOUSE_CONTROL
            /*
             * El sistema de referencia que ocupa el kinect tiene como origen la posición
             * del sensor, es decir que eje Z apunta al frente del kinect, el eje X el positivo estád
             * a la derecha del kinect y el eje Y 
             * */

            //A partir de ahora ya sabemos que hay un jugador detectado por el Kinect y podemos obtener alguna
            //parte del cuerpo de referencia.
            Joint rightHand = activeSkeleton.Joints[JointType.WristRight];
            Joint leftHand = activeSkeleton.Joints[JointType.WristLeft];

            XValueRight.Text = rightHand.Position.X.ToString("F", CultureInfo.InvariantCulture);
            YValueRight.Text = rightHand.Position.Y.ToString("F", CultureInfo.InvariantCulture);
            ZValueRight.Text = rightHand.Position.Z.ToString("F", CultureInfo.InvariantCulture);

            XValueLeft.Text = leftHand.Position.X.ToString("F", CultureInfo.InvariantCulture);
            YValueLeft.Text = leftHand.Position.Y.ToString("F", CultureInfo.InvariantCulture);
            ZValueLeft.Text = leftHand.Position.Z.ToString("F", CultureInfo.InvariantCulture);

            //Queremos la mano más cercana al sensor para que controle el mouse.
            if( KinectDistanceTools.FirstIsCloserToSensor(ref rightHand, ref leftHand) ) {
                RightRaised.Text = "Activada";
                LeftRaised.Text = "Desactivado";
                Vector2 result = mouseController.Move(ref rightHand, isClicks[(int) HandType.Right]);
                MousePos.Text = string.Format("X: {0}, Y: {1}, Click: {2}", result.x, result.y, isClicks[(int) HandType.Right] ? "Sí" : "No");
            }
            else {
                LeftRaised.Text = "Activada";
                RightRaised.Text = "Desactivado";
                Vector2 result = mouseController.Move(ref leftHand, isClicks[(int) HandType.Left]);
                MousePos.Text = string.Format("X: {0}, Y: {1}, Click: {2}", result.x, result.y, isClicks[(int) HandType.Left] ? "Sí" : "No");
            }
#endif
        }

#if MOUSE_CONTROL
        private void KinectInteractionFrameReady(object sender, InteractionFrameReadyEventArgs args) {
            if( activeSkeleton == null ) {
                return;
            }
            using( var iaf = args.OpenInteractionFrame() ) { //dispose as soon as possible
                if( iaf == null )
                    return;
                iaf.CopyInteractionDataTo(_userInfos);
            }

            UserInfo userInfo = ( from u in _userInfos where u.SkeletonTrackingId == activeSkeleton.TrackingId select u ).FirstOrDefault();

            if( userInfo == null ) {
                return;
            }

            int userID = userInfo.SkeletonTrackingId;

            var hands = userInfo.HandPointers;

            if( hands.Count != 0 ) {
                foreach( var hand in hands ) {
                    bool grip = hand.HandEventType == InteractionHandEventType.Grip;
                    bool gripRelease = hand.HandEventType == InteractionHandEventType.GripRelease;
                    mouseController.AnalyzeGrip(grip, gripRelease, ref isClicks[(int) hand.HandType]);
                }
            }
        }

        private void KinectDepthFrameReady(object sender, DepthImageFrameReadyEventArgs args) {

            using( DepthImageFrame depthFrame = args.OpenDepthImageFrame() ) {
                if(depthFrame == null ) {
                    return;
                }
                FramesReady.DepthFrameReady(depthFrame, ref _interactionStream);
            }

    }
#endif

#if VIEW_CAMERA
        private void KinectColorFrameReady(object sender, ColorImageFrameReadyEventArgs args) {
            if( !isTakingPhoto ) {
                return;
            }
            using( ColorImageFrame colorFrame = args.OpenColorImageFrame() ) {
                if( colorFrame == null ) {
                    return;
                }
                ColorImage img;
                if( activeSkeleton == null ) {
                    img = FramesReady.ColorFrameReady(colorFrame, _genderClassifier, areaOfInterest);
                    TBPlayerStatus.Text = "Tracking Player: false";
                }
                else {
                    img = FramesReady.ColorFrameReady(colorFrame, _genderClassifier, areaOfInterest, sensor, activeSkeleton.Joints[JointType.Head]);
                    TBPlayerStatus.Text = "Tracking Player: true";
                }
                ExcelManager.instance.AddOrUpdate(DateTime.Now.GetDate(), ExcelRow.instance);
#if SHOW_CAMERA
                GrayImage grayImg = img.Convert<Gray, byte>();
                grayImg.Processing();
                imgViewer.Image = img;
#endif
            }
            isTakingPhoto = false;
        }
#endif

        #endregion

        #region CALLBACK
        public void ActivateColorFrame(string option) {
            string[] status = option.Split('.');
            switch( status[0] ) {
                case MasterKiwiServerSocket.MOVE_KINECT:
                    sensor.ElevationAngle = -sensor.ElevationAngle;
                    break;
                case MasterKiwiServerSocket.TAKE_PHOTO:
                    string won = status[1];
                    string productGiven = status[2];
                    ExcelRow newData;
                    if(won.Equals("1") ) {
                        newData = new ExcelRow {
                            wins = "1",
                            productsGiven = productGiven,
                            appStart = ExcelRow.instance == null ? "" : ExcelRow.instance.appStart,
                        };
                    }
                    else {
                        newData = new ExcelRow {
                            losses = "1",
                            productsGiven = productGiven,
                            appStart = ExcelRow.instance == null ? "" : ExcelRow.instance.appStart,
                        };
                    }
                    
                    ExcelRow.instance = newData;
                    isTakingPhoto = true;
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region WINDOWS_CONTROLLERS
        private bool isDragging = false;
        private void SliderMotorAngle_DragCompleted(object sender, RoutedEventArgs e) {
            try {
                sensor.ElevationAngle = (int) SliderMotorAngle.Value;
                KinectAngle.Text = string.Format("Ángulo: {0}", sensor.ElevationAngle.ToString());
            }
            catch( InvalidOperationException except ) {
                SliderMotorAngle.Value = sensor.ElevationAngle;
            }
            finally {
                isDragging = false;
            }
        }

        private void SliderMotorAngle_DragStarted(object sender, RoutedEventArgs e) {
            isDragging = true;
        }

        private void SliderMotorAngle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if( !isDragging ) {
                try {
                    sensor.ElevationAngle = (int) ( (Slider) sender ).Value;
                    KinectAngle.Text = string.Format("Ángulo: {0}", sensor.ElevationAngle.ToString());
                }
                catch( InvalidOperationException ) {
                    SliderMotorAngle.Value = sensor.ElevationAngle;
                }
            }
        }
        #endregion
    }
}
