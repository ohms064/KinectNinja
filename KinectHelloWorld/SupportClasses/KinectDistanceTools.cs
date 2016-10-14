using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectHelloWorld.SupportClasses {
    class KinectDistanceTools {
        /// <summary>
        /// La condición para determinar que posición está más cerca a un punto.
        /// La fórmula es: (origin • destiny) - 0.5 (destiny • destiny) donde "•"
        /// es el producto punto de esos vectores.
        /// Para dos puntos destino y un origen el punto destino más cercano
        /// al origen es aquel cuyo resultado de la ecuación sea el mínimo.
        /// </summary>
        /// <param name="origin">El punto que se usará de referencia</param>
        /// <param name="destiny">El punto de interés</param>
        /// <returns></returns>
        public static float MinimumDistanceCondition(SkeletonPoint origin, SkeletonPoint destiny) {
            return Vector3.Dot(origin, destiny) - 0.5f * Vector3.Dot(destiny, destiny);
        }

        /// <summary>
        /// De un arreglo de Joints, retorna el más cercano a uno de referencia.
        /// </summary>
        /// <param name="origin">El Joint de referencia.</param>
        /// <param name="values">Los Joints del que queremos obtener el más cercano.</param>
        /// <returns>El Joint más cercano a origin</returns>
        public static Joint GetClosestJoint(ref Joint origin, Joint[] values) {
            Joint closest = values[0];
            for(int iter = 1; iter < values.Length; iter++ ) {
                if(!FirstIsCloser(ref origin, ref closest, ref values[iter]) ) {
                    closest = values[iter];
                }
            }
            
            return closest;
        }

        /// <summary>
        /// Indica si un Joint está más cerca que otro, en base a un Joint de referencia.
        /// </summary>
        /// <param name="origin">El Joint usado de referencia.</param>
        /// <param name="first">El primer Joint.</param>
        /// <param name="second">El segundo Joint.</param>
        /// <returns>True si el Joint first es el más cercano, false si el segundo es el más cercano.</returns>
        public static bool FirstIsCloser(ref Joint origin, ref Joint first, ref Joint second) {
            return MinimumDistanceCondition(origin.Position, first.Position) <=
                MinimumDistanceCondition(origin.Position, second.Position);
        }

        /// <summary>
        /// Determina si el primer Joint es el más cercano al Kinect.
        /// </summary>
        /// <param name="first">El primer Joint.</param>
        /// <param name="second">El segundo Joint.</param>
        /// <returns>True si first está más cercano al Kinect.</returns>
        public static bool FirstIsCloserToSensor(ref Joint first, ref Joint second) {
            return first.Position.Z < second.Position.Z;
        }

        public static bool FirstSkeletonIsCloserToSensor(ref Skeleton first, ref Skeleton second, JointType jointReference) {
            Joint firstJoint = first.Joints[jointReference];
            Joint secondJoint = second.Joints[jointReference];
            return FirstIsCloserToSensor(ref firstJoint, ref secondJoint);
        }
    }
}
