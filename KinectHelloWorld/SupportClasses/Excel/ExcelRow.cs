using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Emgu.CV.Face.FaceRecognizer;

namespace KinectHelloWorld.SupportClasses.Excel {
    class ExcelRow {
        public string appStart = "";
        public string wins = "0";
        public string losses = "0";
        public string productsGiven = "0";
        public string playerMale = "0";
        public string playerFemale = "0";
        public string playerUnknown = "0";
        public string impressionMale = "0";
        public string impressionFemale = "0";
        public string impressionUnknown = "0";
        public static ExcelRow instance;

        public void SetData(int column, string value) {
            switch( column ) {
                case 3:
                    appStart = value;
                    break;
                case 4:
                    wins = value;
                    break;
                case 5:
                    losses = value;
                    break;
                case 6:
                    productsGiven = value;
                    break;
                case 7:
                    playerMale = value;
                    break;
                case 8:
                    playerFemale = value;
                    break;
                case 9:
                    playerUnknown = value;
                    break;
                case 10:
                    impressionMale = value;
                    break;
                case 11:
                    impressionFemale = value;
                    break;
                case 12:
                    impressionUnknown = value;
                    break;
                default:
                    Console.WriteLine("Esto no debería pasar");
                    break;
            }
        }

        public string GetData(int column) {
            switch( column ) {
                case 3:
                    return appStart;
                case 4:
                    return wins;
                case 5:
                    return losses;
                case 6:
                    return productsGiven;
                case 7:
                    return playerMale;
                case 8:
                    return playerFemale;
                case 9:
                    return playerUnknown;
                case 10:
                    return impressionMale;
                case 11:
                    return impressionFemale;
                case 12:
                    return impressionUnknown;
                default:
                    Console.WriteLine("Esto no debería pasar");
                    return null;
            }
        }

        public void AddOther(ExcelRow other) {
            playerMale = AddValue(playerMale, other.playerMale);
            playerFemale = AddValue(playerFemale, other.playerFemale);
            playerUnknown = AddValue(playerUnknown, other.playerUnknown);
            impressionMale = AddValue(impressionMale, other.impressionMale);
            impressionFemale = AddValue(impressionFemale, other.impressionFemale);
            impressionUnknown = AddValue(impressionUnknown, other.impressionUnknown);
            wins = AddValue(wins, other.wins);
            losses = AddValue(losses, other.losses);
            productsGiven = AddValue(productsGiven, other.productsGiven);
            appStart = other.appStart;
        }

        public void ReceiveImpressionsPredictions(PredictionResult[] results) {
            int male = 0, female = 0, unknown = 0;
            foreach(PredictionResult result in results ) {
                if( result.Distance > GenderClassifier.threshold ) {
                    unknown++;
                }
                else {
                    switch( (GenderEnum) result.Label ) {
                        case GenderEnum.FEMALE:
                            female++;
                            break;
                        case GenderEnum.MALE:
                            male++;
                            break;
                    }
                }
            }
            impressionMale = male.ToString();
            impressionFemale = female.ToString();
            impressionUnknown = unknown.ToString();
        }

        public void ReceivePlayerPrediction(PredictionResult result) {
            int male = 0, female = 0, unknown = 0;
            if( result.Distance > GenderClassifier.threshold ) {
                unknown++;
            }
            else {
                switch( (GenderEnum) result.Label ) {
                    case GenderEnum.FEMALE:
                        female++;
                        break;
                    case GenderEnum.MALE:
                        male++;
                        break;
                }
            }
            impressionMale = male.ToString();
            impressionFemale = female.ToString();
            impressionUnknown = unknown.ToString();
        }

        private string AddValue(string value1, string value2) {
            int aux1 = 0;
            int aux2 = 0;
            if(!int.TryParse(value1, out aux1) ) {
                aux1 = 0;
            }
            if(!int.TryParse(value2, out aux2) ) {
                aux2 = 0;
            }
            return (aux1 + aux2).ToString();
        }
    }
}
