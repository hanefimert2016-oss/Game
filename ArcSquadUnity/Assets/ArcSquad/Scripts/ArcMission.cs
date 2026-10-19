using Unity.Netcode;
using UnityEngine;

namespace ArcSquad
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class ArcMission : NetworkBehaviour
    {
        [SerializeField] GameObject dronePrefab;
        public readonly NetworkVariable<int> Wave = new NetworkVariable<int>(1);
        public readonly NetworkVariable<int> DronesDefeated = new NetworkVariable<int>(0);
        float nextSpawn=3f;
        public void Configure(GameObject prefab) => dronePrefab=prefab;

        void Update()
        {
            if (!IsServer || !IsSpawned || dronePrefab==null) return;
            nextSpawn -= Time.deltaTime;
            if (nextSpawn>0) return;
            nextSpawn = Mathf.Max(1.25f,3.6f-Wave.Value*.2f);
            ArcPilot[] pilots=FindObjectsByType<ArcPilot>(FindObjectsSortMode.None);
            ArcDrone[] enemies=FindObjectsByType<ArcDrone>(FindObjectsSortMode.None);
            if (pilots.Length==0 || enemies.Length>=Mathf.Min(14,3+Wave.Value*2)) return;
            var p=pilots[Random.Range(0,pilots.Length)];
            if(!p.IsSpawned || p.Shield.Value<=0) return;
            Vector3 position=p.transform.position
                +new Vector3(Random.Range(-42,42),Random.Range(-6,13),Random.Range(38,85));
            position.y=Mathf.Clamp(position.y,10,65);
            var spawned=Instantiate(dronePrefab,position,Quaternion.identity);
            spawned.GetComponent<NetworkObject>().Spawn();
        }

        public void ServerRecordDroneDown()
        {
            if (!IsServer) return;
            DronesDefeated.Value++;
            if (DronesDefeated.Value % 6 == 0) Wave.Value++;
        }
    }
}
