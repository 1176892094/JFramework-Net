using System;
using JFramework.Udp;
using UnityEngine;

// ReSharper disable All

namespace JFramework.Net
{
    public sealed partial class NetworkManager : GlobalSingleton<NetworkManager>
    {
        private string sceneName;
        private NetworkMode networkMode;
        [SerializeField] private Transport transport;
        [SerializeField] private bool runInBackground = true;
        public int heartTickRate = 30;
        public int maxConnection = 100;
        public Address address => transport.address;

        protected override void Awake()
        {
            base.Awake();
            SetMode(NetworkMode.None);
        }

        /// <summary>
        /// 设置游戏模式
        /// </summary>
        /// <param name="networkMode">网络模式</param>
        private void SetMode(NetworkMode networkMode)
        {
            this.networkMode = networkMode;
            if (transport == null)
            {
                Debug.LogError("The NetworkManager has no Transport component.");
                return;
            }

            Transport.Instance = transport;
            Application.runInBackground = runInBackground;
        }

        /// <summary>
        /// 开启服务器
        /// </summary>
        /// <param name="isListen">设置false则为单机模式，不进行网络传输</param>
        public void StartServer(bool isListen = true)
        {
            if (NetworkServer.isActive)
            {
                Debug.LogWarning("Server already started.");
                return;
            }

#if UNITY_SERVER
            Application.targetFrameRate = heartRate;
#endif
            SetMode(NetworkMode.Server);
            NetworkServer.StartServer(isListen);
            RegisterServerEvent();
            OnStartServer?.Invoke();
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        public void StopServer()
        {
            if (!NetworkServer.isActive) return;
            OnStopServer?.Invoke();
            NetworkServer.RuntimeInitializeOnLoad();
            networkMode = NetworkMode.None;
            sceneName = "";
        }

        /// <summary>
        /// 开启客户端
        /// </summary>
        /// <param name="uri">不传入Uri则按照默认的address来匹配</param>
        public void StartClient(Uri uri = null)
        {
            if (NetworkClient.isActive)
            {
                Debug.LogWarning("Client already started.");
                return;
            }

            SetMode(NetworkMode.Client);
            RegisterClientEvent();
            NetworkClient.StartClient(uri);
            OnStartClient?.Invoke();
        }

        /// <summary>
        /// 停止客户端
        /// </summary>
        public void StopClient()
        {
            if (networkMode == NetworkMode.None)
            {
                return;
            }

            if (networkMode == NetworkMode.Host)
            {
                OnServerDisconnectInternal(NetworkServer.host);
            }

            NetworkClient.Disconnect();
            OnClientDisconnectInternal();
        }

        /// <summary>
        /// 开启主机
        /// </summary>
        /// <param name="isListen">设置false则为单机模式，不进行网络传输</param>
        public void StartHost(bool isListen = true)
        {
            if (NetworkServer.isActive || NetworkClient.isActive)
            {
                Debug.LogWarning("Server or Client already started.");
                return;
            }

            SetMode(NetworkMode.Host);
            NetworkServer.StartServer(isListen);
            RegisterServerEvent();
            NetworkClient.StartHostClient();
            NetworkServer.OnClientConnect(NetworkServer.host);
            OnStartHost?.Invoke();
            RegisterClientEvent();
            NetworkClient.server.connecting = true;
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

        private void OnApplicationQuit()
        {
            if (NetworkClient.connected)
            {
                StopClient();
            }

            if (NetworkServer.isActive)
            {
                StopServer();
            }

            RuntimeInitializeOnLoad();
        }
    }
}