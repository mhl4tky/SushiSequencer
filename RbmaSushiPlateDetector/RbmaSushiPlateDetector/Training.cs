﻿using System;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using OpenCvSharp;
using System.IO;

namespace RbmaSushiPlateDetector
{
    [Serializable]
    public class Data
    {
        public float[][] Hues { get; set; }
        public float[][] Sats { get; set; }
        public string[] Names { get; set; }

        public static void Save(Data data)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = @"Jam Prototype Setting|*.data",
                Title = @"Save Jam Prototype Setting"
            };
            saveFileDialog.ShowDialog();

            if (saveFileDialog.FileName == "") return;

            var serializer = new XmlSerializer(typeof(Data));
            var xmlPath = Path.ChangeExtension(saveFileDialog.FileName, ".data");
            var textWriter = new StreamWriter(xmlPath);
            serializer.Serialize(textWriter, data);
            textWriter.Flush();
            textWriter.Close();
        }

        public static Data Load()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = @"Jam Prototype Setting|*.data",
                Title = @"Open Jam Prototype Setting"
            };
            openFileDialog.ShowDialog();

            using (var stream = File.Open(openFileDialog.FileName, FileMode.Open))
            {
                var bin = new XmlSerializer(typeof(Data));
                var data = (Data)bin.Deserialize(stream);
                return data;
            }
        }
    }

    public class Training
    {
        public static Training Instance { get; private set; }

        private static IplImage _image, _hsv, _hist;

        public Training()
        {
            Instance = this;
        }

        public static void Train()
        {
            var mask = new IplImage(new CvSize(150, 150), BitDepth.U8, 3);
            _hsv = new IplImage(new CvSize(150, 150), BitDepth.U8, 3);
            _hist = new IplImage(new CvSize(180, 200), BitDepth.U8, 3);
            mask.Set(CvColor.Black);
            mask.Circle(mask.Width / 2, mask.Width / 2, Detector.MaxRadius - 5, CvColor.White, -1);
            mask.Circle(mask.Width / 2, mask.Width / 2, Detector.MinRadius - 5, CvColor.Black, -1);

            var dirs = Directory.GetDirectories(@"images");
            var histsHue = Helpers.New2DArray<float>(dirs.Length, 180);
            var histsSat = Helpers.New2DArray<float>(dirs.Length, 256);

            Console.WriteLine(@"Training files");

            var d = 0;
            foreach (var dir in dirs)
            {
                Console.WriteLine(@"\tProcessing " + dir);

                var files = Directory.GetFiles(dir);

                foreach (var file in files)
                {
                    _image = new IplImage(file);

                    _image.CvtColor(_hsv, ColorConversion.BgrToHsv);
                    var histogramHueData = _hsv.Histogram(0, 180, mask);
                    _hist.SetHistrogramData(histogramHueData);
                    Form1.Instance.Window.Image = _hist;

                    var histogramSaturationData = _hsv.Histogram(1, 256, mask);

                    for (var i = 0; i < histogramHueData.Length; i++)
                        histsHue[d][i] += histogramHueData[i];

                    for (var i = 0; i < histogramSaturationData.Length; i++)
                        histsSat[d][i] += histogramSaturationData[i];
                }

                for (var i = 0; i < histsHue[0].Length; i++)
                    histsHue[d][i] /= files.Length;

                for (var i = 0; i < histsSat[0].Length; i++)
                    histsSat[d][i] /= files.Length;

                _hist.SetHistrogramData(histsHue[d].Select(x => (int)x).ToArray());
                Form1.Instance.Window.Image = _hist;

                d++;
            }

            Console.WriteLine(@"Training done");

            var data = new Data
            {
                Names = dirs.Select(dir => new DirectoryInfo(dir).Name).ToArray(),
                Hues = histsHue,
                Sats = histsSat
            };
            Data.Save(data);
        }

        public static void Validate()
        {
            var dirs = Directory.GetDirectories(@"images");
            var mask = new IplImage(new CvSize(150, 150), BitDepth.U8, 3);
            _hsv = new IplImage(new CvSize(150, 150), BitDepth.U8, 3);
            mask.Set(CvColor.Black);
            mask.Circle(mask.Width / 2, mask.Width / 2, Detector.MaxRadius - 5, CvColor.White, -1);
            mask.Circle(mask.Width / 2, mask.Width / 2, Detector.MinRadius - 5, CvColor.Black, -1);

            Detector._loadData();

            Console.WriteLine(@"Validating files");

            foreach (var file in dirs.Select(Directory.GetFiles).SelectMany(files => files))
            {
                _image = new IplImage(file);

                _image.CvtColor(_hsv, ColorConversion.BgrToHsv);
                var histogramHueData = _hsv.Histogram(0, 180, mask);
                var histogramSaturationData = _hsv.Histogram(1, 256, mask);

                Form1.Instance.Window.Image = _image;

                var result = Helpers.GetClosestColor(histogramHueData, histogramSaturationData);

                if (Path.GetFileName(Path.GetDirectoryName(file)) != Helpers.Names[result.Item1])
                    Console.WriteLine(@"\t" + Path.GetFileName(Path.GetDirectoryName(file) + " " + result.Item2.ToString("00000") + " " + Helpers.Names[result.Item1]));
            }

            Console.WriteLine(@"Validation done");
        }
    }
}
