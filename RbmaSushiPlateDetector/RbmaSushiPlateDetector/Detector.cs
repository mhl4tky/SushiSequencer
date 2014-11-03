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

        public int H { get; set; }
    }

    public class Detector
    {
        public enum DetectedColors
        {
            Red, Yellow, Turquoise, Purple
        }

        private static int _minRadius = 31;
        private static int _maxRadius = 41;

        public static int MinRadius { get { return _minRadius; } set { _minRadius = value; Console.WriteLine(_minRadius); _initMask(); } }
        public static int MaxRadius { get { return _maxRadius; } set { _maxRadius = value; Console.WriteLine(_maxRadius); _initMask(); } }
        public static double Dp = 2.2d;
        public static int Blur = 1;
        public static CvRect Clipping = new CvRect(40, 50, 150, 150);
        private static CvMat _circles;
        private static CvColor _currColor = CvColor.White;
        private static CvFont _textFont = new CvFont(FontFace.HersheyPlain, 1f, 1f);
        private static CvFont _textFont2 = new CvFont(FontFace.HersheyTriplex, 2f, 2f, 0, 2);
        private static IplImage _masked = new IplImage(new CvSize(150, 150), BitDepth.U8, 3);
        private static IplImage _mask = new IplImage(new CvSize(150, 150), BitDepth.U8, 3);
        private static IplImage _hsv = new IplImage(new CvSize(150, 150), BitDepth.U8, 3);
        private static IplImage _h3 = new IplImage(new CvSize(150, 150), BitDepth.U8, 3);
        private static IplImage _s3 = new IplImage(new CvSize(150, 150), BitDepth.U8, 3);
        private static IplImage _histogramHue = new IplImage(new CvSize(180, 200), BitDepth.U8, 3);
        private static IplImage _histogramSaturation = new IplImage(new CvSize(256, 200), BitDepth.U8, 3);
        private static IplImage _aroundDetectedCircle = new IplImage(150, 150, BitDepth.U8, 3);

        private static CvPoint p1 = new CvPoint(10, 16);
        private static CvPoint p2 = new CvPoint(10, 36);
        private static CvPoint _p3 = new CvPoint(1292 + 20, 241 + 200 + 200 + 90);

        public static bool Save = false;
        private static int _n;

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

        

        public static void NewFrame(IplImage background, IplImage original, IplImage downsampled, ulong frame)
        {
            if (downsampled == null) return;

            //cut out a small piece from the original, let the circle detection run
            downsampled.GetSubImage(Clipping).GetCircles(Blur, Dp, MinRadius, MaxRadius, out _circles);

            if (_circles.Any())
            {
                foreach (var circle in _circles)
                {
                    //get the image from the original so that the circle is centered
                    var rect = circle.GetClippingRect(Clipping);
                    _aroundDetectedCircle = downsampled.GetSubImage(rect);

                    //check whether we got a full image
                    if (_aroundDetectedCircle.Width != _mask.Width || _aroundDetectedCircle.Height != _mask.Height)
                        continue;

                    //mask the image with the circular mask
                    _aroundDetectedCircle.And(_mask, _masked);

                    //convert to HSV
                    _masked.CvtColor(_hsv, ColorConversion.BgrToHsv);
                        
                    if (Save)
                        _masked.SaveImage("images/" + (_n++).ToString("0000") + ".png");

                    //convert to debug images
                    _hsv.CvtColorHueToBgr(_h3, _mask);
                    _hsv.CvtColorSaturationToBgr(_s3, _mask);

                    //compute the histogram
                    var histogramHueData = _hsv.Histogram(0, 180, _mask);
                    _histogramHue.SetHistrogramData(histogramHueData);

                    var histogramSaturationData = _hsv.Histogram(1, 256, _mask);
                    _histogramSaturation.SetHistrogramDataWithSaturation(histogramSaturationData, histogramHueData);

                    //get the closest match from the histogram peak index
                    var result = Helpers.GetClosestColor(histogramHueData, histogramSaturationData);
                    var color = result.Item1;

                    Instance.Detected.SafeInvoke(Instance, new DetectedEventArgs
                    {
                        Color = color,
                        X = circle.Val0,
                        Y = circle.Val1,
                        Frame = frame,
                        Distance =  result.Item2,
                        H = histogramHueData.IndexOfMaxValue()
                    });

                    background.DrawRect(_p3._offset(0, 10), _p3._offset(400, -70), CvColor.Black, -1);

                    if (Helpers.Names[color] != "false")
                        background.PutText(Helpers.Names[color].ToUpper(), _p3, _textFont2, CvColor.White);
                    
                }
                foreach (var circle in _circles)
                {
                    downsampled.DrawCircle((int)circle.Val0 + Clipping.X, (int)circle.Val1 + Clipping.Y, MinRadius, _currColor, 2);
                    downsampled.DrawCircle((int)circle.Val0 + Clipping.X, (int)circle.Val1 + Clipping.Y, MaxRadius, _currColor, 2);
                }
               
            }

            downsampled.DrawRect(Clipping, CvColor.Black, 2);
            downsampled.PutText("Dp: " + Dp, p1, _textFont, CvColor.White);
            downsampled.PutText("Blur: " + Blur, p2, _textFont, CvColor.White);

            background.DrawImage(new CvRect(10,10, original.Width, original.Height), original);
            background.DrawImage(new CvRect(original.Width + 20, 10, downsampled.Width, downsampled.Height), downsampled);

            background.DrawImage(
                new CvRect(background.Width - 450 - 10, background.Height - _masked.Height - 10, _masked.Width, _masked.Height), _masked);

            background.DrawImage(
                new CvRect(background.Width - 450 + _masked.Width - 10, background.Height - _masked.Height - 10, _h3.Width, _h3.Height), _h3);

            background.DrawImage(
                new CvRect(background.Width - 450 + _masked.Width + _h3.Width - 10, background.Height - _masked.Height - 10, _s3.Width, _s3.Height), _s3);

            background.DrawImage(
                new CvRect(original.Width + 20, downsampled.Height + 20, _histogramHue.Width, _histogramHue.Height), _histogramHue);

            background.DrawImage(
                new CvRect(original.Width + 20, downsampled.Height + _histogramHue.Height + 30, _histogramSaturation.Width, _histogramSaturation.Height), _histogramSaturation);
        }
    }
}

