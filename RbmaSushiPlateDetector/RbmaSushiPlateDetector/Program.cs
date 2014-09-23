using System;
using System.Windows.Forms;
using System.Diagnostics;

namespace RbmaSushiPlateDetector
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
