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
        [HideInInspector] public GlobalFlock flock;

        bool turning = false;

        void Start()
        {
            speed = Random.Range(0.5f, 1.5f) * averageSpeed;
        }

        void Update()
        {
            if (flock == null) return;

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

        // 新增：鱼被销毁时从鱼群列表移除自身
        void OnDestroy()
        {
            if (flock != null && flock.allFish != null)
            {
                flock.allFish.Remove(gameObject);
            }
        }
    }
}