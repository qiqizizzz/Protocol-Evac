/*
 * ┌──────────────────────────────────────────────────────────────┐
 * │  描    述: 特效挂点绑定器，按稳定 Id 为角色提供运行时挂点查询
 * │  类    名: VfxSocketBinder.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────┘
 */

using System;
using UnityEngine;

namespace Module.Ability.Vfx
{
    public sealed class VfxSocketBinder : MonoBehaviour
    {
        [Serializable]
        private sealed class VfxSocket
        {
            [SerializeField] private string IdValue;
            [SerializeField] private Transform TransformValue;

            public string Id => IdValue;
            public Transform Transform => TransformValue;
        }

        [SerializeField] private VfxSocket[] Sockets = Array.Empty<VfxSocket>();

        // 按挂点 Id 查找对应 Transform
        public bool TryGetSocket(string socketId, out Transform socket)
        {
            for (int socketIndex = 0; socketIndex < Sockets.Length; socketIndex++)
            {
                VfxSocket vfxSocket = Sockets[socketIndex];
                if (vfxSocket == null || vfxSocket.Id != socketId)
                    continue;

                socket = vfxSocket.Transform;
                return socket != null;
            }

            socket = null;
            return false;
        }
    }
}

