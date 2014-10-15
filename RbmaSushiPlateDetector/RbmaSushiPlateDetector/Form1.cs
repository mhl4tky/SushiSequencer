using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Ni.Libraries.Midi;
using OpenCvSharp;
using Avt.Mako;
using Timer = System.Windows.Forms.Timer;

namespace RbmaSushiPlateDetector
{
    public partial class Form1 : Form
    {
        private Camera _camera;
        internal CvWindow _window;
        public static Form1 Instance { get; private set; }
        internal Setting Setting;
        private static IplImage _image = new IplImage(new CvSize(1292, 964), BitDepth.U8, 3);
        private static IplImage _downsampled;
        private static IplImage _background = new IplImage(new CvSize(1625 + 20, 964 + 20), BitDepth.U8, 3);
        private static MidiEngine midiEngine;
        private static bool started = false;

        public Form1()
        {
            InitializeComponent();

            midiEngine = new MidiEngine();
            midiEngine.InstantiateOutputDevice("1. Internal MIDI");

            Instance = this;

            SetUi();
            SetEventHandlers();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            _window = new CvWindow("OpenCV AVT Mako");
            Cam(); 
            //Test();
            //Setup();
        }

        private static void Setup()
        {
            var training = new Training();
            Training.Train();
            Training.Validate();
        }

        private static void Cam()
        {
            var detector = new Detector();
            detector.Detected += DetectorOnDetected;

            _downsampled = new IplImage(323, 241, BitDepth.U8, 3);
            Instance._camera = new Camera();
            Instance._camera.NewFrame += (s, a) =>
            {
                _image.CopyPixelData(a.Buffer);

                Cv.Resize(_image, _downsampled);
                Detector.NewFrame(_background, _image, _downsampled, a.Count);
                Instance._window.Image = _background;
                Instance.label1.Invoke((MethodInvoker)(() => Instance.label1.Text = @"Frame: " + a.Count));
            };
        }

        private static void Test()
        {
            var detector = new Detector();
            detector.Detected += DetectorOnDetected;

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
                Instance._window.Image = _background;
                Instance.label1.Invoke((MethodInvoker) (() => Instance.label1.Text = @"Frame: " + cap.PosFrames));
            };
        }

        static int lastIndex = -1;
        private static ulong frame = 0;

        private static void DetectorOnDetected(object sender, DetectedEventArgs args)
        {
            if (args.Distance < 700 && args.Color != lastIndex && args.Frame > frame + 60 && args.Color != 4)
            {
                lastIndex = args.Color;
                frame = args.Frame;

                Console.WriteLine(args.X.ToString("000") + @" " + args.Y.ToString("000") + @" " +
                                  args.Frame.ToString("0000") + @" " + args.Distance.ToString("00000") + @" " + Helpers.Names[args.Color]);


                if (!started)
                {
                    midiEngine.SendStart("1. Internal MIDI");
                    started = true;
                }
                    

                midiEngine.Send(new MidiMessage("1. Internal MIDI", (byte)args.Color, 127, ChannelCommand.NoteOn, 0));
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

            trackBar1.ValueChanged += (sender, args) => Detector.Clipping.X = trackBar1.Value;
            trackBar2.ValueChanged += (sender, args) => Detector.Clipping.Width = trackBar2.Value;
            trackBar3.ValueChanged += (sender, args) => Detector.Clipping.Y = trackBar3.Value;
            trackBar4.ValueChanged += (sender, args) => Detector.Clipping.Height = trackBar4.Value;
            trackBar5.ValueChanged += (sender, args) => Detector.Dp = trackBar5.Value / 10d;
            trackBar6.ValueChanged += (sender, args) => Detector.Blur = (trackBar6.Value * 2) + 1;
            trackBar7.ValueChanged += (sender, args) => Detector.MinRadius = trackBar7.Value;
            trackBar8.ValueChanged += (sender, args) => Detector.MaxRadius = trackBar8.Value;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Detector.Save = checkBox1.Checked;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            midiEngine.Send(new MidiMessage("1. Internal MIDI", (byte)0, 127, ChannelCommand.NoteOn, 0));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            midiEngine.Send(new MidiMessage("1. Internal MIDI", (byte)1, 127, ChannelCommand.NoteOn, 0));
        }

        private void button3_Click(object sender, EventArgs e)
        {
            midiEngine.Send(new MidiMessage("1. Internal MIDI", (byte)2, 127, ChannelCommand.NoteOn, 0));
        }

        private void button5_Click(object sender, EventArgs e)
        {
            midiEngine.Send(new MidiMessage("1. Internal MIDI", (byte)3, 127, ChannelCommand.NoteOn, 0));
        }

       

        private void button6_Click(object sender, EventArgs e)
        {
            midiEngine.Send(new MidiMessage("1. Internal MIDI", (byte)4, 127, ChannelCommand.NoteOn, 0));
        }

        private void button7_Click(object sender, EventArgs e)
        {
            midiEngine.Send(new MidiMessage("1. Internal MIDI", (byte)5, 127, ChannelCommand.NoteOn, 0));
        }

        private void button13_Click(object sender, EventArgs e)
        {
            midiEngine.Send(new MidiMessage("1. Internal MIDI", (byte)6, 127, ChannelCommand.NoteOn, 0));
        }

        private void button12_Click(object sender, EventArgs e)
        {
            midiEngine.Send(new MidiMessage("1. Internal MIDI", (byte)7, 127, ChannelCommand.NoteOn, 0));
        }

        private void button11_Click(object sender, EventArgs e)
        {
            midiEngine.Send(new MidiMessage("1. Internal MIDI", (byte)8, 127, ChannelCommand.NoteOn, 0));
        }

        private void button8_Click(object sender, EventArgs e)
        {
            midiEngine.Send(new MidiMessage("1. Internal MIDI", (byte)9, 127, ChannelCommand.NoteOn, 0));
        }

        private void button9_Click(object sender, EventArgs e)
        {
            midiEngine.Send(new MidiMessage("1. Internal MIDI", (byte)10, 127, ChannelCommand.NoteOn, 0));
        }

        private void button10_Click(object sender, EventArgs e)
        {
            midiEngine.Send(new MidiMessage("1. Internal MIDI", (byte)11, 127, ChannelCommand.NoteOn, 0));
        }

       
    }
}
