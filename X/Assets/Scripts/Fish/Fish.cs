using UnityEngine;
using System.Collections;

namespace DistantLands
{
    public class Fish : MonoBehaviour
    {
        private float speed;
        public float averageSpeed = 1.0f;
        Vector3 averageHeading;
        Vector3 averagePosition;
        float neighborDistance = 3.0f;
        public int performance = 5;
        public FishSchoolSetting flock;

        bool turning = false;

        void Start()
        {
            speed = Random.Range(0.5f, 1.5f) * averageSpeed;
        }

        void Update()
        {
            if (flock == null) return;

            ApplyTankBoundary(); // 检查是否超出活动范围

            if (turning)
            {
                // 转向逻辑：以父类位置为中心调整方向
                Vector3 direction = flock.schoolParent.transform.position + Vector3.up * Random.Range(-2, 2) - transform.position;
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
                    ApplyRules(); // 应用鱼群规则
                }
            }

            transform.Translate(0, 0, Time.deltaTime * speed);
        }

        /// <summary>
        /// 检查是否超出父类定义的活动范围（以父类位置为中心）
        /// </summary>
        void ApplyTankBoundary()
        {
            if (flock == null) return;

            // 用父类自身位置作为活动中心，替代原target
            float distanceFromCenter = Vector3.Distance(transform.position, flock.schoolParent.transform.position);
            turning = distanceFromCenter >= flock.wanderSize; // 超出范围则需要转向
        }

        /// <summary>
        /// 应用鱼群聚集、避障等规则（以父类位置为参考）
        /// </summary>
        void ApplyRules()
        {
            if (flock == null || flock.schoolFishPrefabs == null) return;

            GameObject[] gos = flock.schoolFishPrefabs.ToArray();
            speed = Random.Range(0.5f, 1.5f) * averageSpeed;

            // 以父类位置作为群体中心参考点（替代原target）
            Vector3 vCenter = flock.schoolParent.transform.position; 
            Vector3 vAvoid = Vector3.zero;
            float gSpeed = 0;
            Vector3 goalPos = flock.schoolParent.transform.position; // 目标点改为父类位置

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
                        vAvoid += (transform.position - go.transform.position); // 避障逻辑
                    }

                    Fish anotherFish = go.GetComponent<Fish>();
                    if (anotherFish != null)
                    {
                        gSpeed += anotherFish.speed; // 平均速度计算
                    }
                }
            }

            if (groupSize > 0)
            {
                // 群体中心 = 平均位置 + 向父类中心的偏移
                vCenter = vCenter / groupSize + (goalPos - transform.position);
                speed = gSpeed / groupSize; // 同步群体速度

                Vector3 direction = (vCenter + vAvoid) - transform.position;
                if (direction != Vector3.zero)
                {
                    // 平滑转向群体目标方向
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(direction),
                        TurnSpeed() * Time.deltaTime
                    );
                }
            }
        }

        /// <summary>
        /// 计算转向速度（与移动速度关联）
        /// </summary>
        float TurnSpeed()
        {
            return Random.Range(0.2f, 0.4f) * speed;
        }

        /// <summary>
        /// 鱼被销毁时从鱼群列表移除自身（避免空引用）
        /// </summary>
        void OnDestroy()
        {
            if (flock != null && flock.schoolFishPrefabs != null && flock.schoolFishPrefabs.Contains(gameObject))
            {
                flock.schoolFishPrefabs.Remove(gameObject);
            }
        }
    }
}