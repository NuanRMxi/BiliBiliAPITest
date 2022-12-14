using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Windows.Forms.DataVisualization.Charting;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using System.Runtime;
using WebSocketSharp;
using Newtonsoft.Json.Linq;

namespace BiliBiliAPITest
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }
        WebSocket wss = new WebSocket("ws://127.0.0.1:4000");
        private void button1_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(a =>
            {
                WebSocket ws = new WebSocket("ws://43.135.158.99:4000");
                if (DebugMode.Checked)
                {
                    ws = new WebSocket("ws://127.0.0.1:4000");
                }
                ws.Connect();
                wss = ws;
                ws.OnMessage += Ws_OnMessage;
                ws.OnClose += Ws_OnClose;
                button1.Enabled = false;
            });
            thread.Start();
            
        }

        private void Ws_OnClose(object sender, CloseEventArgs e)
        {
            //throw new NotImplementedException();
            this.Text = "BiliAPITest v114514 Ping:NaN(连接已意外断开)";
            button1.Enabled = true;
        }
        bool waitsetall = false;
        private void Ws_OnMessage(object sender, MessageEventArgs e)
        {
            
            //throw new NotImplementedException();
            //将e.RawData编码为string
            string rawdata = e.Data;
            //解析json
            var json = JsonConvert.DeserializeObject<dynamic>(rawdata);
            //修改Form1的标题
            double ServerTime = json["data"]["time"];
            this.Text = "BiliAPITest v114514 Ping:" + (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - ServerTime ).ToString() + "ms";
            if (json["type"] == "GetInfo")
            {
                var returnjson = JsonConvert.DeserializeObject<dynamic>("{}");
                returnjson["type"] = "SetInfo";
                returnjson["data"] = JsonConvert.DeserializeObject<dynamic>("{}");
                returnjson["data"]["time"] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                //获取程序本体md5
                returnjson["data"]["md5"] = GetMD5HashFromFile(Application.ExecutablePath);
                wss.Send(returnjson.ToString());
            }
            if (json["type"] == "MetaData")
            {
                //新建一个线程
                Thread returnjsontoserver = new Thread(a =>
                {
                    var returnjson = JsonConvert.DeserializeObject<dynamic>("{}");
                    returnjson["type"] = "MetaData";
                    returnjson["data"] = JsonConvert.DeserializeObject<dynamic>("{}");
                    returnjson["data"]["time"] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    wss.Send(returnjson.ToString());
                });
                returnjsontoserver.Start();
            }
            if (json["type"] == "Info")
            {
                Thread thd = new Thread(a =>
                {
                    MessageBox.Show(json["data"]["info"].ToString(), "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
                thd.Start();
            }
            if (json["type"] == "close")
            {
                //弹出MessageBox
                MessageBox.Show(json["data"]["info"].ToString(), "连接断开", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (json["type"] == "SetAdd")
            {
                Thread setadd = new Thread(a =>
                {
                    while (waitsetall) { }
                    int follow = int.Parse(json["data"]["follow"][0].ToString());
                    chart1.ChartAreas[0].AxisY.Maximum = follow + 500;
                    chart1.ChartAreas[0].AxisY.Minimum = follow - 500;
                    chart1.Series[0].Points.AddXY(json["data"]["follow"][1].ToString(), follow);
                    waitsetall = true;
                    while (chart1.Series[0].Points.Count >= 599)
                    {
                        chart1.Series[0].Points.RemoveAt(0);
                    }
                    waitsetall = false;
                });
                setadd.Start();


            }
            if (json["type"] == "SetAll")
            {
                waitsetall = true;
                Thread thd = new Thread(a =>
                {
                    var jsontemp = json["data"];
                    //截取最后600个数据
                    //var last600 = jsontemp["json"].Skip(jsontemp["json"].Count - 600).Take(600);
                    var array = jsontemp["json"];
                    // 向last600中添加元素
                    var last600 = new JArray();
                    if (array.Count > 599)
                    {
                        
                        for (int i = array.Count - 600; i < array.Count; i++)
                        {
                            last600.Add(array[i]);
                        }

                        // subArray包含了JArray的最后600位
                    }
                    ChartArea ctemp = new ChartArea();
                    //ctemp = chart1.ChartAreas[0];
                    Series temp = new Series();
                    //temp = chart1.Series[0];
                    temp.Name = "粉丝数";
                    temp.ChartType = SeriesChartType.StepLine;
                    //temp.ChartArea = ctemp;
                    
                    for (int i = 0; i < last600.Count; i++)
                    {
                        int follow = (int)last600[i][0];
                        ctemp.AxisY.Maximum = follow + 500;
                        ctemp.AxisY.Minimum = follow - 500;
                        temp.Points.AddXY(last600[i][1].ToString(), follow);
                        //Thread.Sleep(20);
                    }
                    chart1.Series[0] = temp;
                    chart1.ChartAreas[0] = ctemp;
                    waitsetall = false;
                });
                thd.Start();
            }

        }
        private static string GetMD5HashFromFile(string fileName)
        {
            try
            {
                File.Copy(fileName, fileName + ".temp");
                FileStream file = new FileStream(fileName + ".temp", FileMode.Open);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();
                File.Delete(fileName + ".temp");
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                //throw new Exception("GetMD5HashFromFile() fail, error:" +ex.Message);
                MessageBox.Show(ex.ToString(), "连接断开", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return "failed";
                
            }
        }



        private void button4_Click(object sender, EventArgs e)
        {
            var json = JsonConvert.DeserializeObject<dynamic>("{}");
            json["type"] = "Statistics_Request";
            json["data"] = JsonConvert.DeserializeObject<dynamic>("{}");
            json["data"]["time"] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            json["data"]["uid"] = UID.Text;
            wss.Send(json.ToString());
        }

        private void button5_Click(object sender, EventArgs e)
        {
            chart1.Series[0].Points.Clear();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var json = JsonConvert.DeserializeObject<dynamic>("{}");
            json["type"] = "Get";
            json["data"] = JsonConvert.DeserializeObject<dynamic>("{}");
            json["data"]["time"] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            json["data"]["uid"] = UID.Text;
            wss.Send(json.ToString());
        }

        private void DebugMode_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
