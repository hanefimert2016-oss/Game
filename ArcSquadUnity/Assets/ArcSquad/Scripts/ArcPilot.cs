using Unity.Netcode;
using UnityEngine;

namespace ArcSquad
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class ArcPilot : NetworkBehaviour
    {
        public readonly NetworkVariable<float> Shield = new NetworkVariable<float>(100);
        public readonly NetworkVariable<float> Energy = new NetworkVariable<float>(100);
        public readonly NetworkVariable<int> Eliminations = new NetworkVariable<int>(0);
        public readonly NetworkVariable<int> Points = new NetworkVariable<int>(0);

        Vector3 requestedMove;
        Vector3 requestedAim = Vector3.forward;
        float requestedLift, repulsorDelay, rocketDelay, respawnAt, sendInputAt;
        bool requestedBoost, requestedFire, requestedRocket;
        Renderer[] paintRenderers;
        Vector3 spawnPos;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                spawnPos = new Vector3((OwnerClientId % 4) * 5f - 7f, 15f, 0f);
                transform.position = spawnPos;
            }
            paintRenderers = GetComponentsInChildren<Renderer>();
            Color[] colors = { new Color(.9f,.18f,.29f), new Color(.14f,.66f,.96f), new Color(.8f,.36f,.94f), new Color(1f,.64f,.18f) };
            Color body = colors[(int)(OwnerClientId % (ulong)colors.Length)];
            foreach (var r in paintRenderers)
            {
                if (r.name.Contains("ARMOR"))
                {
                    foreach (var mat in r.materials)
                    {
                        if (mat.HasProperty("_Color")) mat.SetColor("_Color", body);
                    }
                }
            }
            if (IsOwner && Camera.main != null)
            {
                var cameraRig = Camera.main.GetComponent<ArcCameraRig>();
                if (cameraRig != null) cameraRig.Target = transform;
            }
        }

        void Update()
        {
            if (!IsSpawned) return;
            if (IsOwner && Time.unscaledTime >= sendInputAt)
            {
                var cam = Camera.main ? Camera.main.GetComponent<ArcCameraRig>() : null;
                Vector3 forwards = cam ? cam.FlatForward : Vector3.forward;
                Vector3 right = cam ? cam.FlatRight : Vector3.right;
                float horizontal = (Input.GetKey(KeyCode.D)?1:0) - (Input.GetKey(KeyCode.A)?1:0);
                float forward = (Input.GetKey(KeyCode.W)?1:0) - (Input.GetKey(KeyCode.S)?1:0);
                Vector3 move = (right*horizontal + forwards*forward).normalized;
                float lift = (Input.GetKey(KeyCode.Space)?1:0) - (Input.GetKey(KeyCode.C)?1:0);
                Vector3 aim = cam ? cam.transform.forward : Vector3.forward;
                bool boost = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                bool fire = Input.GetKey(KeyCode.F) || Input.GetMouseButton(0);
                bool rocket = Input.GetKey(KeyCode.R) || Input.GetMouseButton(1);
                SendInputRpc(move, lift, aim, boost, fire, rocket);
                sendInputAt = Time.unscaledTime + .05f;
            }
            if (IsOwner && Input.GetKeyDown(KeyCode.Tab))
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked
                    ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
            }
        }

        // The owner sends intentions only. The SERVER decides velocity, energy, hits and score.
        [Rpc(SendTo.Server)]
        void SendInputRpc(Vector3 movement, float lift, Vector3 aim, bool boost, bool fire, bool rocket, RpcParams context = default)
        {
            if (context.Receive.SenderClientId != OwnerClientId) return;
            if (!Finite(movement) || !Finite(aim) || float.IsNaN(lift)) return;
            requestedMove = Vector3.ClampMagnitude(movement, 1f);
            requestedMove.y = 0f;
            requestedLift = Mathf.Clamp(lift, -1, 1);
            requestedAim = aim.sqrMagnitude > .01f ? aim.normalized : Vector3.forward;
            requestedBoost = boost;
            requestedFire = fire;
            requestedRocket = rocket;
        }

        static bool Finite(Vector3 v) => !(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)
                ||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z));

        void FixedUpdate()
        {
            if (!IsServer || !IsSpawned) return;
            float dt = SquadRules.LimitMovement(Time.fixedDeltaTime);
            if (Shield.Value <= 0)
            {
                if (Time.time >= respawnAt)
                {
                    Shield.Value=100;
                    Energy.Value=100;
                    requestedMove=Vector3.zero;
                    transform.position = spawnPos + Vector3.forward * 4;
                }
                return;
            }

            bool boosting = requestedBoost && SquadRules.CanBoost(Energy.Value);
            float speed = boosting ? 39f : 19f;
            Vector3 velocity = Vector3.ClampMagnitude(requestedMove, 1f) * speed + Vector3.up * requestedLift * speed * .75f;
            var next = transform.position + velocity * dt;
            next.x = Mathf.Clamp(next.x, -180, 180);
            next.y = Mathf.Clamp(next.y, 6, 75);
            next.z = Mathf.Clamp(next.z, -180, 1200);
            transform.position = next;

            if (requestedMove.sqrMagnitude > .03f)
            {
                var desired = Quaternion.LookRotation(requestedMove,Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desired, 5*dt);
            }
            Energy.Value = SquadRules.ClampEnergy(Energy.Value + (boosting ? -22f : 13f)*dt);
            repulsorDelay -= dt; rocketDelay -= dt;
            if (requestedFire && repulsorDelay <= 0f)
            {
                FireOnServer(false);
                repulsorDelay = .18f;
            }
            if (requestedRocket && SquadRules.CanMissile(Energy.Value, rocketDelay))
            {
                if (FireOnServer(true))
                {
                    Energy.Value = SquadRules.ClampEnergy(Energy.Value - SquadRules.MissileCost);
                    rocketDelay = 2.8f;
                }
            }
        }

        bool FireOnServer(bool rocket)
        {
            Vector3 origin = transform.position + Vector3.up*1f;
            ArcDrone target = null;
            float best = rocket ? 160 : 105;
            Vector3 aim = requestedAim.normalized;
            foreach (var drone in FindObjectsByType<ArcDrone>(FindObjectsSortMode.None))
            {
                if (!drone.IsSpawned) continue;
                Vector3 delta = drone.transform.position - origin;
                float range = delta.magnitude;
                if (range < best && Vector3.Dot(delta.normalized,aim) > (rocket ? .35f : .72f))
                {
                    target = drone; best = range;
                }
            }
            // A miss still generates a proper beam but cannot score.
            Vector3 end = target ? target.transform.position :
                origin + aim * (rocket ? 160f : 110f);
            ShowBeamRpc(origin,end,rocket);
            if (target) target.ServerTakeHit(rocket ? 4 : 1, this);
            return target || !rocket;
        }

        [Rpc(SendTo.ClientsAndHost)]
        void ShowBeamRpc(Vector3 a, Vector3 b, bool rocket)
        {
            ArcEffects.DrawShot(a,b,rocket);
        }

        public void ServerTakeDamage(float amount)
        {
            if (!IsServer || Shield.Value <= 0f) return;
            Shield.Value = SquadRules.ApplyDamage(Shield.Value, amount);
            if (Shield.Value <= 0f) respawnAt = Time.time + 3f;
        }

        public void ServerAwardKill(int wave)
        {
            if (!IsServer) return;
            Eliminations.Value++;
            Points.Value += SquadRules.ScoreForKill(wave);
            if (Eliminations.Value % 5 == 0) Shield.Value = Mathf.Min(100,Shield.Value+15);
        }
    }
}
