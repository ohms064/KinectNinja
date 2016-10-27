using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace KinectHelloWorld.SupportClasses {
    /// <summary>
    /// Container which saves a filePath to an image and a label indicating what the photo represents.
    /// For this application 0 means male and 1 is female.
    /// </summary>
    public class TrainingData {
        public string filePath { get; set; }
        public GenderEnum label { get; set; }

        public static void Serialize(string path, List<TrainingData> data) {
            XmlSerializer ser = new XmlSerializer(typeof(List<TrainingData>));
            TextWriter tw = new StreamWriter(path);
            ser.Serialize(tw, data);
            tw.Close();
        }

        public static List<TrainingData> Deserialize(string path) {
            XmlSerializer ser = new XmlSerializer(typeof(List<TrainingData>));
            if( !File.Exists(path) )
                return new List<TrainingData>();
            StreamReader stream = new StreamReader(path);
            List<TrainingData> salida;
            salida = ser.Deserialize(stream) as List<TrainingData>;
            stream.Close();
            return salida;
        }
    }

}
