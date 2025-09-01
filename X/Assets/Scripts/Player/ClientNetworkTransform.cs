using DistantLands;
using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Multiplayer.Samples.Utilities.ClientAuthority
{
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        [Header("权限设置")]
        [Tooltip("是否默认使用客户端权威模式")]
        [SerializeField] private bool useClientAuthorityByDefault = false;

        [Tooltip("出生阶段是否强制使用服务器权威（确保出生点生效）")]
        [SerializeField] private bool forceServerAuthorityOnSpawn = true;

        // 用于跟踪是否已完成出生点同步
        private bool m_HasSpawnedSuccessfully = false;

        // 权限控制核心方法
        protected override bool OnIsServerAuthoritative()
        {
            // 出生阶段强制服务器权威，确保出生点生效
            if (forceServerAuthorityOnSpawn && !m_HasSpawnedSuccessfully)
            {
                return true;
            }

            // 出生后根据配置决定权限模式
            return !useClientAuthorityByDefault;
        }

        // 当网络对象生成时调用
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // 注册出生完成事件（由PlayerSpawnHandler调用）
            if (IsOwner)
            {
                var playerSpawner = GetComponent<PlayerSpawnHandler>();
                if (playerSpawner != null)
                {
                    playerSpawner.OnSpawnPositionApplied += MarkSpawnComplete;
                }
                else
                {
                    Debug.LogWarning("ClientNetworkTransform: 未找到PlayerSpawnHandler组件，将在1秒后自动切换权限模式");
                    Invoke(nameof(MarkSpawnComplete), 1f);
                }
            }
        }

        // 标记出生完成，切换到客户端权威（如果启用）
        public void MarkSpawnComplete()
        {
            m_HasSpawnedSuccessfully = true;
            Debug.Log($"ClientNetworkTransform: 出生点同步完成，切换到{(useClientAuthorityByDefault ? "客户端" : "服务器")}权威模式");
        }

        // 确保服务器可以在出生阶段强制传送
       new public void Teleport(Vector3 newPosition, Quaternion newRotation, Vector3 newScale)
        {
            // 出生阶段允许服务器强制传送
            if (!m_HasSpawnedSuccessfully || IsServer)
            {
                base.Teleport(newPosition, newRotation, newScale);
            }
            else if (IsOwner)
            {
                // 客户端权威模式下，由客户端发起位置更新
                transform.position = newPosition;
                transform.rotation = newRotation;
                transform.localScale = newScale;
            }
        }
    }
}
