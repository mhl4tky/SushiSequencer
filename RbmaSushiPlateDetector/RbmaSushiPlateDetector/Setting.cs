using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace RbmaSushiPlateDetector
{
    [Serializable]
    public class Setting
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double Dp { get; set; }
        public int Blur { get; set; }
        public int MinRadius { get; set; }
        public int MaxRadius { get; set; }

        public static void SaveSetting()
        {
            Form1.Instance.Setting = new Setting
            {
                X = Detector.Clipping.X,
                Y = Detector.Clipping.Y,
                Width = Detector.Clipping.Width,
                Height = Detector.Clipping.Height,
                Dp = Detector.Dp,
                Blur = Detector.Blur,
                MinRadius = Detector.MinRadius,
                MaxRadius = Detector.MaxRadius
            };

            var saveFileDialog = new SaveFileDialog
            {
                Filter = @"Rbma Sushi Setting|*.rbss",
                Title = @"Save Rbma Sushi Setting"
            };
            saveFileDialog.ShowDialog();

            if (saveFileDialog.FileName == "") return;

            var serializer = new XmlSerializer(typeof(Setting));
            var xmlPath = Path.ChangeExtension(saveFileDialog.FileName, ".rbss");
            TextWriter textWriter = new StreamWriter(xmlPath);
            serializer.Serialize(textWriter, Form1.Instance.Setting);
            textWriter.Flush();
            textWriter.Close();
        }

        public static void LoadSetting()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = @"Rbma Sushi Setting|*.rbss",
                Title = @"Open Rbma Sushi Setting"
            };
            openFileDialog.ShowDialog();

            if (openFileDialog.FileName == "") return;

            using (Stream stream = File.Open(openFileDialog.FileName, FileMode.Open))
            {
                var bin = new XmlSerializer(typeof(Setting));
                Form1.Instance.Setting = (Setting)bin.Deserialize(stream);
            }

            Detector.Clipping.X = Form1.Instance.Setting.X;
            Detector.Clipping.Y = Form1.Instance.Setting.Y;
            Detector.Clipping.Width = Form1.Instance.Setting.Width;
            Detector.Clipping.Height = Form1.Instance.Setting.Height;
            Detector.Dp = Form1.Instance.Setting.Dp;
            Detector.Blur = Form1.Instance.Setting.Blur;
            Detector.MinRadius = Form1.Instance.Setting.MinRadius;
            Detector.MaxRadius = Form1.Instance.Setting.MaxRadius;

            Form1.Instance.SetUi();
        }
    }
}
