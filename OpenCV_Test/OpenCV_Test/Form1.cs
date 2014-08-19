using System;
using System.Collections.Generic;
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
        private static readonly int[] PlateColors = { 5, 27, 90, 118 };
        private static readonly string[] PlateNames = { "Red", "Yellow", "Blue", "Purple" };
        private static readonly CvFont TextFont = new CvFont(FontFace.HersheyPlain, 1f, 1f);
        private static readonly IplImage Masked = new IplImage(new CvSize(200, 200), BitDepth.U8, 3);
        private static readonly IplImage Mask = new IplImage(new CvSize(200, 200), BitDepth.U8, 3);
        private static readonly IplImage Hsv = new IplImage(new CvSize(200, 200), BitDepth.U8, 3);
        private static readonly FixedSizedList<double> PreviousYValues = new FixedSizedList<double> { Limit = 10 };

        public Form1()
        {
            InitializeComponent();

            _window = new CvWindow("OpenCV1");
            Cv.Set(Masked, CvColor.Black);
            Cv.Set(Mask, CvColor.Black);
             

            var openCv = new Thread(OpenCv);
            openCv.Start();

            Closing += (sender, args) => openCv.Abort();
            trackBar1.Scroll += (sender, args) => _dp = trackBar1.Value * 0.25d + 1d;
            trackBar2.Scroll += (sender, args) => _blur = trackBar2.Value * 2 + 1;           
        }

        private static void OpenCv(object o)
        {
            var cap = CvCapture.FromFile(@"C:\Users\michael.hlatky\Documents\GitHub\SushiSequencer\sushi.avi");

            while (CvWindow.WaitKey(16) < 0)
            {
                var src = cap.QueryFrame();

                if(src == null) break;

                //cut out a small piece from the original, let the circle detection run
                src.GetSubImage(_clipping).GetCircles(_blur, _dp, MinRadius, MaxRadius, out _circles); 

                if (_circles.Any())
                {
                    //check if all previous center y coordinates where larger than the new one
                    if (PreviousYValues.All(val => val > _circles[0].Val1)) 
                    {
                        //get the image from the original so that the circle is centered
                        var aroundDetectedCircle = src.GetSubImage(_circles.GetClippingRect(_clipping));

                        //mask the image
                        Cv.And(aroundDetectedCircle, Mask, Masked);

                        //convert to HSV
                        Cv.CvtColor(Masked, Hsv, ColorConversion.BgrToHsv); 

                        //get the closest match from the histogram peak index
                        var color = PlateColors.GetClosestColor((Hsv.Histogram(Mask)).IndexOfMaxValue());

                        Masked.PutText(PlateNames[color], new CvPoint(10, 184), TextFont, CvColor.White);
                        
                        src.DrawImage(src.Width - 200, src.Height - 200, 200, 200, Masked);
                    }

                    PreviousYValues.Add(_circles[0].Val1);

                    src.DrawCircle((int)_circles[0].Val0 + _clipping.X, (int)_circles[0].Val1 + _clipping.Y, MinRadius, CvColor.Green, 2);
                    src.DrawCircle((int)_circles[0].Val0 + _clipping.X, (int)_circles[0].Val1 + _clipping.Y, MaxRadius, CvColor.Green, 2);
                }
                else
                    PreviousYValues.Add(int.MaxValue);

                src.DrawRect(_clipping, CvColor.Red, 2);
                src.PutText("dp: " + _dp, new CvPoint(10, 16), TextFont, CvColor.White);
                src.PutText("blur: " + _blur, new CvPoint(10, 36), TextFont, CvColor.White);
                src.DrawImage(src.Width - 200, src.Height - 200, 200, 200, Masked);
                
                _window.Image = src;
            }
        }

        
    }

    public static class Helpers
    {
        public static int[] Histogram(this IplImage image, IplImage mask)
        {
            var hist = new int[256];
            for (var i = 0; i < image.Height; i++)
            {
                for (var j = 0; j < image.Width; j++)
                {
                    if (mask[i, j][0] == 0) continue;

                    hist[(int)Math.Round(image[i, j][0])]++;
                }
            }
            return hist;
        }

        public static CvRect GetClippingRect(this CvMat circles, CvRect clipping)
        {
            return new CvRect((int) circles[0].Val0 - 100 + clipping.X, (int) circles[0].Val1 - 100 + clipping.Y, 200, 200);
        }

        public static void GetCircles(this IplImage image, int blur, double dp, int minRadius, int maxRadius, out CvMat circles)
        {
            circles = Cv.CreateMat(200, 1, MatrixType.F32C3);
            var gray = new IplImage(image.Size, BitDepth.U8, 1);
            image.CvtColor(gray, ColorConversion.BgrToGray);
            Cv.Smooth(gray, gray, SmoothType.Median, blur);
            Cv.HoughCircles(gray, circles, HoughCirclesMethod.Gradient, dp, int.MaxValue, 100, 100, minRadius, maxRadius);
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