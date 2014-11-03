using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
//using Ni.Libraries.Midi;
using OpenCvSharp;
using Avt.Mako;
using Timer = System.Windows.Forms.Timer;

namespace RbmaSushiPlateDetector
{
    public partial class Form1 : Form
    {
        private Camera _camera;
        public CvWindow Window;
        public static Form1 Instance { get; private set; }
        internal Setting Setting;
        
        private static IplImage _image = new IplImage(new CvSize(1292, 964), BitDepth.U8, 3);
        private static IplImage _downsampled;
        private static IplImage _background = new IplImage(new CvSize(1625 + 20, 964 + 20), BitDepth.U8, 3);

        private static Maschine _maschine = new Maschine();
        
        //private static MidiEngine _midiEngine;

        private string _midiOut = "1. Internal MIDI";

        //public static void SendMidi(MidiMessage m)
        //{
        //    ThreadPool.QueueUserWorkItem(AsyncSendMidi, m);
        //}

        //private static void AsyncSendMidi(object o)
        //{
        //    var m = o as MidiMessage;
        //    if (m == null) return;

        //    _midiEngine.Send(m);
        //}

        public Form1()
        {
            InitializeComponent();

            var detector = new Detector();
            detector.Detected += DetectorOnDetected;

            //_midiEngine = new MidiEngine();
            //_midiEngine.InstantiateOutputDevice(_midiOut);
            //_midiEngine.InstantiateInputDevice("Maschine MK2 In");

            //_midiEngine.MidiMessageReceived += (sender, args) =>
            //{
            //    if (args.MidiMessage.Device == "Maschine MK2 In")
            //    {
            //        args.MidiMessage.Device = _midiOut;
            //        //_maschine.SetColor(args.MidiMessage.Data2 * 2);
            //        SendMidi(args.MidiMessage);
            //    }
            //};

            _maschine = new Maschine();
            _maschine.SetColor(0);

            Instance = this;

            CreateButtons();
            SetUi();
            SetEventHandlers();
        }

        private void buttonRun_Click(object sender, EventArgs e)
        {
            Window = new CvWindow("OpenCV AVT Mako");
            Cam(); 
            //Test();
            //Setup();
        }

        private static void Setup()
        {
            new Training();
            Training.Train();
            //Training.Validate();
        }

        private static void Cam()
        {
            _downsampled = new IplImage(323, 241, BitDepth.U8, 3);
            Instance._camera = new Camera();
            Instance._camera.NewFrame += (s, a) =>
            {
                _image.CopyPixelData(a.Buffer);

                Cv.Resize(_image, _downsampled);
                Detector.NewFrame(_background, _image, _downsampled, a.Count);
                Instance.Window.Image = _background;
                Instance.label1.Invoke((MethodInvoker)(() => Instance.label1.Text = @"Frame: " + a.Count));
            };
        }

        private static void Test()
        {
            var cap = CvCapture.FromFile(@"C:\Users\michael.hlatky\Desktop\test.mp4");
            _downsampled = new IplImage(323, 241, BitDepth.U8, 3);
            var t = new Timer { Interval = 30 };
            t.Start();
            t.Tick += (sender, args) =>
            {
                _image = cap.QueryFrame();
                
                if (cap.PosFrames == cap.FrameCount)
                    cap.PosFrames = 0;

                Cv.Resize(_image, _downsampled);
                Detector.NewFrame(_background, _image, _downsampled, (ulong)cap.PosFrames);
                Instance.Window.Image = _background;
                Instance.label1.Invoke((MethodInvoker) (() => Instance.label1.Text = @"Frame: " + cap.PosFrames));
            };
        }

        static int _lastIndex = -1;
        private static ulong _frame;

        private static void DetectorOnDetected(object sender, DetectedEventArgs args)
        {
            if (args.Distance > 700 || args.Color == _lastIndex || args.Frame <= _frame + 60 || args.Color == 4)
                return;

            _lastIndex = args.Color;
            _frame = args.Frame;

            Console.WriteLine(args.X.ToString("000") + @" " + args.Y.ToString("000") + @" " +
                              args.Frame.ToString("0000") + @" " + args.Distance.ToString("00000") + @" " + Helpers.Names[args.Color]);    

            _maschine.SetColor(args.H * 2);

            //SendMidi(new MidiMessage(Instance._midiOut, (byte)args.Color, 127, ChannelCommand.NoteOn, 0));
        }

        internal static void CreateButtons()
        {
            var y = 10;
            byte x = 0;
            foreach (var b in Helpers.Names)
            {
                var button = new Button();
                button.Name = "button_" + b;
                button.Text = b;
                button.Location = new Point(Instance.trackBar1.Width + 110, y);
                y += 30;
                button.Show();
                button.Tag = x++;
                button.Click += (sender, args) =>
                {
                    var bb = (byte) (sender as Control).Tag;
                    //var m = new MidiMessage(Instance._midiOut, bb, 127, ChannelCommand.NoteOn, 0);
                    //SendMidi(m);
                };
                Instance.Controls.Add(button);
            }
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

            trackBar1.ValueChanged += (sender, args) => { Detector.Clipping.X = trackBar1.Value; Console.WriteLine(trackBar1.Value); };
            trackBar2.ValueChanged += (sender, args) => { Detector.Clipping.Width = trackBar2.Value; Console.WriteLine(trackBar2.Value); };
            trackBar3.ValueChanged += (sender, args) => { Detector.Clipping.Y = trackBar3.Value; Console.WriteLine(trackBar3.Value); };
            trackBar4.ValueChanged += (sender, args) => { Detector.Clipping.Height = trackBar4.Value; Console.WriteLine(trackBar4.Value); };
            trackBar5.ValueChanged += (sender, args) => Detector.Dp = trackBar5.Value / 10d;
            trackBar6.ValueChanged += (sender, args) => Detector.Blur = (trackBar6.Value * 2) + 1;
            trackBar7.ValueChanged += (sender, args) => Detector.MinRadius = trackBar7.Value;
            trackBar8.ValueChanged += (sender, args) => Detector.MaxRadius = trackBar8.Value;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Detector.Save = checkBox1.Checked;
        }
    }
}
