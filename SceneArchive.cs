using System;
using System.Windows.Media;

namespace KeyFrame {
    public class SceneArchive {
        public PointCollection Begin;
        public PointCollection End;

        public SceneArchive() {
        
        }
        
        public SceneArchive(PointCollection begin, PointCollection end) {
            Begin = begin;
            End = end;
        }
    }
}
