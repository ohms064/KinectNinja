using Coding4Fun.Kinect.Wpf;
using KinectCursorController;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace KinectHelloWorld.SupportClasses {
    class MouseController {
        private Joint activeHand;
        private const float SKELETON_MAX_X = 0.60f;
        private const float SKELETON_MAX_Y = 0.40f;
        private const float MAX_THRESHOLD = 100;
        public int previousMouseX = -1, previousMouseY = -1;

        public MouseController() {

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
          
            int cursorX = (int) scaledHand.Position.X;
            int cursorY = (int) scaledHand.Position.Y;

            if( previousMouseX == -1 || previousMouseY == -1 ) {
                //Si cambiamos de mano o reiniciamos el kinect ignoramos el movimiento para
                //poder calcular el que sigue.
                previousMouseX = cursorX;
                previousMouseY = cursorY;
                return new Vector2(-1, -1);
            }

            NativeMethods.SendMouseInput(cursorX, cursorY, (int) SystemParameters.PrimaryScreenWidth, (int) SystemParameters.PrimaryScreenHeight, isClick);
            return new Vector2(cursorX, cursorY);
        }

        
    }
}
