using Unity.Netcode;
using UnityEngine;

namespace ArcSquad
{
    public sealed class ArcDrone : NetworkBehaviour
    {
        public readonly NetworkVariable<int> Health = new NetworkVariable<int>(3);
        ArcPilot target;
        float hitDelay;
        float rotationOffset;

        public override void OnNetworkSpawn()
        {
            rotationOffset = OwnerClientId % 10;
            if (IsServer) Health.Value = 3;
        }

        void Update()
        {
            if (!IsServer || !IsSpawned) return;
            float dt = Time.deltaTime;
            if (!target || !target.IsSpawned || target.Shield.Value<=0)
            {
                float best=float.MaxValue;
                foreach(var pilot in FindObjectsByType<ArcPilot>(FindObjectsSortMode.None))
                {
                    if (!pilot.IsSpawned || pilot.Shield.Value<=0) continue;
                    float sq=(transform.position-pilot.transform.position).sqrMagnitude;
                    if (sq<best) { best=sq;target=pilot; }
                }
            }
            if (!target) return;
            Vector3 delta=target.transform.position-transform.position;
            Vector3 follow=(delta.normalized + Vector3.up * Mathf.Sin(Time.time*2+rotationOffset)*.18f).normalized;
            transform.position += follow*10f*dt;
            transform.rotation=Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(follow,Vector3.up),dt*3);
            hitDelay -= dt;
            if (delta.sqrMagnitude<22f && hitDelay<=0)
            {
                target.ServerTakeDamage(12);
                hitDelay=1.6f;
            }
            // Prevent forever-stuck drones in deserted parts of the world.
            if (Time.time - spawnTimestamp > 125f) NetworkObject.Despawn(true);
        }
        float spawnTimestamp;
        void Start() { spawnTimestamp=Time.time; }

        public void ServerTakeHit(int damage,ArcPilot attacker)
        {
            if (!IsServer || !IsSpawned || Health.Value<=0) return;
            Health.Value=Mathf.Max(0,Health.Value-damage);
            if (Health.Value==0)
            {
                var mission=FindFirstObjectByType<ArcMission>();
                attacker.ServerAwardKill(mission ? mission.Wave.Value : 1);
                if (mission) mission.ServerRecordDroneDown();
                NetworkObject.Despawn(true);
            }
        }
    }
}
