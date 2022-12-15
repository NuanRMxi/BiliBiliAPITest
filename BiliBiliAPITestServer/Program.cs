using Fleck;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;

//检查本地是否存在Data.json
if (!System.IO.File.Exists("Data.json"))
{
    //不存在则创建
    var json = JsonConvert.DeserializeObject<dynamic>("{}");
    json["WaitAccpetUIDs"] = JsonConvert.DeserializeObject<dynamic>("[]");
    json["AccpetedUIDs"] = JsonConvert.DeserializeObject<dynamic>("[]");
    json["AllData"] = JsonConvert.DeserializeObject<dynamic>("{}");
    System.IO.File.WriteAllText("Data.json", json.ToString());
}

WebSocketServer Main = new WebSocketServer("ws://0.0.0.0:4000");
List<IWebSocketConnection> sockets = new List<IWebSocketConnection>();
List<IWebSocketConnection> accpetsocket = new List<IWebSocketConnection>();
List<IWebSocketConnection> waitingsockets = new List<IWebSocketConnection>();
IDictionary<string, IWebSocketConnection> socketindex = new Dictionary<string, IWebSocketConnection>();
IDictionary<string, string> SocketIndexandUID = new Dictionary<string, string>();
void WebSocketStart()
{
    FleckLog.Level = LogLevel.Info;   
    Main.Start(socket =>
    {
        socket.OnOpen = () =>
        {
            string url = socket.ConnectionInfo.ClientIpAddress.ToString() + ":" + socket.ConnectionInfo.ClientPort.ToString();
            sockets.Add(socket);
            socketindex.Add(url, socket);
            var UploadJson = JsonConvert.DeserializeObject<dynamic>("{}");
            UploadJson["type"] = "GetInfo";
            UploadJson["data"] = JsonConvert.DeserializeObject<dynamic>("{}");
            UploadJson["data"]["time"] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            socket.Send(UploadJson.ToString());
        };
        socket.OnMessage = message =>
        {
            //Console.WriteLine(message);
            readfile:
            dynamic Data;
            try
            {
                Data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText("Data.json"));
            }
            catch (Exception)
            {
                goto readfile;
            }
            dynamic json;
            try
            {
                json = JsonConvert.DeserializeObject<dynamic>(message);
            }
            catch
            {
                var returnjson = JsonConvert.DeserializeObject<dynamic>("{}");
                returnjson["type"] = "close";
                returnjson["data"] = JsonConvert.DeserializeObject<dynamic>("{}");
                returnjson["data"]["info"] = "你个肮脏的黑客(Hacked Code:1)";
                returnjson["data"]["time"] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                socket.Send(returnjson.ToString());
                try
                {
                    accpetsocket.Remove(socket);
                    sockets.Remove(socket);
                    socketindex.Remove(socket.ConnectionInfo.ClientIpAddress + ":" + socket.ConnectionInfo.ClientPort);
                    SocketIndexandUID.Remove(socket.ConnectionInfo.ClientIpAddress + ":" + socket.ConnectionInfo.ClientPort);
                }
                catch
                {
                }
                socket.Close();
                json = JsonConvert.DeserializeObject<dynamic>("{\"type\":\"failed\"}");
            }

            try
            {
                if (json["type"] == "SetInfo")
                {
                    //检查当前socket是否存在于sockets中
                    if (sockets.Contains(socket))//如果存在于未鉴权的socket中,那么就开始鉴权
                    {
                        string md5 = json["data"]["md5"];
                        if (json["data"]["md5"] == "" || json["data"]["md5"] == null)
                        {
                            var returnjson = JsonConvert.DeserializeObject<dynamic>("{}");
                            returnjson["type"] = "close";
                            returnjson["data"] = JsonConvert.DeserializeObject<dynamic>("{}");
                            returnjson["data"]["info"] = "你个肮脏的黑客(Hacked Code:0)";
                            returnjson["data"]["time"] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                            Data["WaitAccpetUIDs"].Add(json["data"]["UID"]);
                            socket.Send(returnjson.ToString());
                            try
                            {
                                accpetsocket.Remove(socket);
                                sockets.Remove(socket);
                                socketindex.Remove(socket.ConnectionInfo.ClientIpAddress + ":" + socket.ConnectionInfo.ClientPort);
                                SocketIndexandUID.Remove(socket.ConnectionInfo.ClientIpAddress + ":" + socket.ConnectionInfo.ClientPort);
                            }
                            catch
                            {
                            }
                            socket.Close();
                        }
                        else if (md5.Length < 32 || md5.Length > 32)
                        {
                            var returnjson = JsonConvert.DeserializeObject<dynamic>("{}");
                            returnjson["type"] = "close";
                            returnjson["data"] = JsonConvert.DeserializeObject<dynamic>("{}");
                            returnjson["data"]["info"] = "你个肮脏的黑客(Hacked Code:0)";
                            returnjson["data"]["time"] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                            socket.Send(returnjson.ToString());
                            try
                            {
                                accpetsocket.Remove(socket);
                                sockets.Remove(socket);
                                socketindex.Remove(socket.ConnectionInfo.ClientIpAddress + ":" + socket.ConnectionInfo.ClientPort);
                                SocketIndexandUID.Remove(socket.ConnectionInfo.ClientIpAddress + ":" + socket.ConnectionInfo.ClientPort);
                            }
                            catch
                            {
                            }
                            socket.Close();
                        }
                        else
                        {
                            accpetsocket.Add(socket);
                            sockets.Remove(socket);
                            Console.WriteLine(json["data"]["md5"].ToString());
                            var returnjson = JsonConvert.DeserializeObject<dynamic>("{}");
                            returnjson["type"] = "open";
                            returnjson["data"] = JsonConvert.DeserializeObject<dynamic>("{}");
                            returnjson["data"]["time"] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                            socket.Send(returnjson.ToString());
                        }
                    }
                    else//否则可能是伪造的客户端
                    {
                        //Console.WriteLine("Hacked!");
                        var returnjson = JsonConvert.DeserializeObject<dynamic>("{}");
                        returnjson["type"] = "close";
                        returnjson["data"] = JsonConvert.DeserializeObject<dynamic>("{}");
                        returnjson["data"]["info"] = "你个肮脏的黑客(Hacked Code:0)";
                        returnjson["data"]["time"] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        socket.Send(returnjson.ToString());
                        try
                        {
                            accpetsocket.Remove(socket);
                            sockets.Remove(socket);
                            socketindex.Remove(socket.ConnectionInfo.ClientIpAddress + ":" + socket.ConnectionInfo.ClientPort);
                            SocketIndexandUID.Remove(socket.ConnectionInfo.ClientIpAddress + ":" + socket.ConnectionInfo.ClientPort);
                        }
                        catch
                        {
                        }
                        socket.Close();
                    }
                }
                if (json["type"] == "MetaData")
                {
                    Console.WriteLine(socket.ConnectionInfo.ClientIpAddress + ":" + socket.ConnectionInfo.ClientPort + " ReturnMetaData");
                    try
                    {
                        waitingsockets.Remove(socket);
                    }
                    catch (Exception)
                    {
                    }

                }
                if (json["type"] == "Statistics_Request")
                {
                    if (accpetsocket.Contains(socket))
                    {
                        JArray jarray = Data["AccpetedUIDs"];
                        List<string> jsonarray = new List<string>();
                        for (int i = 0; i < jarray.Count; i++)
                        {
                            jsonarray.Add(jarray[i].ToString());
                        }
                        //JsonArray jsonarray = jarray;
                        if (jsonarray.Contains(json["data"]["uid"].ToString()))
                        {
                            var returnjson = JsonConvert.DeserializeObject<dynamic>("{}");
                            returnjson["type"] = "Info";
                            returnjson["data"] = JsonConvert.DeserializeObject<dynamic>("{}");
                            returnjson["data"]["time"] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                            returnjson["data"]["info"] = "UID已存在,请点击开始接收";
                            socket.Send(returnjson.ToString());
                        }
                        else
                        {
                            var returnjson = JsonConvert.DeserializeObject<dynamic>("{}");
                            returnjson["type"] = "Info";
                            returnjson["data"] = JsonConvert.DeserializeObject<dynamic>("{}");
                            returnjson["data"]["time"] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                            returnjson["data"]["info"] = "请求发送成功,等待服务器处理";
                            Data["WaitAccpetUIDs"].Add(json["data"]["uid"].ToString());
                            socket.Send(returnjson.ToString());
                        }

                    }
                    else
                    {
                        var returnjson = JsonConvert.DeserializeObject<dynamic>("{}");
                        returnjson["type"] = "close";
                        returnjson["data"] = JsonConvert.DeserializeObject<dynamic>("{}");
                        returnjson["data"]["info"] = "你个肮脏的黑客(Hacked Code:2)";
                        returnjson["data"]["time"] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        socket.Send(returnjson.ToString());
                        try
                        {
                            accpetsocket.Remove(socket);
                            sockets.Remove(socket);
                            socketindex.Remove(socket.ConnectionInfo.ClientIpAddress + ":" + socket.ConnectionInfo.ClientPort);
                            SocketIndexandUID.Remove(socket.ConnectionInfo.ClientIpAddress + ":" + socket.ConnectionInfo.ClientPort);
                        }
                        catch
                        {
                        }
                        socket.Close();
                    }
                }
                if (json["type"] == "Get")
                {
                    if (accpetsocket.Contains(socket))
                    {
                        //Console.WriteLine((Data["AccpetedUIDs"].ToString() + (json["data"]["uid"].ToString()).ToString()));
                        bool isdo = false;
                        for (int i = 0; i < Data["AccpetedUIDs"].Count; i++)
                        {
                            if (Data["AccpetedUIDs"][i] == json["data"]["uid"])
                            {
                                isdo = true;
                            }
                        }
                        if (SocketIndexandUID.ContainsKey(socket.ConnectionInfo.ClientIpAddress + ":" + socket.ConnectionInfo.ClientPort))
                        {
                            isdo = false;
                        }
                        if (isdo)
                        {
                            //回复客户端开始接收
                            var returnjson = JsonConvert.DeserializeObject<dynamic>("{}");
                            returnjson["type"] = "SetAll";
                            returnjson["data"] = JsonConvert.DeserializeObject<dynamic>("{}");
                            returnjson["data"]["time"] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                            returnjson["data"]["json"] = Data["AllData"][json["data"]["uid"].ToString()];
                            socket.Send(returnjson.ToString());
                            SocketIndexandUID.Add(socket.ConnectionInfo.ClientIpAddress + ":" + socket.ConnectionInfo.ClientPort, json["data"]["uid"].ToString());
                        }
                        else
                        {
                            //提示客户端UID不存在,请尝试申请
                            var returnjson = JsonConvert.DeserializeObject<dynamic>("{}");
                            returnjson["type"] = "Info";
                            returnjson["data"] = JsonConvert.DeserializeObject<dynamic>("{}");
                            returnjson["data"]["time"] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                            returnjson["data"]["info"] = "UID不存在,请尝试申请或你已经在接收一个uid的统计了";
                            socket.Send(returnjson.ToString());
                        }
                    }
                    else
                    {
                        var returnjson = JsonConvert.DeserializeObject<dynamic>("{}");
                        returnjson["type"] = "close";
                        returnjson["data"] = JsonConvert.DeserializeObject<dynamic>("{}");
                        returnjson["data"]["info"] = "你个肮脏的黑客(Hacked Code:2)";
                        returnjson["data"]["time"] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        socket.Send(returnjson.ToString());
                        try
                        {
                            accpetsocket.Remove(socket);
                            sockets.Remove(socket);
                            socketindex.Remove(socket.ConnectionInfo.ClientIpAddress + ":" + socket.ConnectionInfo.ClientPort);
                            SocketIndexandUID.Remove(socket.ConnectionInfo.ClientIpAddress + ":" + socket.ConnectionInfo.ClientPort);
                        }
                        catch
                        {
                        }
                        socket.Close();
                    }
                }
            }
            catch (IOException iex)
            {
                Console.WriteLine("在OnMessage中捕获到错误,错误为:" + iex.ToString() + "\n收到的消息原文本为:" + message);

            }
            
            Random rdm = new Random();
            Thread.Sleep(rdm.Next(0, 1000));
            try
            {
                File.WriteAllText("Data.json", Data.ToString());
            }
            catch (Exception)
            {
                //throw;
            }
        };
        socket.OnClose = () =>
        {
            try
            {
                accpetsocket.Remove(socket);
                sockets.Remove(socket);
                socketindex.Remove(socket.ConnectionInfo.ClientIpAddress + ":" + socket.ConnectionInfo.ClientPort);
                SocketIndexandUID.Remove(socket.ConnectionInfo.ClientIpAddress + ":" + socket.ConnectionInfo.ClientPort);
            }
            catch
            {
            }
        };
    });
}
void MetaEvent()
{
    while (true)
    {
        waitingsockets.Clear();
        for (int i = 0; i < accpetsocket.Count; i++)
        {
            waitingsockets.Add(accpetsocket[i]);
        }
        var sendjson = JsonConvert.DeserializeObject<dynamic>("{}");
        sendjson["type"] = "MetaData";
        sendjson["data"] = JsonConvert.DeserializeObject<dynamic>("{}");
        sendjson["data"]["time"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        for (int i = 0; i < accpetsocket.Count; i++)
        {
            accpetsocket[i].Send(sendjson.ToString());
        }
        Thread.Sleep(2000);
        _ = sendjson;
        var returnjson = JsonConvert.DeserializeObject<dynamic>("{}");
        returnjson["type"] = "close";
        returnjson["data"] = JsonConvert.DeserializeObject<dynamic>("{}");
        returnjson["data"]["time"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        returnjson["data"]["info"] = "2000ms内未响应,如能收到此消息,请检查网络设置(Close Code:0)";
        for (int i = 0; i < waitingsockets.Count; i++)
        {
            waitingsockets[i].Send(returnjson.ToString());
            waitingsockets[i].Close();
            try
            {
                accpetsocket.Remove(waitingsockets[i]);
            }
            catch
            {
            }
        }
        Thread.Sleep(60000);
    }
}
string GetBiliUPfollower(string UID)
{
    connect:
    //获取up主的粉丝数
    string url = "https://api.bilibili.com/x/relation/stat?vmid=" + UID;
#pragma warning disable SYSLIB0014 // 类型或成员已过时
    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
#pragma warning restore SYSLIB0014 // 类型或成员已过时
    request.Method = "GET";
    try
    {
        var json = JsonConvert.DeserializeObject<dynamic>(new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd());
        if (json["code"] == 0)
        {
            //Console.WriteLine("UID" + UID + "粉丝数:" + json["data"]["follower"]);
            return json["data"]["follower"].ToString();
        }
        else
        {
            Console.WriteLine("获取UP主粉丝数失败");
            goto connect;
            //return "falid";
        }
    }
    catch (Exception)
    {
        goto connect;
    }

}
void GetFollAndSend()
{
    while (true)
    {
        rdfl:
        dynamic Data;
        try
        {
            Data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText("Data.json"));
        }
        catch (Exception)
        {
            goto rdfl;
            //throw;
        }
        
        for (int i = 0; i < Data["AccpetedUIDs"].Count; i++)
        {
            var returnjson = JsonConvert.DeserializeObject<dynamic>("[]");
            returnjson.Add(GetBiliUPfollower(Data["AccpetedUIDs"][i]));
            returnjson.Add(DateTime.Now.ToString());
            //var serjson = JsonConvert.SerializeObject(returnjson);
            Data["AllData"][Data["AccpetedUIDs"][i].ToString()].Add(returnjson);
        }
        writefile:
        try
        {
            反序列化:
            string json = Data.ToString();
            try
            {
                Data = JsonConvert.DeserializeObject<dynamic>(json);
            }
            catch (Exception)
            {
                goto 反序列化;
            }
            File.WriteAllText("Data.json", Data.ToString());
        }
        catch (Exception)
        {
            goto writefile;
        }
        for (int i = 0; i < accpetsocket.Count; i++)
        {
            if (SocketIndexandUID.ContainsKey(accpetsocket[i].ConnectionInfo.ClientIpAddress + ":" + accpetsocket[i].ConnectionInfo.ClientPort))
            {
                string clienturl = accpetsocket[i].ConnectionInfo.ClientIpAddress + ":" + accpetsocket[i].ConnectionInfo.ClientPort;
                var sendjson = JsonConvert.DeserializeObject<dynamic>("{}");
                sendjson["type"] = "SetAdd";
                sendjson["data"] = JsonConvert.DeserializeObject<dynamic>("{}");
                sendjson["data"]["time"] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                sendjson["data"]["follow"] = Data["AllData"][SocketIndexandUID[clienturl]][Data["AllData"][SocketIndexandUID[clienturl]].Count - 1];
                accpetsocket[i].Send(sendjson.ToString());

            }
        }
        Thread.Sleep(5000);
    }
}
Thread server = new Thread(WebSocketStart);
Thread ClientCheck = new Thread(MetaEvent);
Thread GetFollower = new Thread(GetFollAndSend);
server.Start();
ClientCheck.Start();
GetFollower.Start();
while (true)
{
    //无限读取控制台,所有指令都在这里
    string cmd = Console.ReadLine();
    var Data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText("Data.json"));
    if (cmd == "exit")
    {
        Environment.Exit(0);
    }
    else if (cmd == "help")
    {
        Console.WriteLine("exit:退出程序");
        Console.WriteLine("help:显示帮助");
        Console.WriteLine("list:显示当前连接的客户端");
        Console.WriteLine("clear:清空控制台");
        Console.WriteLine("uidaccpet:设置uid白名单");
        Console.WriteLine("uidremove:移除uid白名单");
        Console.WriteLine("uidlist:显示未同意uid名单");
    }
    else if (cmd == "list")
    {
        Console.WriteLine("当前连接的客户端:");
        for (int i = 0; i < accpetsocket.Count; i++)
        {
            Console.WriteLine(accpetsocket[i].ConnectionInfo.ClientIpAddress + ":" + accpetsocket[i].ConnectionInfo.ClientPort);
        }
    }
    else if (cmd == "clear")
    {
        Console.Clear();
    }
    else if (cmd == "uidaccpet")
    {
        Console.WriteLine("请输入要允许的UID:");
        string uid = Console.ReadLine();
        List<string> abab = new();
        for (int i = 0; i < Data["WaitAccpetUIDs"].Count; i++)
        {
            abab.Add(Data["WaitAccpetUIDs"][i].ToString());
        }
        if (!abab.Contains(uid))
        {
            Console.WriteLine("UID已存在,或没有人申请过这个UID");
        }
        else
        {
            Data["AccpetedUIDs"].Add(uid);
            
            abab.Remove(uid);
            Data["WaitAccpetUIDs"] = JsonConvert.DeserializeObject<dynamic>("[]");
            for (int i = 0; i < abab.Count; i++)
            {
                Data["WaitAccpetUIDs"].Add(abab[i]);
            }
            Data["AllData"][uid] = JsonConvert.DeserializeObject<dynamic>("[]");
            Console.WriteLine("UID添加成功");
        }
    }
    else if (cmd == "uidremove")
    {
        //var Data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText("Data.json"));
        Console.WriteLine("请输入要移除出白名单的UID:");
        string uid = Console.ReadLine();
        List<string> abab = new();
        for (int i = 0; i < Data["AccpetedUIDs"].Count; i++)
        {
            abab.Add(Data["AccpetedUIDs"][i].ToString());
        }
        if (abab.Contains(uid))
        {
            abab.Remove(uid);
            Data["AccpetedUIDs"] = JsonConvert.DeserializeObject<dynamic>("[]");
            for (int i = 0; i < abab.Count; i++)
            {
                Data["AccpetedUIDs"].Add(abab[i]);
            }
            Data["WaitAccpetUIDs"].Add(uid); 
            Console.WriteLine("UID移除成功");
        }
        else
        {
            Console.WriteLine("UID不存在");
        }
    }
    else if (cmd == "uidlist")
    {
        //var Data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText("Data.json"));
        Console.WriteLine("未同意的UID:");
        for (int i = 0; i < Data["WaitAccpetUIDs"].Count; i++)
        {
            Console.WriteLine(Data["WaitAccpetUIDs"][i]);
        }
    }
    else
    {
        Console.WriteLine("未知指令,输入help查看帮助");
    }
    File.WriteAllText("Data.json", Data.ToString());
}

