﻿// *********************************************************************************
// # Project: Astraia
// # Unity: 6000.3.5f1
// # Author: 云谷千羽
// # Version: 1.0.0
// # History: 2025-01-10 21:01:21
// # Recently: 2025-01-10 21:01:33
// # Copyright: 2024, 云谷千羽
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Astraia.Net
{
    internal class Program
    {
        public static Setting Setting;
        public static Process Process;

        public static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        private async Task MainAsync()
        {
            Log.Info = Info;
            Log.Warn = Warn;
            Log.Error = Error;
            var transport = new Astraia.Transport();
            transport.Awake();
            try
            {
                Log.Info("运行服务器...");
                if (!File.Exists("setting.json"))
                {
                    var contents = JsonConvert.SerializeObject(new Setting(), Formatting.Indented);
                    File.WriteAllText("setting.json", contents);

                    Log.Warn("请将 setting.json 文件配置正确并重新运行。");
                    Console.ReadKey();
                    Environment.Exit(0);
                    return;
                }

                Setting = JsonConvert.DeserializeObject<Setting>(File.ReadAllText("setting.json"));

                Log.Info("加载程序集...");
                Assembly.LoadFile(Path.GetFullPath("Astraia.dll"));
                Assembly.LoadFile(Path.GetFullPath("Astraia.Kcp.dll"));
                
                Log.Info("初始化传输类...");
                Process = new Process(transport);
                
                transport.port = Setting.RestPort;
                transport.OnServerConnect = Process.ServerConnect;
                transport.OnServerReceive = Process.ServerReceive;
                transport.OnServerDisconnect = Process.ServerDisconnect;
                transport.StartServer();

                Log.Info("开始进行传输...");
                if (Setting.UseEndPoint)
                {
                    Log.Info("开启REST服务...");
                    if (!RestUtility.StartServer(Setting.RestPort))
                    {
                        Log.Error("请以管理员身份运行或检查端口是否被占用。");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                Console.ReadKey();
                Environment.Exit(0);
            }

            while (true)
            {
                transport.Update();
                await Task.Delay(Setting.UpdateTime);
            }

            void Info(string message)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(Service.Text.Format("[{0}] {1}", DateTime.Now.ToString("MM-dd HH:mm:ss"), message));
            }

            void Warn(string message)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(Service.Text.Format("[{0}] {1}", DateTime.Now.ToString("MM-dd HH:mm:ss"), message));
            }

            void Error(string message)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(Service.Text.Format("[{0}] {1}", DateTime.Now.ToString("MM-dd HH:mm:ss"), message));
            }
        }
    }
}