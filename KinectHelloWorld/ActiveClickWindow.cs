using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectHelloWorld {
    class ActiveClickWindow {
        public int waitFrames;
        public IEnumerable<bool> isClicking {
            get {
                for( int i = 0; i < waitFrames; i++ ) {
                    yield return false;
                }
                yield return true;
            }
        }
        public ActiveClickWindow(int WaitFrames) {
            this.waitFrames = WaitFrames;
        }
    }
}
