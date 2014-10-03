using System;
using System.Windows.Forms;
using Ni.Libraries.Midi;
using OpenCvSharp;
using Avt.Mako;

namespace RbmaSushiPlateDetector
{
    public partial class Form1 : Form
    {
        private Camera _camera;
        private CvWindow _window;
        public static Form1 Instance { get; private set; }
        internal Setting Setting;
        private static IplImage _image = new IplImage(new CvSize(1292, 964), BitDepth.U8, 3);
        private static IplImage _downsampled = new IplImage(323, 241, BitDepth.U8, 3);

        public Form1()
        {
            InitializeComponent();

            Instance = this;

            var midiEngine = new MidiEngine();
            midiEngine.InstantiateOutputDevice("1. Internal MIDI");

            var detector = new Detector();
            detector.Detected += (sender, args) =>
            {
                Console.WriteLine(args.Color + @" " + args.X.ToString("000") + @" " + args.Y.ToString("000") + @" " +
                                  args.Frame);
                midiEngine.Send(new MidiMessage("1. Internal MIDI", (byte) args.Color, 127, ChannelCommand.NoteOn, 0));
            };
               
           
            SetUi();
            SetEventHandlers();

            Shown += (sender, args) =>
            {
                _window = new CvWindow("OpenCV AVT Mako");
                //Cam();
                Test();
            };
        }

        private static void Cam()
        {
            Instance._camera = new Camera();
            Instance._camera.NewFrame += (s, a) =>
            {
                _image.CopyPixelData(a.Buffer);

                Cv.Resize(_image, _downsampled);
                Detector.NewFrame(_downsampled, a.Count);
                Instance._window.Image = _downsampled;
                Instance.label1.Invoke((MethodInvoker)(() => Instance.label1.Text = @"Frame: " + a.Count));
            };
        }

        private static void Test()
        {
            var cap = CvCapture.FromFile(@"C:\Users\michael.hlatky\Documents\Sequences\Camera 1\23-22-46.543.avi");

            var t = new Timer { Interval = 30 };
            t.Start();
            t.Tick += (sender, args) =>
            {
                _image = cap.QueryFrame();
                
                if (cap.PosFrames == cap.FrameCount)
                    cap.PosFrames = 0;

                Cv.Resize(_image, _downsampled);
                Detector.NewFrame(_downsampled, (ulong)cap.PosFrames);
                Instance._window.Image = _downsampled;
                Instance.label1.Invoke((MethodInvoker) (() => Instance.label1.Text = @"Frame: " + cap.PosFrames));
            };
        }

        internal void SetUi()
        {
            trackBar1.Value = Detector.Clipping.X;
            trackBar2.Value = Detector.Clipping.Width;
            trackBar3.Value = Detector.Clipping.Y;
            trackBar4.Value = Detector.Clipping.Height;
            trackBar5.Value = (int)(Detector.Dp * 10);
            trackBar6.Value = (Detector.Blur - 1) / 2;
            trackBar7.Value = Detector.MinRadius;
            trackBar8.Value = Detector.MaxRadius;
        }

        private void SetEventHandlers()
        {
            Closing += (sender, args) => _camera.Dispose();

            trackBar1.ValueChanged += (sender, args) => Detector.Clipping.X = trackBar1.Value;
            trackBar2.ValueChanged += (sender, args) => Detector.Clipping.Width = trackBar2.Value;
            trackBar3.ValueChanged += (sender, args) => Detector.Clipping.Y = trackBar3.Value;
            trackBar4.ValueChanged += (sender, args) => Detector.Clipping.Height = trackBar4.Value;
            trackBar5.ValueChanged += (sender, args) => Detector.Dp = trackBar5.Value / 10d;
            trackBar6.ValueChanged += (sender, args) => Detector.Blur = (trackBar6.Value * 2) + 1;
            trackBar7.ValueChanged += (sender, args) => Detector.MinRadius = trackBar7.Value;
            trackBar8.ValueChanged += (sender, args) => Detector.MaxRadius = trackBar8.Value;

            button1.Click += (sender, args) => Setting.SaveSetting();
            button2.Click += (sender, args) => Setting.LoadSetting();
        }
    }
}
