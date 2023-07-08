using System;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class NetworkServer
    {
        private static void RegisterMessage()
        {
            RegisterMessage<CommandMessage>(OnCommandMessage);
            RegisterMessage<PingMessage>(OnPingMessage, false);
        }
        
        /// <summary>
        /// 注册网络消息
        /// </summary>
        internal static void RegisterMessage<T>(Action<ClientEntity, T> handle, bool isAuthority = true) where T : struct, IEvent
        {
            messages[MessageId<T>.Id] = NetworkUtils.Register(handle, isAuthority);
        }

        /// <summary>
        /// 注册网络消息
        /// </summary>
        private static void RegisterMessage<T>(Action<ClientEntity, T, Channel> handle, bool isAuthority = true) where T : struct, IEvent
        {
            messages[MessageId<T>.Id] = NetworkUtils.Register(handle, isAuthority);
        }

        /// <summary>
        /// 当发送一条命令到Transport
        /// </summary>
        private static void OnCommandMessage(ClientEntity client, CommandMessage message, Channel channel)
        {
            if (!client.isReady)
            {
                Debug.LogWarning("Command received while client is not ready.");
                return;
            }

            if (!spawns.TryGetValue(message.netId, out var identity))
            {
                Debug.LogWarning($"Spawned object not found when handling Command message netId = {message.netId}");
                return;
            }

            if (RpcUtils.GetAuthorityByHash(message.functionHash) && identity.client != client)
            {
                Debug.LogWarning($"Command for object without authority netId = {message.netId}");
                return;
            }

            using var reader = NetworkReader.Pop(message.payload);
            identity.HandleRpcEvent(message.componentIndex, message.functionHash, RpcType.ServerRpc, reader, client);
        }

        private static void OnPingMessage(ClientEntity client, PingMessage message)
        {
            PongMessage pongMessage = new PongMessage
            {
                clientTime = message.clientTime,
            };
            client.Send(pongMessage, Channel.Unreliable);
        }
    }
}