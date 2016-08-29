using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectHelloWorld.SupportClasses {
    class DistanceTools {
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

        public static Joint GetClosestJoint(ref Joint origin, ref Joint first, ref Joint second) {
            if( MinimumDistanceCondition(origin.Position, first.Position) <=
                MinimumDistanceCondition(origin.Position, second.Position) )
                return first;
            return second;
        }

        public static bool FirstIsClosest(ref Joint origin, ref Joint first, ref Joint second) {
            return MinimumDistanceCondition(origin.Position, first.Position) <=
                MinimumDistanceCondition(origin.Position, second.Position);
        }
    }
}
