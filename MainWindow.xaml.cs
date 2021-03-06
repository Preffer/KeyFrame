﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Serialization;

namespace KeyFrame {
    public enum DrawMode {
        Begin,
        End
    }

    public enum DisplayMode {
        Input,
        Smooth
    }

    public enum EditMode {
        None,
        Append,
        Move,
        Insert
    }

    public enum BlendMode {
        Linear,
        Vector
    }

    public enum TimeMode {
        Linear,
        Easein,
        Easeout
    }

    public partial class MainWindow : Window, INotifyPropertyChanged {
        public static readonly Algebra.Matrix Hermite = new Algebra.Matrix(new double[4, 4] {
            {2, -2, 1, 1},
            {-3, 3, -2, -1},
            {0, 0, 1, 0},
            {1, 0, 0, 0}
        });

        private DrawMode drawStat = DrawMode.Begin;
        private DisplayMode displayStat = DisplayMode.Input;
        private EditMode editStat = EditMode.None;
        private double tension = 1;
        private int grain = 40;
        private BlendMode blendStat = BlendMode.Linear;
        private TimeMode timeStat = TimeMode.Linear;
        private double duration = 2;
        private Polyline activePolyline;
        private Polyline activeSmoothLine;
        private int activePointIndex = -1;
        private Timer timer;
        private double elapsed;
        private List<Polar> beginPolar;
        private List<Polar> endPolar;
        private const double SNAP_DISTANCE = 400;
        private bool rendering = false;
        private string path;
        RenderTargetBitmap bitmap;

        public MainWindow() {
            InitializeComponent();
            DataContext = this;
            activePolyline = BeginInputLine;
            activeSmoothLine = BeginSmoothLine;
            timer = new Timer(40);
            timer.Elapsed += TimerElapsed;
            PropertyChanged += SceneChanged;
        }

        public DrawMode DrawStat {
            get {
                return drawStat;
            }
            set {
                drawStat = value;
                switch (drawStat) {
                    case (DrawMode.Begin): {
                        activePolyline = BeginInputLine;
                        activeSmoothLine = BeginSmoothLine;
                        break;
                    }
                    case (DrawMode.End): {
                        activePolyline = EndInputLine;
                        activeSmoothLine = EndSmoothLine;
                        break;
                    }
                }
                NotifyPropertyChanged("DrawStat");
            }
        }

        private List<Polar> activePolar {
            set {
                switch (drawStat) {
                    case (DrawMode.Begin): {
                        beginPolar = value;
                        break;
                    }
                    case (DrawMode.End): {
                        endPolar = value;
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

        public BlendMode BlendStat {
            get {
                return blendStat;
            }
            set {
                blendStat = value;
                NotifyPropertyChanged("BlendStat");
            }
        }

        public TimeMode TimeStat {
            get {
                return timeStat;
            }
            set {
                timeStat = value;
                NotifyPropertyChanged("TimeStat");
            }
        }

        public double Duration {
            get {
                return duration;
            }
            set {
                duration = value;
                NotifyPropertyChanged("Duration");
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
            if (Run.IsEnabled) {
                if (e.LeftButton == MouseButtonState.Pressed) {
                    if (activePolyline.Points.Count == 0) {
                        activePolyline.Points.Add(e.GetPosition(Scene));
                    }
                    activePolyline.Points.Add(ApplySnap(e.GetPosition(Scene)));
                    NotifyPropertyChanged(activePolyline.Name);

                    activePointIndex = activePolyline.Points.Count - 1;
                    editStat = EditMode.Append;
                }
                if (e.MiddleButton == MouseButtonState.Pressed && activePolyline.Points.Count > 0) {
                    Point clicked = ApplySnap(e.GetPosition(Scene));
                    activePointIndex = activePolyline.Points.Select((point, index) => new KeyValuePair<Point, int>(point, index)).OrderBy(pair => (clicked - pair.Key).LengthSquared).First().Value;
                    editStat = EditMode.Move;
                }
                if (e.RightButton == MouseButtonState.Pressed && activePolyline.Points.Count >= 2) {
                    Point clicked = ApplySnap(e.GetPosition(Scene));

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
                activePolyline.Points.Insert(activePointIndex, ApplySnap(e.GetPosition(Scene)));
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

        private void Run_Click(object sender, RoutedEventArgs e) {
            if (BeginInputLine.Points.Count > 0 && EndInputLine.Points.Count > 0) {
                if (BeginInputLine.Points.Count == EndInputLine.Points.Count) {
                    elapsed = 0;
                    timer.Start();
                    Run.IsEnabled = false;
                    Render.IsEnabled = false;
                } else {
                    MessageBox.Show(FindResource("PointDismatchText") as string, FindResource("PointDismatchTitle") as string, MessageBoxButton.OK, MessageBoxImage.Stop);
                }
            } else {
                MessageBox.Show(FindResource("DrawShapeText") as string, FindResource("DrawShapeTitle") as string, MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }

        private void Render_Click(object sender, RoutedEventArgs e) {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                path = dialog.SelectedPath;
                bitmap = new RenderTargetBitmap((int)Scene.ActualWidth, (int)Scene.ActualHeight, 96, 96, PixelFormats.Default);
                rendering = true;
                Run_Click(sender, e);
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e) {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.DefaultExt = "xml";
            dialog.Filter = "XML Files (*.xml)|*.xml";

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                Matrix scaleMatrix = new Matrix();
                scaleMatrix.Scale(ActualWidth, ActualHeight);

                using (FileStream reader = File.OpenRead(dialog.FileName)) {
                    SceneArchive archive = (SceneArchive)(new XmlSerializer(typeof(SceneArchive))).Deserialize(reader);

                    DrawMode originStat = DrawStat;
                    DrawStat = DrawMode.Begin;
                    BeginInputLine.Points = new PointCollection(archive.Begin.Select(p => p * scaleMatrix));
                    NotifyPropertyChanged(activePolyline.Name);

                    DrawStat = DrawMode.End;
                    EndInputLine.Points = new PointCollection(archive.End.Select(p => p * scaleMatrix));
                    NotifyPropertyChanged(activePolyline.Name);
                    DrawStat = originStat;
                }

                MessageBox.Show(FindResource("SceneLoadText") as string + dialog.FileName, FindResource("SceneLoadTitle") as string, MessageBoxButton.OK, MessageBoxImage.Information);
            } 
        }

        private void Save_Click(object sender, RoutedEventArgs e) {
            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.DefaultExt = "xml";
            dialog.Filter = "XML Files (*.xml)|*.xml";

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                Matrix scaleMatrix = new Matrix();
                scaleMatrix.Scale(1 / ActualWidth, 1 / ActualHeight);

                SceneArchive archive = new SceneArchive(
                    new PointCollection(BeginInputLine.Points.Select(p => p * scaleMatrix)),
                    new PointCollection(EndInputLine.Points.Select(p => p * scaleMatrix))
                );

                using (FileStream writer = File.Create(dialog.FileName)) {
                    (new XmlSerializer(archive.GetType())).Serialize(writer, archive);
                }

                MessageBox.Show(FindResource("SceneSaveText") as string + dialog.FileName, FindResource("SceneSaveTitle") as string, MessageBoxButton.OK, MessageBoxImage.Information);
            } 
        }

        private void Clear_Click(object sender, RoutedEventArgs e) {
            timer.Stop();
            BeginInputLine.Points.Clear();
            BeginSmoothLine.Points.Clear();
            EndInputLine.Points.Clear();
            EndSmoothLine.Points.Clear();
            BlendInputLine.Points.Clear();
            BlendSmoothLine.Points.Clear();
            activePointIndex = -1;
            editStat = EditMode.None;
            Run.IsEnabled = true;
            Render.IsEnabled = true;
        }

        private void Help_Click(object sender, RoutedEventArgs e) {
            string helpText = "Click left button to append point\n"
                            + "Click middle button to move the nearest point\n"
                            + "Click right button to insert point\n"
                            + "Press Escape to save pending modification\n"
                            + "Press Delete to delete the pending point";

            MessageBox.Show(helpText, "Help", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e) {
            if (elapsed <= duration) {
                Dispatcher.Invoke(delegate() {
                    double rate = elapsed / duration;
                    switch (timeStat) {
                        case (TimeMode.Easein): {
                            rate = 1 - Math.Cos(Math.PI * rate / 2);
                            break;
                        }
                        case (TimeMode.Easeout): {
                            rate = Math.Sin(Math.PI * rate / 2);
                            break;
                        }
                    }
                    switch (blendStat) {
                        case (BlendMode.Linear): {
                            BlendInputLine.Points = LinearBlend(BeginInputLine.Points, EndInputLine.Points, rate);
                            break;
                        }
                        case (BlendMode.Vector): {
                            BlendInputLine.Points = VectorBlend(BeginInputLine.Points, EndInputLine.Points, beginPolar, endPolar, rate);
                            break;
                        }
                    }
                    BlendSmoothLine.Points = SmoothLine(BlendInputLine.Points);

                    if (rendering) {
                        bitmap.Render(Scene);
                        PngBitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmap));
                        using (FileStream file = File.Create(string.Format("{0}\\frame-{1:000}.png", path, (int)(elapsed * 25)))) {
                            encoder.Save(file);
                        }
                    }
                });
                elapsed += 0.04;
            } else {
                Dispatcher.Invoke(delegate() {
                    BlendInputLine.Points.Clear();
                    BlendSmoothLine.Points.Clear();
                    Run.IsEnabled = true;
                    Render.IsEnabled = true;
                });
                if (rendering) {
                    rendering = false;
                    MessageBox.Show(FindResource("SceneRenderText") as string + path, FindResource("SceneRenderTitle") as string, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                timer.Stop();
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) {
            Matrix scaleMatrix = new Matrix();
            scaleMatrix.Scale(e.NewSize.Width / e.PreviousSize.Width, e.NewSize.Height / e.PreviousSize.Height);

            BeginInputLine.Points = new PointCollection(BeginInputLine.Points.Select(p => p * scaleMatrix));
            BeginSmoothLine.Points = new PointCollection(BeginSmoothLine.Points.Select(p => p * scaleMatrix));
            EndInputLine.Points = new PointCollection(EndInputLine.Points.Select(p => p * scaleMatrix));
            EndSmoothLine.Points = new PointCollection(EndSmoothLine.Points.Select(p => p * scaleMatrix));
            BlendInputLine.Points = new PointCollection(BlendInputLine.Points.Select(p => p * scaleMatrix));
            BlendSmoothLine.Points = new PointCollection(BlendSmoothLine.Points.Select(p => p * scaleMatrix));
            beginPolar = PointToPolar(BeginInputLine.Points);
            endPolar = PointToPolar(EndInputLine.Points);
        }

        private void SceneChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == activePolyline.Name) {
                if (activePolyline.Points.Count >= 2) {
                    activeSmoothLine.Points = SmoothLine(activePolyline.Points);
                    activePolar = PointToPolar(activePolyline.Points);
                }
            }
        }

        private PointCollection LinearBlend(PointCollection beginLine, PointCollection endLine, double rate) {
            return new PointCollection(beginLine.Zip<Point, Point, Point>(endLine, (begin, end) => begin + rate * (end - begin)));
        }

        private PointCollection VectorBlend(PointCollection beginLine, PointCollection endLine, List<Polar> beginPolar, List<Polar> endPolar, double rate) {
            PointCollection blend = new PointCollection();
            blend.Add(beginLine.First() + rate * (endLine.First() - beginLine.First()));

            if (beginLine.First() == beginLine.Last() && endLine.First() == endLine.Last()) {
                int count = beginPolar.Count - 1;
                for (int i = 0; i < count; i++) {
                    blend.Add(blend.Last() + Polar.Blend(beginPolar[i], endPolar[i], rate).ToVector());
                }
                blend.Add(beginLine.Last() + rate * (endLine.Last() - beginLine.Last()));
            } else {
                int count = beginPolar.Count;
                for (int i = 0; i < count; i++) {
                    blend.Add(blend.Last() + Polar.Blend(beginPolar[i], endPolar[i], rate).ToVector());
                }
            }

            return blend;
        }

        private List<Polar> PointToPolar(PointCollection line) {
            return new List<Polar>(line.Zip<Point, Point, Polar>(line.Skip(1), (one, two) => new Polar(two - one)));
        }

        private PointCollection SmoothLine(PointCollection inputLine) {
            List<Point> controlPoint = new List<Point>();
            PointCollection smoothPoint = new PointCollection();

            if (inputLine.First() == inputLine.Last()) {
                controlPoint.Add(inputLine.ElementAt(inputLine.Count - 2));
                controlPoint.AddRange(inputLine);
                controlPoint.Add(inputLine.ElementAt(1));
            } else {
                controlPoint.Add(inputLine.First());
                controlPoint.AddRange(inputLine);
                controlPoint.Add(inputLine.Last());
            }

            int count = controlPoint.Count - 3;
            double step = 1.0 / grain;
            for (int i = 0; i < count; i++) {
                for (double u = 0; u < 1.0; u += step) {
                    smoothPoint.Add(Interpolation(controlPoint[i], controlPoint[i + 1], controlPoint[i + 2], controlPoint[i + 3], u, tension));
                }
                smoothPoint.Add(Interpolation(controlPoint[i], controlPoint[i + 1], controlPoint[i + 2], controlPoint[i + 3], 1.0, tension));
            }

            return smoothPoint;
        }

        private Point Interpolation(Point p0, Point p1, Point p2, Point p3, double u, double t) {
            Algebra.Vector uVector = new Algebra.Vector(new double[4] { Math.Pow(u, 3), Math.Pow(u, 2), u, 1 });
            Algebra.Vector uhVector = uVector * Hermite;
            Algebra.Vector pxVector = new Algebra.Vector(new double[4] { p1.X, p2.X, t * (p2.X - p0.X), t * (p3.X - p1.X) });
            Algebra.Vector pyVector = new Algebra.Vector(new double[4] { p1.Y, p2.Y, t * (p2.Y - p0.Y), t * (p3.Y - p1.Y) });

            return new Point(uhVector * pxVector, uhVector * pyVector);
        }

        private Point ApplySnap(Point p) {
            if ((activePolyline.Points.First() - p).LengthSquared < SNAP_DISTANCE) {
                return activePolyline.Points.First();
            } else {
                return p;
            }
        }

    }
}
