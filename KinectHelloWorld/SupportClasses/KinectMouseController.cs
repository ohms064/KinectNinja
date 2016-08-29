using Coding4Fun.Kinect.Wpf;
using KinectCursorController;
using Microsoft.Kinect;
using System.Windows;

namespace KinectHelloWorld.SupportClasses {
    class KinectMouseController {
        private Joint activeHand;
        private const float SKELETON_MAX_X = 0.60f;
        private const float SKELETON_MAX_Y = 0.40f;
        private const int MOUSE_SPEED_X = 15, MOUSE_SPEED_Y = 10;

        public KinectMouseController() {

        }

        /// <summary>
        /// Función que controla el mouse con un Joint que recibe de parámetro. Se recomiendan las manos
        /// pero funciona con cualquier Joint.
        /// </summary>
        /// <param name="activeHand">El joint que controla el Mouse.</param>
        public Vector2 Move(ref Joint activeHand, bool isClick) {
            if(this.activeHand != activeHand ) {
                this.activeHand = activeHand;
            }
            Joint scaledHand = this.activeHand.ScaleTo((int) SystemParameters.PrimaryScreenWidth, (int) SystemParameters.PrimaryScreenHeight, SKELETON_MAX_X, SKELETON_MAX_Y);

            int cursorX = (int) scaledHand.Position.X + MOUSE_SPEED_X;
            int cursorY = (int) scaledHand.Position.Y + MOUSE_SPEED_Y;

            Vector2 cursorVector = new Vector2(cursorX, cursorY);
            NativeMethods.SendMouseInput(cursorVector.x, cursorVector.y, (int) SystemParameters.PrimaryScreenWidth, (int) SystemParameters.PrimaryScreenHeight, isClick);
            return cursorVector;

            
        }

        
    }
}
