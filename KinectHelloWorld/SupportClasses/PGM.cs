using ShaniSoft.IO.DataReader;
using System.IO;
using System.Drawing;
using ShaniSoft.Drawing.PNMReader;

namespace KinectHelloWorld.SupportClasses {
    class PGM {
        public const string P5 = "P5";
        public const string P2 = "P2";
        public bool binary = false;
        public int width, height;
        public int white;
        private IPNMDataReader reader;

        public PGM(string path) {
            if( !File.Exists(path) ) {
                return;
            }
            string line;
            int lineNumber = 0;
            int byteCount = 0;
            StreamReader sr = new StreamReader(path);
            while( ( line = sr.ReadLine().Trim() ) != null ) {
                if( line.StartsWith("#") ) {
                    continue;
                }
                lineNumber++;
                switch( lineNumber ) {
                    case 1:
                        binary = line.Equals(P5);
                        if( binary ) {
                            reader = new BinaryDataReader(new FileStream(path, FileMode.Open));
                        }else {
                            reader = new ASCIIDataReader(new FileStream(path, FileMode.Open));
                        }
                        break;
                    case 2:
                        string[] aux = line.Split(' ');
                        width = int.Parse(aux[0]);
                        height = int.Parse(aux[1]);
                        break;
                    case 3:
                        white = int.Parse(line);
                        break;
                    default:
                        return;
                }

            }
        }

        public Image ReadImage() {
            PGMReader pr = new PGMReader();
            return pr.ReadImageData(reader, width, height);
        }
    }
}
