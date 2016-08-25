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

namespace KinectHelloWorld
{    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const float RAISED_THRESHOLD = 0.35f;
        private const float SKELETON_MAX_X = 0.60f;
        private const float SKELETON_MAX_Y = 0.40f;
        private KinectSensor sensor;

        [DllImport("msvcrt.dll")]
        static extern bool system(string str);

        public MainWindow(){
            InitializeComponent();
            Loaded += MainWindowLoaded;
            //Process.Start("C:\\Users\\sferea\\Documents\\visual studio 2015\\Projects\\KinectHelloWorld\\KinectHelloWorld\\MasterNinjaKiwi\\MasterKiwi.exe");
            //system("MasterKiwi.exe");
            //system("pause");
            
                // This code assumes the process you are starting will terminate itself.
                // Given that is is started without a window so you cannot terminate it
                // on the desktop, it must terminate itself or you can do it programmatically
                // from this application using the Kill method.
        }

        private void MainWindowLoaded(object sender, RoutedEventArgs e){

            KinectSensorChooser kinectSensorChooser = new KinectSensorChooser();

            kinectSensorChooser.KinectChanged += KinectSensorChooserKinectChanged; 
            //Entonces la función es KinectSensorChooser.KinectChanged(object sender, KinectChangedEventARgs e)

            kinectChooser.KinectSensorChooser = kinectSensorChooser;

            kinectSensorChooser.Start();

        }

        private void KinectSensorChooserKinectChanged(object sender, KinectChangedEventArgs e) {

            if( sensor != null )
                sensor.SkeletonFrameReady -= KinectSkeletonFrameReady;

            sensor = e.NewSensor;

            if( sensor == null )
                return;

            switch( Convert.ToString(e.NewSensor.Status) ) {

                case "Connected":
                    KinectStatus.Content = "Connected";
                    break;

                case "Disconnected":
                    KinectStatus.Content = "Disconnected";
                    break;

                case "Error":
                    KinectStatus.Content = "Error";
                    break;

                case "NotReady":
                    KinectStatus.Content = "Not Ready";
                    break;

                case "NotPowered":
                    KinectStatus.Content = "Not Powered";
                    break;

                case "Initializing":
                    KinectStatus.Content = "Initialising";
                    break;

                default:
                    KinectStatus.Content = "Undefined";
                    break;

            }

            sensor.SkeletonStream.Enable();
            sensor.SkeletonFrameReady += KinectSkeletonFrameReady;

        }

        private void KinectSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e) {

            var skeletons = new Skeleton[0];

            using( var skeletonFrame = e.OpenSkeletonFrame() ) {

                if( skeletonFrame != null ) {

                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);

                }

            }

            if( skeletons.Length == 0 ) { return; }

            var skel = skeletons.FirstOrDefault(x => x.TrackingState == SkeletonTrackingState.Tracked);

            if( skel == null ) { return; }

            /*
             * El sistema de referencia que ocupa el kinect tiene como origen la posición
             * del sensor, es decir que eje Z apunta al frente del kinect, el eje X el positivo estád
             * a la derecha del kinect y el eje Y 
             * */

            var rightHand = skel.Joints[JointType.HandRight];
            XValueRight.Text = rightHand.Position.X.ToString(CultureInfo.InvariantCulture);
            YValueRight.Text = rightHand.Position.Y.ToString(CultureInfo.InvariantCulture);
            ZValueRight.Text = rightHand.Position.Z.ToString(CultureInfo.InvariantCulture);

            var leftHand = skel.Joints[JointType.HandLeft];
            XValueLeft.Text = leftHand.Position.X.ToString(CultureInfo.InvariantCulture);
            YValueLeft.Text = leftHand.Position.Y.ToString(CultureInfo.InvariantCulture);
            ZValueLeft.Text = leftHand.Position.Z.ToString(CultureInfo.InvariantCulture);

            var centreHip = skel.Joints[JointType.HipCenter];

            if( centreHip.Position.Z - rightHand.Position.Z > RAISED_THRESHOLD ) {

                RightRaised.Text = "Raised";

            }

            else if( centreHip.Position.Z - leftHand.Position.Z > RAISED_THRESHOLD ) {

                LeftRaised.Text = "Raised";
            
            }

            else {

                LeftRaised.Text = "Lowered";
                RightRaised.Text = "Lowered";

            }

            

            var scaledRightHand = rightHand.ScaleTo((int) SystemParameters.PrimaryScreenWidth, (int) SystemParameters.PrimaryScreenHeight, SKELETON_MAX_X, SKELETON_MAX_Y);

            var cursorX = (int) scaledRightHand.Position.X;
            var cursorY = (int) scaledRightHand.Position.Y;

            //NativeMethods es una clase obtenida de 
            NativeMethods.SendMouseInput(cursorX, cursorY, (int) SystemParameters.PrimaryScreenWidth, (int) SystemParameters.PrimaryScreenHeight, false);

        }

    }
}
