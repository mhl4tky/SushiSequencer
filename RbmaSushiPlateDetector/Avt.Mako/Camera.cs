using System;
using AVT.VmbAPINET;


namespace Avt.Mako
{
    public class Camera : IDisposable
    {
        public event EventHandler<CameraEventArgs> NewFrame;
        private AVT.VmbAPINET.Camera _camera;

        public Camera()
        {
            StartCamera();
        }

        private void StartCamera()
        {
            var vimba = new Vimba();
            var frameArray = new Frame[3];

            vimba.Startup();

            if (vimba.Cameras.Count == 0) return;

            _camera = vimba.Cameras[0];
            _camera.Open(VmbAccessModeType.VmbAccessModeFull);

            _camera.OnFrameReceived += frame =>
            {
                if (VmbFrameStatusType.VmbFrameStatusComplete == frame.ReceiveStatus)
                    NewFrame.SafeInvoke(this,
                        new CameraEventArgs
                        {
                            Buffer = frame.Buffer,
                            Width = (int) frame.Width,
                            Height = (int) frame.Height,
                            Count = frame.FrameID
                        });

                try
                {
                    _camera.QueueFrame(frame);
                }
                catch (VimbaException ಠ_ಠ)
                {
                    Console.WriteLine(ಠ_ಠ);
                }
            };

            var features = _camera.Features;
            var feature = features["PayloadSize"];
            var payloadSize = feature.IntValue;

            for (var index = 0; index < frameArray.Length; ++ index)
            {
                frameArray[index] = new Frame(payloadSize);
                _camera.AnnounceFrame(frameArray[index]);
            }

            _camera.StartCapture();

            foreach (var t in frameArray)
                _camera.QueueFrame(t);

            feature = features["AcquisitionMode"];
            feature.EnumValue = "Continuous";

            feature = features["AcquisitionStart"];
            feature.RunCommand();

            feature = _camera.Features["PixelFormat"];
            feature.EnumValue = "BGR8Packed";
        }

        private void StopCamera()
        {
            var features = _camera.Features;
            var feature = features["AcquisitionStop"];
            feature.RunCommand();

            _camera.EndCapture();
            _camera.FlushQueue();
            _camera.RevokeAllFrames();
            _camera.Close();
        }

        public void Dispose()
        {
            StopCamera();
        }
    }

    public class CameraEventArgs : EventArgs
    {
        public ulong Count { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[] Buffer { get; set; }
    }
}
