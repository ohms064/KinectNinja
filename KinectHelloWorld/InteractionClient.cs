using Microsoft.Kinect.Toolkit.Interaction;

namespace KinectHelloWorld {
    internal class InteractionClient : IInteractionClient {
        /// <summary>
        /// Como utilizamos este programa para controlar el mouse con alguna de las dos manos
        /// retornamos que la ubicación tiene la posibilidad de grip y press.
        /// </summary>
        /// <returns>InteractionInfo con la información de la interacción</returns>
        public InteractionInfo GetInteractionInfoAtLocation(int skeletonTrackingId, InteractionHandType handType, double x, double y) {
            InteractionInfo result = new InteractionInfo();
            result.IsGripTarget = true;
            result.IsPressTarget = true;
            result.PressAttractionPointX = 0.5;
            result.PressAttractionPointY = 0.5;
            result.PressTargetControlId = 1;

            return result;
        }

    }
}