using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcFlight
{
    public sealed class FlightGame : MonoBehaviour
    {
        public Transform Suit;
        public float Armor = 100f;
        public float Reactor = 100f;
        public int Score;
        public int Kills;
        public int Wave = 1;

        readonly List<DroneEnemy> drones = new List<DroneEnemy>();
        float shotTimer;
        float missileTimer;
        float spawnTimer = 1.5f;
        float damageFlash;
        bool paused;
        bool ended;
        GUIStyle labelStyle;
        GUIStyle titleStyle;

        void Start()
        {
            if (Suit == null) { Debug.LogError("Missing flight suit"); enabled = false; return; }
            Time.timeScale = 1f;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.P) && !ended)
            {
                paused = !paused;
                Time.timeScale = paused ? 0f : 1f;
            }
            if (ended && Input.GetKeyDown(KeyCode.R))
            {
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }
            if (paused || ended) return;

            float dt = Time.deltaTime;
            float x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f)
                    - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            float y = (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) ? 1f : 0f)
                    - (Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.S) ? 1f : 0f);
            bool boost = Input.GetKey(KeyCode.LeftShift) && Reactor > 1f;
            float speed = boost ? 65f : 32f;
            Suit.position += new Vector3(x * 24f, y * 20f, speed) * dt;
            Vector3 p = Suit.position;
            Suit.position = new Vector3(Mathf.Clamp(p.x, -115f, 115f), Mathf.Clamp(p.y, 7f, 72f), p.z);
            Suit.rotation = Quaternion.Slerp(Suit.rotation,
                Quaternion.Euler(-y * 12f, 0f, -x * 18f), 5f * dt);

            Reactor = Mathf.Clamp(Reactor + (boost ? -22f : 12f) * dt, 0f, 100f);
            shotTimer -= dt; missileTimer -= dt; spawnTimer -= dt;
            damageFlash = Mathf.Max(0f, damageFlash - dt);

            if ((Input.GetKey(KeyCode.F) || Input.GetMouseButton(0)) && shotTimer <= 0f)
            {
                Shoot(false);
                shotTimer = 0.14f;
            }
            if (Input.GetKeyDown(KeyCode.E) && missileTimer <= 0f && Reactor >= 20f)
            {
                if (Shoot(true)) { Reactor -= 20f; missileTimer = 2.8f; }
            }

            drones.RemoveAll(d => d == null);
            if (spawnTimer <= 0f)
            {
                if (drones.Count < 12) SpawnDrone();
                spawnTimer = Mathf.Max(.8f, 2.7f - Wave * .1f);
            }

            if (Armor <= 0f)
            {
                Armor = 0f;
                ended = true;
                Time.timeScale = 0f;
            }
        }

        void SpawnDrone()
        {
            Vector3 at = Suit.position +
                new Vector3(UnityEngine.Random.Range(-38f, 38f), UnityEngine.Random.Range(-9f, 18f),
                            UnityEngine.Random.Range(85f, 145f));
            GameObject root = new GameObject("Hostile Drone");
            root.transform.position = at;
            root.AddComponent<SphereCollider>().radius = 3f;
            DroneEnemy drone = root.AddComponent<DroneEnemy>();
            drone.Game = this;
            drone.HP = 1 + Wave / 3;
            DroneEnemy.ConstructMesh(root.transform);
            drones.Add(drone);
        }

        DroneEnemy NearestTarget()
        {
            DroneEnemy nearest = null;
            float best = 180f;
            foreach (DroneEnemy drone in drones)
            {
                if (drone == null) continue;
                Vector3 delta = drone.transform.position - Suit.position;
                if (delta.z <= 0f || delta.z >= best || Mathf.Abs(delta.x) > 45f) continue;
                best = delta.z;
                nearest = drone;
            }
            return nearest;
        }

        bool Shoot(bool missile)
        {
            DroneEnemy target = NearestTarget();
            if (missile && target == null) return false;
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = missile ? "Homing Missile" : "Repulsor Bolt";
            Destroy(projectile.GetComponent<Collider>());
            projectile.transform.position = Suit.position + new Vector3(
                UnityEngine.Random.value < .5f ? -1.7f : 1.7f, 0.3f, 3f);
            projectile.transform.localScale = Vector3.one * (missile ? 1f : .48f);
            Material mat = new Material(Shader.Find("Standard"));
            Color c = missile ? new Color(1f, .52f, .17f) : new Color(.16f, .95f, 1f);
            mat.color = c; mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", c * 3f);
            projectile.GetComponent<Renderer>().material = mat;
            Projectile shot = projectile.AddComponent<Projectile>();
            shot.Game = this; shot.Target = target; shot.IsMissile = missile;
            return true;
        }

        public void DestroyDrone(DroneEnemy drone)
        {
            drones.Remove(drone);
            Destroy(drone.gameObject);
            Kills++;
            Score += 100 + Wave * 10;
            if (Kills % 6 == 0) Wave++;
            if (Kills % 5 == 0) Armor = Mathf.Min(100f, Armor + 15f);
        }

        public void ReceiveDamage(float damage)
        {
            if (ended) return;
            Armor = Mathf.Max(0f, Armor - damage);
            damageFlash = .2f;
        }

        void OnGUI()
        {
            labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 17, fontStyle = FontStyle.Bold, normal = { textColor = Color.cyan }
            };
            titleStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 27, fontStyle = FontStyle.Bold, normal = { textColor = Color.white }
            };
            GUI.Box(new Rect(14, 14, 335, 154), "");
            GUI.Label(new Rect(28, 23, 320, 34), "ARC FLIGHT  //  MARK ZERO", titleStyle);
            GUI.Label(new Rect(28, 67, 300, 26), "ARMOR: " + Mathf.RoundToInt(Armor) + " %", labelStyle);
            GUI.Label(new Rect(28, 91, 300, 26), "REACTOR: " + Mathf.RoundToInt(Reactor) + " %", labelStyle);
            GUI.Label(new Rect(28, 117, 320, 26), "WAVE: " + Wave + "   KILLS: " + Kills + "   SCORE: " + Score, labelStyle);
            GUI.Label(new Rect(22, Screen.height - 55, 800, 30),
                "WASD / SPACE / C: FLIGHT  |  SHIFT: BOOST  |  F / CLICK: REPULSOR  |  E: MISSILE  |  P: PAUSE", labelStyle);
            float cx = Screen.width * .5f, cy = Screen.height * .5f;
            GUI.Label(new Rect(cx - 7f, cy - 15f, 20f, 30f), "+", titleStyle);
            if (damageFlash > 0f) GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "ARMOR HIT");
            if (paused || ended)
            {
                GUI.Box(new Rect(cx - 240f, cy - 80f, 480f, 160f),
                    ended ? "ARMOR DESTROYED  -  PRESS R TO RESTART" : "PAUSED  -  PRESS P TO RESUME");
            }
        }

        void OnDisable() { Time.timeScale = 1f; }
    }

    public sealed class DroneEnemy : MonoBehaviour
    {
        public FlightGame Game;
        public int HP = 2;
        float shootIn = 2f;
        float wobble;
        public void TakeHit(int damage)
        {
            HP -= damage;
            if (HP <= 0 && Game != null) Game.DestroyDrone(this);
        }
        void Update()
        {
            if (Game == null || Game.Suit == null) return;
            wobble += Time.deltaTime * 3f;
            transform.position += new Vector3(Mathf.Sin(wobble) * 1.5f, Mathf.Sin(wobble * .8f), -14f)
                * Time.deltaTime;
            transform.Rotate(0f, 35f * Time.deltaTime, 0f);
            Vector3 offset = Game.Suit.position - transform.position;
            if (offset.z > 35f && offset.z < -15f) return;
            if (offset.magnitude < 5f) { Game.ReceiveDamage(30f * Time.deltaTime); }
            if (transform.position.z < Game.Suit.position.z - 25f)
            {
                Destroy(gameObject); return;
            }
            shootIn -= Time.deltaTime;
            if (shootIn <= 0f && offset.z > -95f && offset.z < -10f)
            {
                GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bullet.name = "Enemy Plasma";
                bullet.transform.position = transform.position;
                bullet.transform.localScale = Vector3.one * .6f;
                Destroy(bullet.GetComponent<Collider>());
                Renderer renderer = bullet.GetComponent<Renderer>();
                renderer.material.color = Color.red;
                Projectile shot = bullet.AddComponent<Projectile>();
                shot.Game = Game; shot.IsEnemy = true; shot.Speed = 62f;
                shot.Direction = (Game.Suit.position - transform.position).normalized;
                shootIn = UnityEngine.Random.Range(1.6f, 3f);
            }
        }
        public static void ConstructMesh(Transform t)
        {
            Color red = new Color(.46f, .06f, .12f);
            Primitive(t, PrimitiveType.Cube, new Vector3(0, 0, 0), new Vector3(3.2f, 1.5f, 4.4f), red);
            Primitive(t, PrimitiveType.Cube, new Vector3(-3.3f, -.1f, 0), new Vector3(4.2f, .45f, 2f), red);
            Primitive(t, PrimitiveType.Cube, new Vector3(3.3f, -.1f, 0), new Vector3(4.2f, .45f, 2f), red);
            Primitive(t, PrimitiveType.Sphere, new Vector3(0, 0, -2.1f), new Vector3(.9f, .9f, .4f),
                new Color(1f, .27f, .22f));
        }
        static void Primitive(Transform parent, PrimitiveType primitive, Vector3 pos, Vector3 scale, Color color)
        {
            GameObject child = GameObject.CreatePrimitive(primitive);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = pos;
            child.transform.localScale = scale;
            UnityEngine.Object.Destroy(child.GetComponent<Collider>());
            Material m = new Material(Shader.Find("Standard")); m.color = color;
            child.GetComponent<Renderer>().material = m;
        }
    }

    public sealed class Projectile : MonoBehaviour
    {
        public FlightGame Game;
        public DroneEnemy Target;
        public bool IsMissile;
        public bool IsEnemy;
        public Vector3 Direction = Vector3.forward;
        public float Speed = 160f;
        float life = 3f;
        void Update()
        {
            if (Game == null) { Destroy(gameObject); return; }
            float dt = Time.deltaTime; life -= dt;
            if (life <= 0f) { Destroy(gameObject); return; }
            if (IsMissile)
            {
                Speed = 110f;
                if (Target != null)
                {
                    Vector3 delta = Target.transform.position - transform.position;
                    Direction = Vector3.Slerp(Direction, delta.normalized, Mathf.Min(1f, dt * 8f));
                }
            }
            transform.position += Direction * Speed * dt;
            if (IsEnemy)
            {
                if (Vector3.Distance(transform.position, Game.Suit.position) < 3.3f)
                {
                    Game.ReceiveDamage(13f); Destroy(gameObject);
                }
                return;
            }
            // Broad collision test to keep the prototype independent of Rigidbody timing.
            DroneEnemy[] enemies = FindObjectsByType<DroneEnemy>(FindObjectsSortMode.None);
            foreach (DroneEnemy drone in enemies)
            {
                if (drone == null || drone.Game != Game) continue;
                if (Vector3.Distance(drone.transform.position, transform.position) < (IsMissile ? 5f : 3f))
                {
                    drone.TakeHit(IsMissile ? 3 : 1);
                    Destroy(gameObject); return;
                }
            }
        }
    }

    public sealed class ChaseCamera : MonoBehaviour
    {
        public Transform Target;
        void LateUpdate()
        {
            if (Target == null) return;
            Vector3 desired = Target.position + new Vector3(0f, 6f, -20f);
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-4f * Time.deltaTime));
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(Target.position + Vector3.forward * 35f - transform.position),
                1f - Mathf.Exp(-5f * Time.deltaTime));
        }
    }
}
