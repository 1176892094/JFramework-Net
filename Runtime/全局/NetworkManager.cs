using System;
using JFramework.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkManager : MonoBehaviour
    {
        /// <summary>
        /// NetworkManager 单例
        /// </summary>
        public static NetworkManager Instance;

        /// <summary>
        /// 服务器场景
        /// </summary>
        private static string sceneName;

        /// <summary>
        /// 消息发送率
        /// </summary>
        internal static float sendRate => Instance.tickRate < int.MaxValue ? 1f / Instance.tickRate : 0;

        /// <summary>
        /// 网络传输组件
        /// </summary>
        [FoldoutGroup("网络管理器"), SerializeField]
        private Transport transport;

        /// <summary>
        /// 网络发现组件
        /// </summary>
        [FoldoutGroup("网络管理器"), SerializeField]
        public NetworkDiscovery discovery;

        /// <summary>
        /// 网络设置和预置体
        /// </summary>
        [FoldoutGroup("网络管理器"), SerializeField]
        internal NetworkSetting setting;

        /// <summary>
        /// 玩家预置体
        /// </summary>
        [FoldoutGroup("网络管理器"), SerializeField]
        internal GameObject playerPrefab;

        /// <summary>
        /// 心跳传输率
        /// </summary>
        [FoldoutGroup("网络管理器"), SerializeField]
        internal int tickRate = 30;

        /// <summary>
        /// 客户端最大连接数量
        /// </summary>
        [FoldoutGroup("网络管理器"), SerializeField]
        internal uint maxConnection = 100;

        /// <summary>
        /// 传输连接地址
        /// </summary>
        [FoldoutGroup("网络管理器"), ShowInInspector]
        public string address
        {
            get => transport ? transport.address : NetworkConst.Address;
            set => transport.address = transport ? value : NetworkConst.Address;
        }

        /// <summary>
        /// 传输连接端口
        /// </summary>
        [FoldoutGroup("网络管理器"), ShowInInspector]
        public ushort port
        {
            get => transport ? transport.port : NetworkConst.Port;
            set => transport.port = transport ? value : NetworkConst.Port;
        }

        /// <summary>
        /// 网络运行模式
        /// </summary>
        [FoldoutGroup("网络管理器"), ShowInInspector]
        public static NetworkMode mode
        {
            get
            {
                if (NetworkServer.isActive)
                {
                    return NetworkClient.isActive ? NetworkMode.Host : NetworkMode.Server;
                }

                return NetworkClient.isActive ? NetworkMode.Client : NetworkMode.None;
            }
        }

        /// <summary>
        /// 初始化配置传输
        /// </summary>
        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            GlobalManager.OnQuit += OnQuit;
            SceneManager.OnLoadComplete += OnLoadComplete;
           
            if (transport == null)
            {
                Debug.LogError("NetworkManager 没有 Transport 组件。");
                return;
            }

            if (setting == null)
            {
                Debug.LogError("NetworkManager 的 Setting 为空");
                return;
            }

            Application.runInBackground = true;
#if UNITY_SERVER
            Application.targetFrameRate = tickRate;
#endif
            Transport.current = transport;
        }

        /// <summary>
        /// 开启服务器
        /// </summary>
        public void StartServer()
        {
            if (NetworkServer.isActive)
            {
                Debug.LogWarning("服务器已经连接！");
                return;
            }

            sceneName = "";
            NetworkServer.StartServer(true);
            OnStartServer?.Invoke();
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        public void StopServer()
        {
            if (!NetworkServer.isActive)
            {
                Debug.LogWarning("服务器已经停止！");
                return;
            }

            sceneName = "";
            OnStopServer?.Invoke();
            NetworkServer.StopServer();
        }

        /// <summary>
        /// 开启客户端
        /// </summary>
        public void StartClient()
        {
            if (NetworkClient.isActive)
            {
                Debug.LogWarning("客户端已经连接！");
                return;
            }


            NetworkClient.StartClient(address, port);
            NetworkClient.RegisterPrefab(setting.prefabs);
            OnStartClient?.Invoke();
        }

        /// <summary>
        /// 开启客户端 (根据Uri来连接)
        /// </summary>
        /// <param name="uri">传入Uri</param>
        public void StartClient(Uri uri)
        {
            if (NetworkClient.isActive)
            {
                Debug.LogWarning("客户端已经连接！");
                return;
            }

            NetworkClient.StartClient(uri);
            NetworkClient.RegisterPrefab(setting.prefabs);
            OnStartClient?.Invoke();
        }

        /// <summary>
        /// 停止客户端
        /// </summary>
        public void StopClient()
        {
            if (!NetworkClient.isActive)
            {
                Debug.LogWarning("客户端已经停止！");
                return;
            }

            if (mode == NetworkMode.Host)
            {
                NetworkServer.OnServerDisconnected(NetworkServer.connection.clientId);
            }

            OnStopClient?.Invoke();
            NetworkClient.StopClient();
        }

        /// <summary>
        /// 开启主机
        /// </summary>
        /// <param name="isListen">设置false则为单机模式，不进行网络传输</param>
        public void StartHost(bool isListen = true)
        {
            if (NetworkServer.isActive || NetworkClient.isActive)
            {
                Debug.LogWarning("客户端或服务器已经连接！");
                return;
            }

            NetworkServer.StartServer(isListen);
            NetworkClient.StartClient();
            NetworkClient.RegisterPrefab(setting.prefabs);
            OnStartHost?.Invoke();
        }

        /// <summary>
        /// 停止主机
        /// </summary>
        public void StopHost()
        {
            OnStopHost?.Invoke();
            StopClient();
            StopServer();
        }

        /// <summary>
        /// 生成玩家预置体
        /// </summary>
        /// <param name="client"></param>
        internal void SpawnPrefab(ClientEntity client)
        {
            if (client.isSpawn && playerPrefab != null)
            {
                NetworkServer.Spawn(Instantiate(playerPrefab), client);
                client.isSpawn = false;
            }
        }

        /// <summary>
        /// 当程序退出，停止服务器和客户端
        /// </summary>
        private void OnQuit()
        {
            if (NetworkClient.isConnect)
            {
                StopClient();
            }

            if (NetworkServer.isActive)
            {
                StopServer();
            }
            
            RuntimeInitializeOnLoad();
            GlobalManager.OnQuit -= OnQuit;
            SceneManager.OnLoadComplete -= OnLoadComplete;
        }
    }
}