using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using OpenCvSharp;

namespace OpenCV_Test
{
    public partial class Form1 : Form
    {
        private const int MinRadius = 50;
        private const int MaxRadius = 65;
        private static double _dp = 1.8d;
        private static int _blur = 1;
        private static CvWindow _window;
        private static CvRect _clipping = new CvRect(560, 108, 210, 172);
        private static CvMat _circles;
        private static readonly int[] PlateColorIndeces = { 5, 27, 90, 118 };
        private static readonly string[] PlateNames = { "Red", "Yellow", "Turquoise", "Purple" };
        private static readonly CvColor[] PlateColors = {CvColor.Red, CvColor.Yellow, CvColor.Turquoise, CvColor.Purple};
        private static CvColor _currColor = CvColor.White;
        private static CvFont _textFont = new CvFont(FontFace.HersheyPlain, 1f, 1f);
        private static IplImage _masked = new IplImage(new CvSize(200, 200), BitDepth.U8, 3);
        private static IplImage _mask = new IplImage(new CvSize(200, 200), BitDepth.U8, 3);
        private static IplImage _hsv = new IplImage(new CvSize(200, 200), BitDepth.U8, 3);
        private static IplImage _h3 = new IplImage(new CvSize(200, 200), BitDepth.U8, 3);
        private static IplImage _s3 = new IplImage(new CvSize(200, 200), BitDepth.U8, 3);
        private static IplImage _detector = new IplImage(new CvSize(200, 200), BitDepth.U8, 3);
        private static IplImage _histogramHue = new IplImage(new CvSize(180, 200), BitDepth.U8, 3);
        private static IplImage _histogramSat = new IplImage(new CvSize(260, 200), BitDepth.U8, 3);
        private static IplImage _histogramR = new IplImage(new CvSize(260, 200), BitDepth.U8, 3);
        private static IplImage _histogramG = new IplImage(new CvSize(260, 200), BitDepth.U8, 3);
        private static IplImage _histogramB = new IplImage(new CvSize(260, 200), BitDepth.U8, 3);
        private static FixedSizedList<double> _previousYValues = new FixedSizedList<double> { Limit = 10 };

        public Form1()
        {
            InitializeComponent();

            _window = new CvWindow("OpenCV1");
            _h3.Set(CvColor.Black);
            _histogramHue.Set(CvColor.Black);
            _masked.Set(CvColor.Black);
            _mask.Set(CvColor.Black);
            _mask.Circle(100, 100, MaxRadius, CvColor.White, -1);
            _mask.Circle(100, 100, MinRadius, CvColor.Black, -1);
             
            var openCv = new Thread(OpenCv);
            openCv.Start();

            Closing += (sender, args) => openCv.Abort();
            trackBar1.Scroll += (sender, args) => _dp = trackBar1.Value * 0.2d + 1d;
            trackBar2.Scroll += (sender, args) => _blur = trackBar2.Value * 2 + 1;           
        }

        private static void OpenCv(object o)
        {
            var cap = CvCapture.FromFile(@"C:\Users\michael.hlatky\Documents\GitHub\SushiSequencer\sushi.avi");

            while (CvWindow.WaitKey(1) < 0)
            {
                var src = cap.QueryFrame();

                if (src == null) break;

                //cut out a small piece from the original, let the circle detection run
                src.GetSubImage(_clipping).GetCircles(_blur, _dp, MinRadius, MaxRadius, out _circles, out _detector); 

                if (_circles.Any())
                {
                    //check if all previous center y coordinates where larger than the new one
                    if (_previousYValues.All(val => val > _circles[0].Val1)) 
                    {
                        //get the image from the original so that the circle is centered
                        var aroundDetectedCircle = src.GetSubImage(_circles.GetClippingRect(_clipping));

                        //mask the image
                        aroundDetectedCircle.And(_mask, _masked);

                        //convert to HSV
                        _masked.CvtColor(_hsv, ColorConversion.BgrToHsv);
                        _hsv.CvtColorHueToBgr(_h3, _mask);
                        //_hsv.CvtColorSaturationToBgr(_s3, _mask);

                        //compute the histogram
                        var histogramHueData = _hsv.Histogram(0, 180, _mask);
                        //var histogramSaturationData = _hsv.Histogram(1, 256, _mask);

                        //var rData = _masked.Histogram(0, 256, _mask);
                        //var gData = _masked.Histogram(1, 256, _mask);
                        //var bData = _masked.Histogram(2, 256, _mask);

                        //var maxRgb = Math.Max(rData.Max(), Math.Max(gData.Max(), bData.Max()));

                        _histogramHue.SetHistrogramData(histogramHueData);
                        //_histogramSat.SetHistrogramData(histogramSaturationData, CvColor.White);
                        //_histogramR.SetHistrogramData(rData, CvColor.Red, maxRgb);
                        //_histogramG.SetHistrogramData(gData, CvColor.Green, maxRgb);
                        //_histogramB.SetHistrogramData(bData, CvColor.Blue, maxRgb);

                        //get the closest match from the histogram peak index
                        var color = PlateColorIndeces.GetClosestColor(histogramHueData.IndexOfMaxValue());
                        _currColor = PlateColors[color];

                        _masked.PutText(PlateNames[color], new CvPoint(10, 184), _textFont, CvColor.White);
                    }

                    _previousYValues.Add(_circles[0].Val1);

                    src.DrawCircle((int)_circles[0].Val0 + _clipping.X, (int)_circles[0].Val1 + _clipping.Y, MinRadius, _currColor, 2);
                    src.DrawCircle((int)_circles[0].Val0 + _clipping.X, (int)_circles[0].Val1 + _clipping.Y, MaxRadius, _currColor, 2);
                    //_detector.DrawCircle((int)_circles[0].Val0, (int)_circles[0].Val1, MinRadius, _currColor, 2);
                    //_detector.DrawCircle((int)_circles[0].Val0, (int)_circles[0].Val1, MaxRadius, _currColor, 2);
                }
                else
                    _previousYValues.Add(int.MaxValue);

                src.DrawRect(_clipping, CvColor.Red, 2);
                src.PutText("dp: " + _dp, new CvPoint(10, 16), _textFont, CvColor.White);
                src.PutText("blur: " + _blur, new CvPoint(10, 36), _textFont, CvColor.White);
                //src.DrawImage(src.Width - _detector.Width, src.Height - _detector.Height - 200 - 20, _detector.Width, _detector.Height, _detector);
                src.DrawImage(src.Width - 200, src.Height - 200, 200, 200, _masked);
                src.DrawImage(src.Width - 200 - _histogramHue.Width - 20, src.Height - _histogramHue.Height, _histogramHue.Width, _histogramHue.Height, _histogramHue);
                //src.DrawImage(src.Width - 200 - _histogramSat.Width - 20, src.Height - 2 * _histogramSat.Height - 20, _histogramSat.Width, _histogramSat.Height, _histogramSat);
                src.DrawImage(src.Width - 200 - _histogramHue.Width - _h3.Width - 40, src.Height - _h3.Height, _h3.Width, _h3.Height, _h3);
                //src.DrawImage(src.Width - 200 - _histogramSat.Width - _h3.Width - 40, src.Height - 2 * _h3.Height - 20, _h3.Width, _h3.Height, _s3);

                //src.DrawImage(0, src.Height - 3 * _histogramR.Height, _histogramR.Width, _histogramR.Height, _histogramR);
                //src.DrawImage(0, src.Height - 2 * _histogramR.Height, _histogramR.Width, _histogramR.Height, _histogramG);
                //src.DrawImage(0, src.Height - 1 * _histogramR.Height, _histogramR.Width, _histogramR.Height, _histogramB);
                
                _window.Image = src;
            }
        }
    }

    public static class Helpers
    {
        public static void CvtColorHueToBgr(this IplImage src, IplImage dest, IplImage mask)
        {
            for (var i = 0; i < src.Width; i++)
                for (var j = 0; j < src.Height; j++)
                    if (mask[i, j][0] != 0)
                        dest[i, j] = ConvertHsvToRgb(src[i, j][0] * 2, 1, 1);
        }

        public static void CvtColorSaturationToBgr(this IplImage src, IplImage dest, IplImage mask)
        {
            for (var i = 0; i < src.Width; i++)
                for (var j = 0; j < src.Height; j++)
                    if (mask[i, j][0] != 0)
                        dest[i, j] = ConvertHsvToRgb(0, src[i, j][1], 1);

            var mono = new IplImage(dest.Size, BitDepth.U8, 1);
            Cv.CvtColor(dest, mono, ColorConversion.BgrToGray);
            Cv.CvtColor(mono, dest, ColorConversion.GrayToBgr);
        }

        public static int[] Histogram(this IplImage image, int channel, int size, IplImage mask)
        {
            var hist = new int[size];

            for (var i = 0; i < image.Height; i++)
                for (var j = 0; j < image.Width; j++)
                    if (mask[i, j][0] != 0)
                    {
                        var index = (int) Math.Round(image[i, j][channel]);
                        //if (index > size - 1) index = size - 1;
                        hist[index]++;
                    }
                        

            return hist;
        }

        public static void SetHistrogramData(this IplImage image, int[] data)
        {
            image.Set(CvColor.Black);
            var max = (float)data.Max();

            for (var i = 0; i < data.Length; i++)
            {
                image.DrawLine(i, image.Height, i, (int)(image.Height - (data[i] / max) * image.Height),
                    ConvertHsvToRgb(i * 2d, 1, 1), 1);
            }
        }

        public static void SetHistrogramData(this IplImage image, int[] data, CvColor color)
        {
            image.Set(CvColor.Black);
            var max = (float) data.Max();

            for (var i = 0; i < data.Length; i++)
            {
                image.DrawLine(i + 2, image.Height, i + 2, (int) (image.Height - (data[i]/max)*image.Height),
                    color, 1);
            }
        }

        public static void SetHistrogramData(this IplImage image, int[] data, CvColor color, float max)
        {
            image.Set(CvColor.Black);

            for (var i = 0; i < data.Length; i++)
            {
                image.DrawLine(i + 2, image.Height, i + 2, (int)(image.Height - (data[i] / max) * image.Height),
                    color, 1);
            }
        }

        public static CvRect GetClippingRect(this CvMat circles, CvRect clipping)
        {
            return new CvRect((int) circles[0].Val0 - 100 + clipping.X, (int) circles[0].Val1 - 100 + clipping.Y, 200, 200);
        }

        public static void GetCircles(this IplImage image, int blur, double dp, int minRadius, int maxRadius, out CvMat circles, out IplImage detector)
        {
            circles = Cv.CreateMat(200, 1, MatrixType.F32C3);
            var gray = new IplImage(image.Size, BitDepth.U8, 1);
            
            image.CvtColor(gray, ColorConversion.BgrToGray);
            gray.Smooth(gray, SmoothType.Median, blur);
            
            Cv.HoughCircles(gray, circles, HoughCirclesMethod.Gradient, dp, int.MaxValue, 100, 100, minRadius, maxRadius);
            
            detector = new IplImage(image.Size, BitDepth.U8, 3);
            gray.CvtColor(detector, ColorConversion.GrayToBgr);
        }

        public static int IndexOfMaxValue(this int[] array)
        {
            return array.ToList().IndexOf(array.Max());
        }

        public static int GetClosestColor(this int[] plateColors, int n)
        {
            var minDiff = int.MaxValue;
            var minIndex = -1;
            for (var i = 0; i < plateColors.Count(); i++)
            {
                var diff = Math.Min(Math.Abs(plateColors[i] - n), Math.Abs(plateColors[i] + 180 - n));
                if (diff >= minDiff) continue;
                minIndex = i;
                minDiff = diff;
            }
            return minIndex;
        }

        public static CvColor ConvertHsvToRgb(double hue, double saturation, double value)
        {
            var hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            var f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            var v = Convert.ToInt32(value);
            var p = Convert.ToInt32(value * (1 - saturation));
            var q = Convert.ToInt32(value * (1 - f * saturation));
            var t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return new CvColor(v, t, p);
            if (hi == 1)
                return new CvColor(q, v, p);
            if (hi == 2)
                return new CvColor(p, v, t);
            if (hi == 3)
                return new CvColor(p, q, v);
            if (hi == 4)
                return new CvColor(t, p, v);
            else
                return new CvColor(v, p, q);
        }
    }

    public class FixedSizedList<T>
    {
        readonly List<T> _list = new List<T>();

        public int Limit { get; set; }
        public void Add(T obj)
        {
            _list.Add(obj);
            while (_list.Count > Limit)
                _list.RemoveAt(0);
        }

        public bool All(Func<T,bool> predicate)
        {
            return _list.All(predicate);
        }
    } 
}