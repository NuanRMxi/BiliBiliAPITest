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
            
            try
            {
                byte[] dll = Properties.Resources.websocket_sharp_core;
                File.WriteAllBytes("websocket-sharp-core.dll", dll);
                dll = Properties.Resources.Newtonsoft_Json;
                File.WriteAllBytes("Newtonsoft.Json.dll", dll);
            }
            catch (Exception)
            {
                //提示用户dll被占用,可能正在多开,你确定要继续运行吗?
                if (MessageBox.Show("dll被占用,可能正在多开,你确定要继续运行吗?", "提示", MessageBoxButtons.YesNo,MessageBoxIcon.Information) == DialogResult.No)
                {
                    return;
                }
            }

            Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
        }
    }
}
