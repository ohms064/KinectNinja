using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectHelloWorld.SupportClasses {
    public static class DateUtils {
        public static string GetDate(this DateTime d) {
            return string.Format("{0}-{1}-{2}", d.Day, d.Month, d.Year);
        }

        public static string GetTime(this DateTime d) {
            return string.Format("{0}:{1}:{2}", d.Hour, d.Minute, d.Second);
        }
    }
}
