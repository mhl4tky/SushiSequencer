using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;

namespace RbmaSushiPlateDetector
{
    public static class Helpers
    {
        public static void CvtColorHueToBgr(this IplImage src, IplImage dest, IplImage mask)
        {
            for (var i = 0; i < src.Width; i++)
                for (var j = 0; j < src.Height; j++)
                    if (mask[i, j][0] != 0)
                        dest[i, j] = ConvertHsvToRgb(src[i, j][0]*2, 1, 1);
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
            var max = (float) data.Max();

            for (var i = 0; i < data.Length; i++)
            {
                image.DrawLine(i, image.Height, i, (int) (image.Height - (data[i]/max)*image.Height),
                    ConvertHsvToRgb(i*2d, 1, 1), 1);
            }
        }

        public static void SetHistrogramData(this IplImage image, int[] data, CvColor color)
        {
            image.Set(CvColor.Black);
            var max = (float) data.Max();

            for (var i = 0; i < data.Length; i++)
            {
                image.DrawLine(i, image.Height, i, (int) (image.Height - (data[i]/max)*image.Height),
                    color, 1);
            }
        }

        public static void SetHistrogramData(this IplImage image, int[] data, CvColor color, float max)
        {
            image.Set(CvColor.Black);

            for (var i = 0; i < data.Length; i++)
            {
                image.DrawLine(i, image.Height, i, (int) (image.Height - (data[i]/max)*image.Height),
                    color, 1);
            }
        }

        public static void SetHistrogramDataWithSaturation(this IplImage image, int[] data, int[] huedata)
        {
            double h = (double)huedata.IndexOfMaxValue() * 2;

            image.Set(CvColor.Black);
            var max = (float)data.Max();

            for (var i = 0; i < data.Length; i++)
            {
                image.DrawLine(i, image.Height, i, (int)(image.Height - (data[i] / max) * image.Height), ConvertHsvToRgb(h, (double)i / data.Length, 1), 1);
            }
        }

        public static CvRect GetClippingRect(this CvScalar circle, CvRect clipping)
        {
            return new CvRect((int)circle.Val0 - clipping.Height / 2 + clipping.X, (int)circle.Val1 - clipping.Width / 2 + clipping.Y, 
                clipping.Height, clipping.Width);
        }

        public static void GetCircles(this IplImage image, int blur, double dp, int minRadius, int maxRadius,
            out CvMat circles)
        {
            circles = Cv.CreateMat(200, 1, MatrixType.F32C3);
            var gray = new IplImage(image.Size, BitDepth.U8, 1);

            image.CvtColor(gray, ColorConversion.BgrToGray);
            gray.Smooth(gray, SmoothType.Median, blur);

            Cv.HoughCircles(gray, circles, HoughCirclesMethod.Gradient, dp, 100, 100, 100, minRadius, maxRadius);

            gray.Dispose();
        }

        public static int IndexOfMaxValue(this int[] array)
        {
            return array.ToList().IndexOf(array.Max());
        }

        public static float[][] HistHues;
        public static float[][] HistSats;
        public static string[] Names;

        public static void GetClosestColor(int[] histHue, int[] histSat, out int index, out float minDistance)
        {
            index = -1;
            minDistance = float.MaxValue;
            for (var i = 0; i < HistHues.Length; i++)
            {
                var d = _distance(histHue, HistHues[i]);
                d += _distance(histSat, HistSats[i]);
                if (d < minDistance)
                {
                    minDistance = d;
                    index = i;
                }
            }
            //Console.WriteLine(Names[index] + " " + minDistance);
        }

        public static CvPoint _offset(this CvPoint p, int x, int y)
        {
            return new CvPoint(p.X + x, p.Y + y);
        }

        private static float _distance(int[] a, float[] b)
        {
            double distance = 0;
            for (var i = 0; i < a.Length; i++)
            {
                distance += (a[i]-b[i]) * (a[i]-b[i]);
            }
            distance = Math.Sqrt(distance);
            return (float) distance;
        }

        public static CvColor ConvertHsvToRgb(double hue, double saturation, double value)
        {
            var hi = Convert.ToInt32(Math.Floor(hue/60))%6;
            var f = hue/60 - Math.Floor(hue/60);

            value = value*255;
            var v = Convert.ToInt32(value);
            var p = Convert.ToInt32(value*(1 - saturation));
            var q = Convert.ToInt32(value*(1 - f*saturation));
            var t = Convert.ToInt32(value*(1 - (1 - f)*saturation));

            switch (hi)
            {
                case 0:
                    return new CvColor(v, t, p);
                case 1:
                    return new CvColor(q, v, p);
                case 2:
                    return new CvColor(p, v, t);
                case 3:
                    return new CvColor(p, q, v);
                case 4:
                    return new CvColor(t, p, v);
                default:
                    return new CvColor(v, p, q);
            }
        }

        public static T[][] New2DArray<T>(int x, int y)
            where T : new()
        {
            var array = new T[x][];
            for (var i = 0; i < x; i++)
            {
                array[i] = new T[y];

                for (var j = 0; j < y; j++)
                    array[i][j] = new T();
            }
            return array;
        }
    }

}


