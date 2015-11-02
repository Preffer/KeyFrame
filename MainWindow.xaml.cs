﻿using System;
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
        End
    }

    public enum DisplayMode {
        Input,
        Smooth
    }

    public enum RenderMode {
        Animation,
        Fusion,
        Video
    }

    public enum EditMode {
        None,
        Append,
        Move,
        Insert
    }

    public partial class MainWindow : Window, INotifyPropertyChanged {
        private DrawMode drawStat = DrawMode.Begin;
        private DisplayMode displayStat = DisplayMode.Input;
        private RenderMode renderStat = RenderMode.Animation;
        private EditMode editStat = EditMode.None;
        private Polyline activePolyline;
        private int activePointIndex = -1;

        public MainWindow() {
            InitializeComponent();
            DataContext = this;
            activePolyline = BeginInputLine;
        }

        public DrawMode DrawStat {
            get {
                return drawStat;
            }
            set {
                drawStat = value;
                NotifyPropertyChanged("DrawStat");
                switch (drawStat) {
                    case (DrawMode.Begin): {
                        activePolyline = BeginInputLine;
                        break;
                    }
                    case (DrawMode.End): {
                        activePolyline = EndInputLine;
                        break;
                    }
                    default: {
                        activePolyline = null;
                        break;
                    }
                }
            }
        }

        public DisplayMode DisplayStat {
            get {
                return displayStat;
            }
            set {
                displayStat = value;
                NotifyPropertyChanged("DisplayStat");
                NotifyPropertyChanged("ShowInputLine");
                NotifyPropertyChanged("ShowSmoothLine");
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

        public Visibility ShowInputLine {
            get {
                return displayStat == DisplayMode.Input ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public Visibility ShowSmoothLine {
            get {
                return displayStat == DisplayMode.Smooth ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void Scene_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                if (activePolyline.Points.Count == 0) {
                    activePolyline.Points.Add(e.GetPosition(Scene));
                }
                activePolyline.Points.Add(e.GetPosition(Scene));
                NotifyPropertyChanged(activePolyline.Name);

                activePointIndex = activePolyline.Points.Count - 1;
                editStat = EditMode.Append;
            }
            if (e.MiddleButton == MouseButtonState.Pressed && activePolyline.Points.Count > 0) {
                Point clicked = e.GetPosition(Scene);
                activePointIndex = activePolyline.Points.Select((point, index) => new KeyValuePair<Point, int>(point, index)).OrderBy(pair => (clicked - pair.Key).LengthSquared).First().Value;
                editStat = EditMode.Move;
            }
            if (e.RightButton == MouseButtonState.Pressed && activePolyline.Points.Count >= 2) {
                Point clicked = e.GetPosition(Scene);

                activePointIndex = activePolyline.Points
                    .Take(activePolyline.Points.Count - 1)
                    .Select((point, index) => new KeyValuePair<Point, int>(point, index))
                    .Zip<KeyValuePair<Point, int>, Point, KeyValuePair<double, int>>(
                        activePolyline.Points.Skip(1),
                        (one, two) => new KeyValuePair<double, int>(
                            (Math.Abs(Vector.AngleBetween(clicked - one.Key, two - one.Key)) < 90 && Math.Abs(Vector.AngleBetween(clicked - two, one.Key - two)) < 90)
                            ? Math.Abs(Vector.CrossProduct(clicked - one.Key, clicked - two) / 2 / (one.Key - two).Length)
                            : Math.Min((clicked - one.Key).Length, (clicked - two).Length),
                            one.Value
                        )
                    ).OrderBy(pair => pair.Key).First().Value + 1;

                activePolyline.Points.Insert(activePointIndex, clicked);
                NotifyPropertyChanged(activePolyline.Name);
                editStat = EditMode.Insert;
            }
        }

        private void Scene_MouseUp(object sender, MouseButtonEventArgs e) {
            if (editStat != EditMode.Append) {
                activePointIndex = -1;
                editStat = EditMode.None;
            }
        }

        private void Scene_MouseMove(object sender, MouseEventArgs e) {
            if (activePointIndex != -1) {
                activePolyline.Points.RemoveAt(activePointIndex);
                activePolyline.Points.Insert(activePointIndex, e.GetPosition(Scene));
                NotifyPropertyChanged(activePolyline.Name);
            }
        }

        private void Scene_KeyDown(object sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Escape: {
                    activePointIndex = -1;
                    editStat = EditMode.None;
                    break;
                }
                case Key.Delete: {
                    if (activePointIndex != -1) {
                        activePolyline.Points.RemoveAt(activePointIndex);
                        NotifyPropertyChanged(activePolyline.Name);

                        activePointIndex = -1;
                        editStat = EditMode.None;
                    }
                    break;
                }
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) {
            Matrix scaleMatrix = new Matrix();
            scaleMatrix.Scale(e.NewSize.Width / e.PreviousSize.Width, e.NewSize.Height / e.PreviousSize.Height);

            BeginInputLine.Points = new PointCollection(BeginInputLine.Points.Select(p => p * scaleMatrix));
            EndInputLine.Points = new PointCollection(EndInputLine.Points.Select(p => p * scaleMatrix));
        }

        private void SceneChanged(object sender, PropertyChangedEventArgs e) {

        }
    }
}
