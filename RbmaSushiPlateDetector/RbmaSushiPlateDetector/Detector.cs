using System;
using System.Linq;
using OpenCvSharp;
using Avt.Mako;

namespace RbmaSushiPlateDetector
{
    public class DetectedEventArgs : EventArgs
    {
        public Detector.DetectedColors Color { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public ulong Frame { get; set; }
    }

    public class Detector
    {
        public enum DetectedColors
        {
            Red, Yellow, Turquoise, Purple
        }

        public static int MinRadius = 65;
        public static int MaxRadius = 85;
        public static double Dp = 1.8d;
        public static int Blur = 1;
        public static CvRect Clipping = new CvRect(75, 25, 200, 200);
        private static CvMat _circles;
        private static readonly int[] PlateColorIndeces = {5, 27, 90, 118};
        private static readonly string[] PlateNames = {"Red", "Yellow", "Turquoise", "Purple"};
        private static readonly CvColor[] PlateColors = {CvColor.Red, CvColor.Yellow, CvColor.Turquoise, CvColor.Purple};
        private static CvColor _currColor = CvColor.White;
        private static CvFont _textFont = new CvFont(FontFace.HersheyPlain, 1f, 1f);
        private static IplImage _masked = new IplImage(new CvSize(200, 200), BitDepth.U8, 3);
        private static IplImage _mask = new IplImage(new CvSize(200, 200), BitDepth.U8, 3);
        private static IplImage _hsv = new IplImage(new CvSize(200, 200), BitDepth.U8, 3);
        private static IplImage _h3 = new IplImage(new CvSize(200, 200), BitDepth.U8, 3);
        private static IplImage _histogramHue = new IplImage(new CvSize(180, 200), BitDepth.U8, 3);
        private static IplImage _aroundDetectedCircle = new IplImage(200, 200, BitDepth.U8, 3);
        private static CvWindow _debug1 = new CvWindow();
        private static CvWindow _debug2 = new CvWindow();
        private static CvWindow _debug3 = new CvWindow();

        private static CvPoint p1 = new CvPoint(10, 16);
        private static CvPoint p2 = new CvPoint(10, 36);
        private static CvPoint p3 = new CvPoint(10, 184);

        private static FixedSizedList<double> _previousValues = new FixedSizedList<double> {Limit = 10}; 

        public event EventHandler<DetectedEventArgs> Detected;

        public static Detector Instance { get; private set; }

        public Detector()
        {
            Instance = this;

            _h3.Set(CvColor.Black);
            _histogramHue.Set(CvColor.Black);
            _masked.Set(CvColor.Black);
            _mask.Set(CvColor.Black);
            _mask.Circle(100, 100, MaxRadius - 10, CvColor.White, -1);
            _mask.Circle(100, 100, MinRadius - 10, CvColor.Black, -1);
            
        }

        public static void NewFrame(IplImage src, ulong frame)
        {
            if (src == null) return;

            //cut out a small piece from the original, let the circle detection run
            src.GetSubImage(Clipping).GetCircles(Blur, Dp, MinRadius, MaxRadius, out _circles);

            if (_circles.Any())
            {
                //get the image from the original so that the circle is centered
                _aroundDetectedCircle = src.GetSubImage(_circles.GetClippingRect(Clipping));

                //check whether we got a full image
                if (_aroundDetectedCircle.Width == _mask.Width && _aroundDetectedCircle.Height == _mask.Height)
                {
                    //mask the image with the circular mask
                    _aroundDetectedCircle.And(_mask, _masked);

                    //convert to HSV
                    _masked.CvtColor(_hsv, ColorConversion.BgrToHsv);
                    _hsv.CvtColorHueToBgr(_h3, _mask);

                    //compute the histogram
                    var histogramHueData = _hsv.Histogram(0, 180, _mask);
                    _histogramHue.SetHistrogramData(histogramHueData);

                    //get the closest match from the histogram peak index
                    var color = PlateColorIndeces.GetClosestColor(histogramHueData.IndexOfMaxValue());
                    _currColor = PlateColors[color];

                    Instance.Detected.SafeInvoke(Instance, new DetectedEventArgs
                    {
                        Color = (DetectedColors)color, 
                        X = _circles[0].Val0, 
                        Y = _circles[0].Val1, 
                        Frame = frame
                    });

                    _masked.PutText(PlateNames[color], p3, _textFont, CvColor.White);
                    _previousValues.Add(_circles[0].Val0);
                    
                }
                
                src.DrawCircle((int)_circles[0].Val0 + Clipping.X, (int)_circles[0].Val1 + Clipping.Y, MinRadius, _currColor, 2);
                src.DrawCircle((int)_circles[0].Val0 + Clipping.X, (int)_circles[0].Val1 + Clipping.Y, MaxRadius, _currColor, 2);
            }

            src.DrawRect(Clipping, CvColor.Black, 2);
            src.PutText("Dp: " + Dp, p1, _textFont, CvColor.White);
            src.PutText("Blur: " + Blur, p2, _textFont, CvColor.White);

            _debug1.Image = _masked;
            _debug2.Image = _histogramHue;
            _debug3.Image = _h3;
        }
    }
}

