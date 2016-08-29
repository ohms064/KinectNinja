using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using KinectCursorController;
using Coding4Fun.Kinect.Wpf;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect.Toolkit.Interaction;
using System.ComponentModel;
using KinectHelloWorld.SupportClasses;

namespace KinectHelloWorld
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public const float RAISED_THRESHOLD = 0.35f;
        private const float SKELETON_MAX_X = 0.60f;
        private const float SKELETON_MAX_Y = 0.40f;
        private KinectSensor sensor;
        private InteractionStream _interactionStream;
        private UserInfo[] _userInfos;
        private bool isClick = false;
        private MouseController mouseController;
        private Dictionary<int, InteractionHandEventType> _lastLeftHandEvents = new Dictionary<int, InteractionHandEventType>();
        private Dictionary<int, InteractionHandEventType> _lastRightHandEvents = new Dictionary<int, InteractionHandEventType>();

        public MainWindow() {
            InitializeComponent();
            Loaded += MainWindowLoaded;
            Activated += MainWindowActive;
            Deactivated += MainWindowHidden;
            mouseController = new MouseController();
        }

        private void MainWindowLoaded(object sender, RoutedEventArgs e) {

            KinectSensorChooser kinectSensorChooser = new KinectSensorChooser();
            kinectSensorChooser.KinectChanged += KinectSensorChooserKinectChanged;
            kinectChooser.KinectSensorChooser = kinectSensorChooser;
            kinectSensorChooser.Start();

            _userInfos = new UserInfo[InteractionFrame.UserInfoArrayLength];
        }

        private void MainWindowActive(object sender, EventArgs e) {
            this.Topmost = true;
        }

        private void MainWindowHidden(object sender, EventArgs e) {
            this.Topmost = true;
            Activate();
        }

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
                    }
                    catch( InvalidOperationException ) {
                        // Si el Kinect no es compatible con "Near Mode" lo reestablecemos al default.
                        args.NewSensor.DepthStream.Range = DepthRange.Default;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    }
                    sensor = args.NewSensor;
                    sensor.SkeletonFrameReady += KinectSkeletonFrameReady;
                    sensor.DepthFrameReady += KinectDepthFrameReady;

                    _interactionStream = new InteractionStream(sensor, new InteractionClient());
                    _interactionStream.InteractionFrameReady += KinectInteractionFrameReady;

                    StatusValue.Text = "Connected";

                }
                catch( InvalidOperationException ) {
                    StatusValue.Text = "Error";
                    //Ignoramos los errores
                }
            }

        }
        #region KinectFramesReady
        private void KinectSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs args) {
            //Por Skeleton nos referiremos de ahora en adelante a un jugador.
            Skeleton[] skeletons = new Skeleton[0];
            using(SkeletonFrame skeletonFrame = args.OpenSkeletonFrame() ) {
                if( skeletonFrame != null ) {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    try {
                        skeletonFrame.CopySkeletonDataTo(skeletons);
                        Vector4 accelReading = sensor.AccelerometerGetCurrentReading();
                        _interactionStream.ProcessSkeleton(skeletons, accelReading, skeletonFrame.Timestamp);
                    }catch( InvalidOperationException ) {
                        //Ignoramos el frame
                        return;
                    }
                }
            }

            if( skeletons.Length == 0 )
                return;

            //Retorna el primer jugador que tenga tracking del Kinect.
            Skeleton firstSkeleton = skeletons.FirstOrDefault(x => x.TrackingState == SkeletonTrackingState.Tracked);

            if( firstSkeleton == null )
                return;

            /*
             * El sistema de referencia que ocupa el kinect tiene como origen la posición
             * del sensor, es decir que eje Z apunta al frente del kinect, el eje X el positivo estád
             * a la derecha del kinect y el eje Y 
             * */

            //A partir de ahora ya sabemos que hay un jugador detectado por el Kinect y podemos obtener alguna
            //parte del cuerpo de referencia.

            
            Joint hip = firstSkeleton.Joints[JointType.HipCenter];
            Joint rightHand = firstSkeleton.Joints[JointType.HandRight];
            Joint leftHand = firstSkeleton.Joints[JointType.HandLeft];

            XValueRight.Text = rightHand.Position.X.ToString(CultureInfo.InvariantCulture);
            YValueRight.Text = rightHand.Position.Y.ToString(CultureInfo.InvariantCulture);
            ZValueRight.Text = rightHand.Position.Z.ToString(CultureInfo.InvariantCulture);

            XValueLeft.Text = leftHand.Position.X.ToString(CultureInfo.InvariantCulture);
            YValueLeft.Text = leftHand.Position.Y.ToString(CultureInfo.InvariantCulture);
            ZValueLeft.Text = leftHand.Position.Z.ToString(CultureInfo.InvariantCulture);

            //Queremos el más lejano entonces el que tenga el valor más pequeño
            //será la mano que controlará el Mouse.
            if( DistanceTools.FirstIsClosest(ref hip, ref rightHand, ref leftHand )) {
                RightRaised.Text = "Activada";
                LeftRaised.Text = "Desactivado";
                Vector2 result = mouseController.Move(ref rightHand, isClick);
                MousePos.Text = string.Format("X: {0}, Y: {1}, Click: {2}", result.x, result.y, isClick ? "Sí" : "No");
            }
            else {
                LeftRaised.Text = "Activada";
                RightRaised.Text = "Desactivado";
                Vector2 result = mouseController.Move(ref leftHand, isClick);
                MousePos.Text = string.Format("X: {0}, Y: {1}, Click: {2}", result.x, result.y, isClick ? "Sí" : "No");
            }
        }

        private void KinectInteractionFrameReady(object sender, InteractionFrameReadyEventArgs args) {
            using( var iaf = args.OpenInteractionFrame() ) { //dispose as soon as possible
                if( iaf == null )
                    return;
                iaf.CopyInteractionDataTo(_userInfos);
            }

            foreach( var userInfo in _userInfos ) {
                var userID = userInfo.SkeletonTrackingId;
                if( userID == 0 )
                    continue;

                var hands = userInfo.HandPointers;


                if(hands.Count != 0) {
                    foreach( var hand in hands ) {
                        bool grip = hand.HandEventType == InteractionHandEventType.Grip;
                        bool gripRelease = hand.HandEventType == InteractionHandEventType.GripRelease;
                        AnalyzeGrip(grip, gripRelease);
                    }
                }
            
            }

        }

        private void KinectDepthFrameReady(object sender, DepthImageFrameReadyEventArgs args) {
            using( DepthImageFrame depthFrame = args.OpenDepthImageFrame() ) {
                if( depthFrame == null )
                    return;

                try {
                    _interactionStream.ProcessDepth(depthFrame.GetRawPixelData(), depthFrame.Timestamp);
                }
                catch( InvalidOperationException ) {
                    // DepthFrame functions may throw when the sensor gets
                    // into a bad state.  Ignore the frame in that case.
                }
            }
        }
        #endregion

        private void AnalyzeGrip(bool grip, bool gripRelease) {
            GripValue.Text = grip.ToString();
            ReleaseValue.Text = gripRelease.ToString();
            if( gripRelease ) {
                isClick = false;
            }
            else if( grip ) {
                isClick = true;
            }

        }

    }
}
