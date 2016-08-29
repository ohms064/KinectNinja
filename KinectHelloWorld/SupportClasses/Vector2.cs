using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectHelloWorld.SupportClasses {
    /// <summary>
    /// Clase que representa un Vector de 2 valores enteros.
    /// </summary>
    class Vector2{
        public int x;
        public int y;
    
        public Vector2(int x, int y){
            this.x = x;
            this.y = y;
        }

        public static int Dot(Vector2 first, Vector2 second) {
            int result = 0;
            result += first.x * first.x;
            result += first.y * first.y;
            return result;
        }
    }
}

