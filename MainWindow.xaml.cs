using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KeyFrame {
    public enum DrawMode {
        Begin,
        End,
        Both
    }

    public enum RenderMode {
        Animation,
        Fusion,
        Video
    }

    public partial class MainWindow : Window, INotifyPropertyChanged {
        private DrawMode drawStat = DrawMode.Both;
        private RenderMode renderStat = RenderMode.Animation;
        private double thickness = 2;

        public MainWindow() {
            InitializeComponent();
            DataContext = this;
        }

        public DrawMode DrawStat {
            get {
                return drawStat;
            }
            set {
                drawStat = value;
                NotifyPropertyChanged("DrawStat");
            }
        }

        public RenderMode RenderStat {
            get {
                return renderStat;
            }
            set {
                renderStat = value;
                NotifyPropertyChanged("RenderStat");
            }
        }

        public double Thickness {
            get {
                return thickness;
            }
            set {
                thickness = value;
                NotifyPropertyChanged("Thickness");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
