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
        public int magnitude {
            get {
                return (int) Math.Sqrt(x * x + y * y );
            }
        }
        public Vector2 normalized {
            get {
                return new Vector2(x / magnitude, y / magnitude);
            }
        }
    
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

        public static int DistanceSquared(Vector2 first, Vector2 second) {
            int firstDist = first.x - second.x;
            int secondDist = first.y - second.y;
            return ( firstDist * firstDist ) + ( secondDist * secondDist );

        }

        public Vector2 Minus(Vector2 other) {
            this.x -= other.x;
            this.y -= other.y;
            return this;
        }

        public Vector2 Mul(int other) {
            this.x *= other;
            this.y *= other;
            return this;
        }

        public static Vector2 Lerp(Vector2 origin, Vector2 destiny, float t) {
            Vector2 interpolated = destiny.Minus(origin);
            if(t > 1 ) { //Clamp
                t = 1;
            }else if(t < 0 ) {
                t = 0;
            }
            return interpolated.normalized.Mul((int)(t * interpolated.magnitude));
        }

        public static Vector2 Invalid() {
            return new Vector2(-1, -1);
        }

        public bool isInvalid() {
            return x == -1 && y == -1;
        }
    }
}

