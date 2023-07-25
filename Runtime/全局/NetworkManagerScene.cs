using JFramework.Core;
using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkManager
    {
        /// <summary>
        /// 服务器加载场景
        /// </summary>
        public static void LoadScene(string newSceneName)
        {
            if (string.IsNullOrWhiteSpace(newSceneName))
            {
                Debug.LogError("服务器不能加载空场景！");
                return;
            }

            if (NetworkServer.isLoadScene && newSceneName == sceneName)
            {
                Debug.LogError($"服务器已经在加载 {newSceneName} 场景");
                return;
            }

            Debug.Log("服务器开始加载场景");
            foreach (var client in NetworkServer.clients.Values)
            {
                NetworkServer.NotReadyForClient(client);
            }

            OnServerLoadScene?.Invoke(newSceneName);
            sceneName = newSceneName;
            NetworkServer.isLoadScene = true;
            if (NetworkServer.isActive)
            {
                using var writer = NetworkWriter.Pop();
                NetworkMessage.WriteMessage(writer, new SceneMessage(newSceneName));
                foreach (var client in NetworkServer.clients.Values)
                {
                    client.SendMessage(writer.ToArraySegment());
                }
            }

            SceneManager.LoadSceneAsync(newSceneName);
        }

        /// <summary>
        /// 客户端加载场景
        /// </summary>
        internal static void ClientLoadScene(string newSceneName)
        {
            if (string.IsNullOrWhiteSpace(newSceneName))
            {
                Debug.LogError("客户端不能加载空场景！");
                return;
            }

            Debug.Log("客户端器开始加载场景");
            OnClientLoadScene?.Invoke(newSceneName);
            if (NetworkServer.isActive) return; //Host不做处理
            sceneName = newSceneName;
            NetworkClient.isLoadScene = true;
            SceneManager.LoadSceneAsync(newSceneName);
        }


        private static void OnLoadComplete(string sceneName)
        {
            switch (mode)
            {
                case NetworkMode.Host:
                    OnServerSceneLoadCompleted();
                    OnClientSceneLoadCompleted();
                    break;
                case NetworkMode.Server:
                    OnServerSceneLoadCompleted();
                    break;
                case NetworkMode.Client:
                    OnClientSceneLoadCompleted();
                    break;
            }
        }

        /// <summary>
        /// 服务器端场景加载完成
        /// </summary>
        private static void OnServerSceneLoadCompleted()
        {
            Debug.Log("服务器加载场景完成");
            NetworkServer.isLoadScene = false;
            NetworkServer.SpawnObjects();
            OnServerSceneChanged?.Invoke(sceneName);
        }

        /// <summary>
        /// 客户端场景加载完成
        /// </summary>
        private static void OnClientSceneLoadCompleted()
        {
            NetworkClient.isLoadScene = false;
            if (!NetworkClient.isConnect) return;
            Debug.Log("客户端加载场景完成");
            if (!NetworkClient.isReady)
            {
                NetworkClient.Ready();
            }

            OnClientSceneChanged?.Invoke(SceneManager.localScene);
        }
    }
}