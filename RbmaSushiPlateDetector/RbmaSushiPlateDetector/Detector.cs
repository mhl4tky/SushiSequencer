using System;
using System.Linq;
using OpenCvSharp;
using Avt.Mako;

namespace RbmaSushiPlateDetector
{
    public class DetectedEventArgs : EventArgs
    {
        public int Color { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public ulong Frame { get; set; }
        public float Distance { get; set; }
    }

    public class Detector
    {
        public enum DetectedColors
        {
            Red, Yellow, Turquoise, Purple
        }

        private static int _minRadius = 30;
        private static int _maxRadius = 48;

        public static int MinRadius { get { return _minRadius; } set { _minRadius = value; Console.WriteLine(_minRadius); _initMask(); } }
        public static int MaxRadius { get { return _maxRadius; } set { _maxRadius = value; Console.WriteLine(_maxRadius); _initMask(); } }
        public static double Dp = 1.8d;
        public static int Blur = 1;
        public static CvRect Clipping = new CvRect(75, 25, 150, 150);
        private static CvMat _circles;
        private static CvColor _currColor = CvColor.White;
        private static CvFont _textFont = new CvFont(FontFace.HersheyPlain, 1f, 1f);
        private static IplImage _masked = new IplImage(new CvSize(150, 150), BitDepth.U8, 3);
        private static IplImage _mask = new IplImage(new CvSize(150, 150), BitDepth.U8, 3);
        private static IplImage _hsv = new IplImage(new CvSize(150, 150), BitDepth.U8, 3);
        private static IplImage _h3 = new IplImage(new CvSize(150, 150), BitDepth.U8, 3);
        private static IplImage _s3 = new IplImage(new CvSize(150, 150), BitDepth.U8, 3);
        private static IplImage _histogramHue = new IplImage(new CvSize(180, 200), BitDepth.U8, 3);
        private static IplImage _histogramSaturation = new IplImage(new CvSize(256, 200), BitDepth.U8, 3);
        private static IplImage _aroundDetectedCircle = new IplImage(150, 150, BitDepth.U8, 3);
        private static CvWindow _debug1 = new CvWindow();
        private static CvWindow _debug2 = new CvWindow();
        private static CvWindow _debug3 = new CvWindow();
        private static CvWindow _debug4 = new CvWindow();
        private static CvWindow _debug5 = new CvWindow();

        private static CvPoint p1 = new CvPoint(10, 16);
        private static CvPoint p2 = new CvPoint(10, 36);
        private static CvPoint p3 = new CvPoint(10, 184);

        public static bool _save = false;

        public event EventHandler<DetectedEventArgs> Detected;

        public static Detector Instance { get; private set; }

        public Detector()
        {
            Instance = this;

            _initMask();

            _loadData();

        }

        internal static void _loadData()
        {
            var data = Data.Load();
            Helpers.HistHues = data.Hues;
            Helpers.HistSats = data.Sats;
            Helpers.Names = data.Names;
        }

         private static void _initMask()
        {
            _h3.Set(CvColor.Black);
            _s3.Set(CvColor.Black);
            _histogramHue.Set(CvColor.Black);
            _masked.Set(CvColor.Black);
            _mask.Set(CvColor.Black);
            _mask.Circle(_mask.Width / 2, _mask.Width / 2, MaxRadius - 5, CvColor.White, -1);
            _mask.Circle(_mask.Width / 2, _mask.Width / 2, MinRadius - 5, CvColor.Black, -1);
        }

        private static int n = 0;

        public static void NewFrame(IplImage src, ulong frame)
        {
            if (src == null) return;

            //cut out a small piece from the original, let the circle detection run
            src.GetSubImage(Clipping).GetCircles(Blur, Dp, MinRadius, MaxRadius, out _circles);

            if (_circles.Any())
            {
                foreach (var circle in _circles)
                {
                    //get the image from the original so that the circle is centered
                    var rect = circle.GetClippingRect(Clipping);
                    _aroundDetectedCircle = src.GetSubImage(rect);

                    //check whether we got a full image
                    //if (_aroundDetectedCircle.Width == _mask.Width && _aroundDetectedCircle.Height == _mask.Height)
                    {
                        //mask the image with the circular mask
                        _aroundDetectedCircle.And(_mask, _masked);

                        //convert to HSV
                        _masked.CvtColor(_hsv, ColorConversion.BgrToHsv);
                        
                        if (_save)
                            _masked.SaveImage("images/" + (n++).ToString("0000") + ".png");


                        _hsv.CvtColorHueToBgr(_h3, _mask);
                        _hsv.CvtColorSaturationToBgr(_s3, _mask);

                        //compute the histogram
                        var histogramHueData = _hsv.Histogram(0, 180, _mask);
                        _histogramHue.SetHistrogramData(histogramHueData);

                        var histogramSaturationData = _hsv.Histogram(1, 256, _mask);
                        _histogramSaturation.SetHistrogramData(histogramSaturationData, CvColor.White);

                        //get the closest match from the histogram peak index
                        var color = -1;
                        var distance = 0.0f;
                        Helpers.GetClosestColor(histogramHueData, histogramSaturationData, out color, out distance);

                       // _currColor = PlateColors[color];

                        Instance.Detected.SafeInvoke(Instance, new DetectedEventArgs
                        {
                            Color = color,
                            X = circle.Val0,
                            Y = circle.Val1,
                            Frame = frame,
                            Distance =  distance
                        });

                        _masked.PutText(Helpers.Names[color], p3, _textFont, CvColor.White);

                    }

                    
                }
                foreach (var circle in _circles)
                {
                    src.DrawCircle((int)circle.Val0 + Clipping.X, (int)circle.Val1 + Clipping.Y, MinRadius, _currColor, 2);
                    src.DrawCircle((int)circle.Val0 + Clipping.X, (int)circle.Val1 + Clipping.Y, MaxRadius, _currColor, 2);
                }
               
            }

            src.DrawRect(Clipping, CvColor.Black, 2);
            src.PutText("Dp: " + Dp, p1, _textFont, CvColor.White);
            src.PutText("Blur: " + Blur, p2, _textFont, CvColor.White);

            _debug1.Image = _masked;
            _debug2.Image = _histogramHue;
            _debug3.Image = _h3;
            _debug4.Image = _s3;
            _debug5.Image = _histogramSaturation;
        }
    }
}

