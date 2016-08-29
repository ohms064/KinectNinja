using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectHelloWorld.SupportClasses {
    class Vector3 {
        /// <summary>
        /// Clase que representa un Vector de 2 valores enteros.
        /// </summary>

        public int x;
        public int y;
        public int z;

        public Vector3(int x, int y, int z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static int Dot(Vector3 first, Vector3 second) {
            int result = 0;
            result += first.x * second.x;
            result += first.y * second.y;
            result += first.z * second.z;
            return result;
        }

        public static float Dot(SkeletonPoint first, SkeletonPoint second) {
            float result = 0;
            result += first.X * second.X;
            result += first.Y * second.Y;
            result += first.Z * second.Z;
            return result;
        }

    }
}
