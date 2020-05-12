using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PylonLiveView
{
    static class PylonLiveView
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch
            {
                throw;
            }
        }
    }
}