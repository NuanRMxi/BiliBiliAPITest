using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BiliBiliAPITest
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            byte[] dll = Properties.Resources.websocket_sharp_core;
            File.WriteAllBytes("websocket-sharp-core.dll", dll);
            dll = Properties.Resources.Newtonsoft_Json;
            File.WriteAllBytes("Newtonsoft.Json.dll", dll);
            Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
        }
    }
}
