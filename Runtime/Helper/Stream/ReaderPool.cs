// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: jinyijie
// # Version: 1.0.0
// # History: 2024-06-05  21:06
// # Copyright: 2024, jinyijie
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using System.Collections.Generic;
using UnityEngine;

namespace JFramework.Net
{
    [Serializable]
    internal class ReaderPool
    {
        /// <summary>
        /// 写入器列表
        /// </summary>
        private Queue<NetworkWriter> writers = new Queue<NetworkWriter>();

        /// <summary>
        /// 读取器
        /// </summary>
        [SerializeField] private NetworkReader reader = new NetworkReader();

        /// <summary>
        /// 批处理数量
        /// </summary>
        public int Count => writers.Count;

        /// <summary>
        /// 将接收到的数据写入到列表
        /// </summary>
        /// <param name="segment">批处理数据</param>
        /// <returns>返回处理成功</returns>
        public bool AddBatch(ArraySegment<byte> segment)
        {
            if (segment.Count < Const.HeaderSize)
            {
                return false;
            }

            var writer = NetworkWriter.Pop();
            writer.WriteBytes(segment.Array, segment.Offset, segment.Count);
            if (writers.Count == 0)
            {
                reader.Reset(writer);
            }

            writers.Enqueue(writer);
            return true;
        }

        /// <summary>
        /// 读取数据并输出数据和时间戳
        /// </summary>
        /// <param name="newReader"></param>
        /// <returns></returns>
        public bool GetMessage(out NetworkReader newReader)
        {
            newReader = null;
            if (writers.Count == 0)
            {
                return false;
            }

            if (reader.buffer.Count == 0)
            {
                return false;
            }

            if (reader.residue == 0)
            {
                var writer = writers.Dequeue();
                NetworkWriter.Push(writer);
                if (writers.Count > 0)
                {
                    writer = writers.Peek();
                    reader.Reset(writer);
                }
                else
                {
                    return false;
                }
            }
            
            newReader = reader;
            return true;
        }
    }
}