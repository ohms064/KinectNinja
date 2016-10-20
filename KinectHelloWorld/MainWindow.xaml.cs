﻿//#define ON_TOP //For debug only

using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using Microsoft.Kinect.Toolkit.Interaction;
using KinectHelloWorld.SupportClasses;
using System.Windows.Controls;


namespace KinectHelloWorld {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private KinectSensor sensor;
        private InteractionStream _interactionStream;
        private UserInfo[] _userInfos;
        private bool[] isClicks;
        private Skeleton activeSkeleton = null; 
        private KinectMouseController mouseController;
        private Dictionary<int, InteractionHandEventType> _lastLeftHandEvents = new Dictionary<int, InteractionHandEventType>();
        private Dictionary<int, InteractionHandEventType> _lastRightHandEvents = new Dictionary<int, InteractionHandEventType>();

        public MainWindow() {
            InitializeComponent();
            Loaded += MainWindowLoaded;
#if ON_TOP
            StatusValue.Text = "Debugging!";
            Activated += MainWindowActive;
            Deactivated += MainWindowHidden;
#else
            StatusValue.Text = "Release";
#endif
            mouseController = new KinectMouseController();
        }

        private void MainWindowLoaded(object sender, RoutedEventArgs e) {
            KinectSensorChooser kinectSensorChooser = new KinectSensorChooser();
            kinectSensorChooser.KinectChanged += KinectSensorChooserKinectChanged;
            kinectChooser.KinectSensorChooser = kinectSensorChooser;
            kinectSensorChooser.Start();

            _userInfos = new UserInfo[InteractionFrame.UserInfoArrayLength];
            isClicks = new bool[Enum.GetValues(typeof(HandType)).Length];
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
                    TransformSmoothParameters smoothingParam = new TransformSmoothParameters(); {
                        smoothingParam.Smoothing = 0.5f;
                        smoothingParam.Correction = 0.1f;
                        smoothingParam.Prediction = 0.5f;
                        smoothingParam.JitterRadius = 0.1f;
                        smoothingParam.MaxDeviationRadius = 0.1f;
                    };

                    sensor.SkeletonStream.Enable(smoothingParam);
                    sensor.SkeletonFrameReady += KinectSkeletonFrameReady;
                    sensor.DepthFrameReady += KinectDepthFrameReady;

                    _interactionStream = new InteractionStream(sensor, new InteractionClient());
                    _interactionStream.InteractionFrameReady += KinectInteractionFrameReady;

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
            Skeleton[] trackedSkeletons = (from s in skeletons where s.TrackingState == SkeletonTrackingState.Tracked select s).ToArray();
            if(trackedSkeletons.Length == 0 ) {
                return;
            }

            activeSkeleton = trackedSkeletons[0];
            if(trackedSkeletons.Length > 1 ) {
                for( int i = 1; i < trackedSkeletons.Length; i++ ) {
                    if(KinectDistanceTools.FirstSkeletonIsCloserToSensor(ref trackedSkeletons[i], ref activeSkeleton, JointType.HipCenter) ) {
                        activeSkeleton = trackedSkeletons[i];
                    }
                }
            }

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
            if( KinectDistanceTools.FirstIsCloserToSensor( ref rightHand, ref leftHand )) {
                RightRaised.Text = "Activada";
                LeftRaised.Text = "Desactivado";
                Vector2 result = mouseController.Move(ref rightHand,  isClicks[(int)HandType.Right]);
                MousePos.Text = string.Format("X: {0}, Y: {1}, Click: {2}", result.x, result.y, isClicks[(int) HandType.Right] ? "Sí" : "No");
            }
            else {
                LeftRaised.Text = "Activada";
                RightRaised.Text = "Desactivado";
                Vector2 result = mouseController.Move(ref leftHand, isClicks[(int) HandType.Left]);
                MousePos.Text = string.Format("X: {0}, Y: {1}, Click: {2}", result.x, result.y, isClicks[(int) HandType.Left] ? "Sí" : "No");
            }
        }

        private void KinectInteractionFrameReady(object sender, InteractionFrameReadyEventArgs args) {
            if(activeSkeleton == null ) {
                return;
            }
            using( var iaf = args.OpenInteractionFrame() ) { //dispose as soon as possible
                if( iaf == null )
                    return;
                iaf.CopyInteractionDataTo(_userInfos);
            }

            UserInfo userInfo = ( from u in _userInfos where u.SkeletonTrackingId == activeSkeleton.TrackingId select u ).FirstOrDefault();

            if(userInfo == null ) {
                return;
            }
            
            int userID = userInfo.SkeletonTrackingId;

            var hands = userInfo.HandPointers;

            if(hands.Count != 0) {
                foreach( var hand in hands ) {
                    bool grip = hand.HandEventType == InteractionHandEventType.Grip;
                    bool gripRelease = hand.HandEventType == InteractionHandEventType.GripRelease;
                    AnalyzeGrip(grip, gripRelease, ref isClicks[(int)hand.HandType]);
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

        private void AnalyzeGrip(bool grip, bool gripRelease, ref bool isClick) {
            if( gripRelease ) {
                isClick = false;
            }
            else if( grip ) {
                isClick = true;
            }
        }

        private bool isDragging = false;
        private void SliderMotorAngle_DragCompleted(object sender, RoutedEventArgs e) {
            try {
                sensor.ElevationAngle = (int) SliderMotorAngle.Value;
                KinectAngle.Text = string.Format("Ángulo: {0}", sensor.ElevationAngle.ToString());
            }catch (InvalidOperationException except){
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
                catch( InvalidOperationException except ) {
                   SliderMotorAngle.Value = sensor.ElevationAngle;
                }
            }
        }
    }
}
