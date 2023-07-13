using System;
using System.Linq;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class ServerManager
    {
        /// <summary>
        /// 生成物体
        /// </summary>
        internal static void SpawnObjects()
        {
            if (!isActive)
            {
                Debug.LogError($"服务器不是活跃的。");
                return;
            }
            
            NetworkObject[] objects = Resources.FindObjectsOfTypeAll<NetworkObject>();

            foreach (var @object in objects)
            {
                if (NetworkUtils.IsSceneObject(@object) && @object.netId == 0)
                {
                    @object.gameObject.SetActive(true);
                    if (NetworkUtils.IsValidParent(@object))
                    {
                        Spawn(@object.gameObject, @object.connection);
                    }
                }
            }
        }
        
        /// <summary>
        /// 仅在Server和Host能使用，生成物体的方法
        /// </summary>
        /// <param name="obj">生成的游戏物体</param>
        /// <param name="client">客户端Id</param>
        public static void Spawn(GameObject obj, ClientEntity client = null)
        {
            if (!isActive)
            {
                Debug.LogError($"服务器不是活跃的。", obj);
                return;
            }

            if (!obj.TryGetComponent(out NetworkObject @object))
            {
                Debug.LogError($"生成对象 {obj} 没有 NetworkObject 组件", obj);
                return;
            }

            if (spawns.ContainsKey(@object.netId))
            {
                Debug.LogWarning($"网络对象 {@object} 已经被生成。", @object.gameObject);
                return;
            }
            
            @object.connection = client;
            
            if (NetworkManager.mode == NetworkMode.Host)
            {
                @object.isOwner = true;
            }
            
            if (!@object.isServer && @object.netId == 0)
            {
                @object.netId = ++netId;
                @object.isServer = true;
                @object.isClient = ClientManager.isActive;
                spawns[@object.netId] = @object;
                @object.OnStartServer();
            }
            
            SpawnForClient(@object);
        }

        /// <summary>
        /// 遍历所有客户端，发送生成物体的事件
        /// </summary>
        /// <param name="object">传入对象</param>
        private static void SpawnForClient(NetworkObject @object)
        {
            foreach (var client in clients.Values.Where(client => client.isReady))
            {
                SendSpawnEvent(client, @object);
            }
        }
        
        /// <summary>
        /// 服务器向指定客户端发送生成对象的消息
        /// </summary>
        /// <param name="client">指定的客户端</param>
        /// <param name="object">生成的游戏对象</param>
        private static void SendSpawnEvent(ClientEntity client, NetworkObject @object)
        {
            Debug.Log($"服务器为客户端 {client.clientId} 生成 {@object}");
            using var writer = NetworkWriter.Pop();
            var transform = @object.transform;
            SpawnEvent @event = new SpawnEvent
            {
                netId = @object.netId,
                sceneId = @object.sceneId,
                assetId = @object.assetId,
                position = transform.localPosition,
                rotation = transform.localRotation,
                localScale = transform.localScale,
                isOwner = @object.connection == client,
                segment = SerializeNetworkObject(@object, writer)
            };
            client.Send(@event);
        }

        /// <summary>
        /// 序列化网络对象，并将数据转发给客户端
        /// </summary>
        /// <param name="object">网络对象生成</param>
        /// <param name="writer"></param>
        /// <returns></returns>
        private static ArraySegment<byte> SerializeNetworkObject(NetworkObject @object, NetworkWriter writer)
        {
            if (@object.objects.Length == 0) return default;
            @object.SerializeServer(true, writer);
            ArraySegment<byte> segment = writer.ToArraySegment();
            return segment;
        }

        /// <summary>
        /// 将网络对象重置并隐藏
        /// </summary>
        /// <param name="object"></param>
        public static void Despawn(NetworkObject @object)
        {
            spawns.Remove(@object.netId);

            if (NetworkManager.mode == NetworkMode.Host)
            {
                @object.isOwner = false;
                @object.OnStopClient();
                @object.OnNotifyAuthority();
                ClientManager.spawns.Remove(@object.netId);
            }

            @object.OnStopServer();
            @object.Reset();
            DespawnForClient(@object);
        }

        /// <summary>
        /// 向所有客户端发送对象重置的消息
        /// </summary>
        /// <param name="object"></param>
        private static void DespawnForClient(NetworkObject @object)
        {
            foreach (var client in clients.Values)
            {
                SendDespawnEvent(client, @object);
            }
        }
        
        /// <summary>
        /// 服务器给指定客户端移除游戏对象
        /// </summary>
        /// <param name="client">传入指定客户端</param>
        /// <param name="object">传入指定对象</param>
        private static void SendDespawnEvent(ClientEntity client, NetworkObject @object)
        {
            Debug.Log($"服务器为客户端 {client.clientId} 重置 {@object}");
            client.Send(new DespawnEvent(@object.netId));
        }
    }
}