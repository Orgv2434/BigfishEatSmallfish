using UnityEngine;
using System.Collections;
using Unity.Netcode;

namespace DistantLands
{
    public class Fish : NetworkBehaviour
    {
        private float speed;
        public float averageSpeed = 1.0f;
        Vector3 averageHeading;
        Vector3 averagePosition;
        float neighborDistance = 3.0f;
        public int performance = 5;
        [HideInInspector] public GlobalFlock flock;

        bool turning = false;

        // 网络同步变量 - 服务器写入，客户端读取
        private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>(
            writePerm: NetworkVariableWritePermission.Server
        );
        private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>(
            writePerm: NetworkVariableWritePermission.Server
        );
        private NetworkVariable<float> networkSpeed = new NetworkVariable<float>(
            writePerm: NetworkVariableWritePermission.Server
        );

        void Start()
        {
            speed = Random.Range(0.5f, 1.5f) * averageSpeed;

            // 客户端初始化网络变量回调
            if (!IsServer)
            {
                networkPosition.OnValueChanged += OnPositionChanged;
                networkRotation.OnValueChanged += OnRotationChanged;
                networkSpeed.OnValueChanged += OnSpeedChanged;
            }
        }

        // 网络变量变更回调
        private void OnPositionChanged(Vector3 oldVal, Vector3 newVal)
        {
            transform.position = newVal;
        }

        private void OnRotationChanged(Quaternion oldVal, Quaternion newVal)
        {
            transform.rotation = newVal;
        }

        private void OnSpeedChanged(float oldVal, float newVal)
        {
            speed = newVal;
        }

        void Update()
        {
            if (flock == null) return;

            // 只有服务器计算运动逻辑
            if (IsServer)
            {
                CalculateMovement();

                // 更新网络变量同步到客户端
                networkPosition.Value = transform.position;
                networkRotation.Value = transform.rotation;
                networkSpeed.Value = speed;
            }
            // 客户端只执行基础移动以保持流畅
            else
            {
                transform.Translate(0, 0, Time.deltaTime * speed);
            }
        }

        // 将原Update中的运动计算逻辑提取为独立方法，仅服务器执行
        private void CalculateMovement()
        {
            ApplyTankBoundary();

            if (turning)
            {
                if (flock.target == null) return;

                Vector3 direction = flock.target.transform.position + Vector3.up * Random.Range(-2, 2) - transform.position;
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    TurnSpeed() * Time.deltaTime
                );
            }
            else
            {
                if (Random.Range(0, performance + 1) < 1)
                {
                    ApplyRules();
                }
            }

            transform.Translate(0, 0, Time.deltaTime * speed);
        }

        void ApplyTankBoundary()
        {
            if (flock == null || flock.target == null) return;

            if (Vector3.Distance(transform.position, flock.target.transform.position) >= flock.wanderSize)
            {
                turning = true;
            }
            else
            {
                turning = false;
            }
        }

        void ApplyRules()
        {
            if (flock == null || flock.allFish == null) return;

            GameObject[] gos = flock.allFish.ToArray();
            speed = Random.Range(0.5f, 1.5f) * averageSpeed;

            if (flock.target == null) return;
            Vector3 vCenter = flock.target.transform.position;
            Vector3 vAvoid = Vector3.zero;
            float gSpeed = 0;
            Vector3 goalPos = flock.target.transform.position;

            int groupSize = 0;

            foreach (GameObject go in gos)
            {
                if (go == this.gameObject) continue;

                float dist = Vector3.Distance(go.transform.position, transform.position);
                if (dist <= neighborDistance)
                {
                    vCenter += go.transform.position;
                    groupSize++;

                    if (dist < 0.75f)
                    {
                        vAvoid += (transform.position - go.transform.position);
                    }

                    Fish anotherFish = go.GetComponent<Fish>();
                    if (anotherFish != null)
                    {
                        gSpeed += anotherFish.speed;
                    }
                }
            }

            if (groupSize > 0)
            {
                vCenter = vCenter / groupSize + (goalPos - transform.position);
                speed = gSpeed / groupSize;

                Vector3 direction = (vCenter + vAvoid) - transform.position;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(direction),
                        TurnSpeed() * Time.deltaTime
                    );
                }
            }
        }

        float TurnSpeed()
        {
            return Random.Range(0.2f, 0.4f) * speed;
        }

        new void OnDestroy()
        {
            if (flock != null && flock.allFish != null)
            {
                flock.allFish.Remove(gameObject);
            }
        }
    }
}
