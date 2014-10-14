using System;
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
            TextWriter textWriter = new StreamWriter(xmlPath);
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

            using (Stream stream = File.Open(openFileDialog.FileName, FileMode.Open))
            {
                var bin = new XmlSerializer(typeof (Data));
                var data = (Data) bin.Deserialize(stream);
                return data;
            }
        }
    }

    public class Training
    {
        public static Training Instance { get; private set; }

        private static IplImage _image, _hsv, _hist;

        private static IplImage _downsampled = new IplImage(480, 270, BitDepth.U8, 3);
        public Training()
        {
            Instance = this;
        }

        public static void Train()
        {
            var _mask = new IplImage(new CvSize(150, 150), BitDepth.U8, 3);
            _hsv = new IplImage(new CvSize(150, 150), BitDepth.U8, 3);
            _hist = new IplImage(new CvSize(180, 200), BitDepth.U8, 3);
            _mask.Set(CvColor.Black);
            _mask.Circle(_mask.Width / 2, _mask.Width / 2, Detector.MaxRadius - 5, CvColor.White, -1);
            _mask.Circle(_mask.Width / 2, _mask.Width / 2, Detector.MinRadius - 5, CvColor.Black, -1);

            var dirs = System.IO.Directory.GetDirectories(@"C:\Users\michael.hlatky\Documents\GitHub\SushiSequencer\RbmaSushiPlateDetector\RbmaSushiPlateDetector\bin\Debug\images");
            var histsHue = Ni.Libraries.Util.Objects.New2DArray<float>(dirs.Length, 180);
            var histsSat = Ni.Libraries.Util.Objects.New2DArray<float>(dirs.Length, 256);

            var d = 0;
            foreach (var files in dirs.Select(dir => System.IO.Directory.GetFiles(dir)))
            {
                foreach (var file in files)
                {
                    _image = new IplImage(file);

                    _image.CvtColor(_hsv, ColorConversion.BgrToHsv);
                    var histogramHueData = _hsv.Histogram(0, 180, _mask);
                    //Console.WriteLine(histogramHueData.Sum());
                    _hist.SetHistrogramData(histogramHueData);
                    Form1.Instance._window.Image = _hist;

                    var histogramSaturationData = _hsv.Histogram(1, 256, _mask);
                    
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
                Form1.Instance._window.Image = _hist;
                Console.WriteLine(histsHue[d].Sum());

                d++;
            }

            Data dd = new Data();
            dd.Names = dirs.Select(dir => new DirectoryInfo(dir).Name).ToArray();
            dd.Hues = histsHue;
            dd.Sats = histsSat;
            Data.Save(dd);
        }

        public static void Validate()
        {
            var dirs = System.IO.Directory.GetDirectories(@"C:\Users\michael.hlatky\Documents\GitHub\SushiSequencer\RbmaSushiPlateDetector\RbmaSushiPlateDetector\bin\Debug\images");
            var _mask = new IplImage(new CvSize(150, 150), BitDepth.U8, 3);
            _hsv = new IplImage(new CvSize(150, 150), BitDepth.U8, 3);
            _mask.Set(CvColor.Black);
            _mask.Circle(_mask.Width / 2, _mask.Width / 2, Detector.MaxRadius - 5, CvColor.White, -1);
            _mask.Circle(_mask.Width / 2, _mask.Width / 2, Detector.MinRadius - 5, CvColor.Black, -1);

            Detector._loadData();

            foreach (var files in dirs.Select(dir => System.IO.Directory.GetFiles(dir)))
            {
                foreach (var file in files)
                {
                    _image = new IplImage(file);

                    _image.CvtColor(_hsv, ColorConversion.BgrToHsv);
                    var histogramHueData = _hsv.Histogram(0, 180, _mask);
                    var histogramSaturationData = _hsv.Histogram(1, 256, _mask);

                    Form1.Instance._window.Image = _image;

                    
                    var n = 0;
                    var m = 0.0f;
                    Helpers.GetClosestColor(histogramHueData, histogramSaturationData, out n, out m);

                    if (Path.GetFileName( Path.GetDirectoryName( file ) ) !=  Helpers.Names[n])
                        Console.WriteLine( Path.GetFileName( Path.GetDirectoryName( file ) + " " + m.ToString("00000") + " " + Helpers.Names[n]));

                   
                }
            }
        }
    }
}
