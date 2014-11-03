//using Ni.Libraries.Hardware;
//using Ni.Libraries.Util;

namespace RbmaSushiPlateDetector
{
    class Maschine
    {
        //private MaschineMk2 _mk2;

        public Maschine()
        {
            //_mk2 = new MaschineMk2();
            //_mk2.SetRestLeds(new int[31].SetAllValues(127));
        }

        public void SetColor(double hue)
        {
            var c =  Helpers.ConvertHsvToRgb(hue, 1, 1);

            //_mk2.SetPadLeds(new int[16].SetAllValues(c.R), new int[16].SetAllValues(c.G), new int[16].SetAllValues(c.B));
            //_mk2.SetGroupLeds(new int[8].SetAllValues(c.R), new int[8].SetAllValues(c.G), new int[8].SetAllValues(c.B), new int[8].SetAllValues(127));
        }
    }
}
