using System;
using System.IO;
using System.Linq;
using ArcFlight;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ArcFlightSceneBuilder
{
    const string ScenePath = "Assets/ArcFlight/Scenes/CityFlight.unity";

    static ArcFlightSceneBuilder()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !File.Exists(ScenePath))
                BuildScene();
        };
    }

    [MenuItem("Tools/Arc Flight/Build or Rebuild City")]
    public static void BuildScene()
    {
        EnsureFolder("Assets/ArcFlight/Scenes");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.32f, .43f, .58f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = .006f;
        RenderSettings.fogColor = new Color(.055f, .13f, .23f);

        GameObject sun = new GameObject("Sun / Directional Light");
        Light sunlight = sun.AddComponent<Light>();
        sunlight.type = LightType.Directional;
        sunlight.intensity = 1.65f;
        sunlight.color = new Color(.95f, .94f, 1f);
        sunlight.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(48, -35, 0);

        GameObject world = new GameObject("CITY // 3D OBJECTS");
        MakePrimitive("Highway", PrimitiveType.Cube,
            new Vector3(0, -.5f, 900), new Vector3(270f, 1f, 1850f),
            new Color(.065f, .115f, .18f), world.transform);
        for (int lane = -3; lane <= 3; lane++)
            MakePrimitive("Road / " + lane, PrimitiveType.Cube,
                new Vector3(lane * 33, .02f, 900), new Vector3(9f, .12f, 1850f),
                new Color(.09f, .16f, .24f), world.transform);

        GameObject[] imported = AssetDatabase.FindAssets("t:Model",
                new[] {"Assets/ArcFlight/Vendor/Kenney"})
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
            .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
            .Where(p => p != null)
            .ToArray();

        System.Random rand = new System.Random(4060);
        int importsPlaced = 0;
        for (int z = 0; z < 58; z++)
        {
            for (int x = -3; x <= 3; x++)
            {
                if (rand.NextDouble() < .16 || x == 0) continue;
                float bx = x * 34 + (float)(rand.NextDouble() - .5) * 5;
                float bz = z * 31 + (float)(rand.NextDouble() - .5) * 3;
                float height = 18 + (float)rand.NextDouble() * 63;
                if (imported.Length > 0 && rand.NextDouble() > .4)
                {
                    GameObject source = imported[rand.Next(imported.Length)];
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                    if (instance != null)
                    {
                        instance.name = "Kenney CC0 Building";
                        instance.transform.SetParent(world.transform, false);
                        instance.transform.position = new Vector3(bx, 0f, bz);
                        Renderer renderer = instance.GetComponentInChildren<Renderer>();
                        float h = renderer != null ? renderer.bounds.size.y : 1f;
                        instance.transform.localScale = Vector3.one * Mathf.Clamp(height / Mathf.Max(h, .25f), .4f, 65f);
                        importsPlaced++;
                        continue;
                    }
                }

                Color color = Color.Lerp(new Color(.06f, .14f, .22f),
                    new Color(.14f, .35f, .49f), (float)rand.NextDouble());
                MakePrimitive("City Tower", PrimitiveType.Cube,
                    new Vector3(bx, height*.5f, bz),
                    new Vector3(22f, height, 24f), color, world.transform);
                if (rand.NextDouble() > .55)
                    MakePrimitive("Roof Facility", PrimitiveType.Cube,
                        new Vector3(bx, height + 2.2f, bz),
                        new Vector3(8f, 4f, 9f), color * .8f, world.transform);
            }
        }

        GameObject player = new GameObject("PLAYER / MARK ZERO ARMOR");
        player.transform.position = new Vector3(0, 28, 0);
        CreateSuit(player.transform);

        GameObject cameraGO = new GameObject("Main Camera");
        cameraGO.tag = "MainCamera";
        Camera camera = cameraGO.AddComponent<Camera>();
        camera.fieldOfView = 66f;
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 900f;
        camera.backgroundColor = new Color(.035f, .08f, .14f);
        camera.clearFlags = CameraClearFlags.Skybox;
        cameraGO.transform.position = new Vector3(0, 36, -20);
        cameraGO.AddComponent<AudioListener>();
        cameraGO.AddComponent<ChaseCamera>().Target = player.transform;

        GameObject game = new GameObject("ARC FLIGHT // GAME CONTROLLER");
        game.AddComponent<FlightGame>().Suit = player.transform;

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        EditorSceneManager.OpenScene(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("ARC FLIGHT: Native Unity CityFlight scene generated. CC0 OBJ instances: " + importsPlaced);
    }

    static void EnsureFolder(string path)
    {
        string[] seg = path.Split('/');
        string p = seg[0];
        for (int i = 1; i < seg.Length; i++)
        {
            string next = p + "/" + seg[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(p, seg[i]);
            p = next;
        }
    }

    static Material Material(Color baseColor, float metallic = .35f, float smooth = .6f, bool glow = false)
    {
        Shader shader = Shader.Find("Standard");
        var mat = new Material(shader);
        mat.color = baseColor;
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Glossiness", smooth);
        if (glow)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", baseColor * 2.8f);
        }
        return mat;
    }

    static GameObject MakePrimitive(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Color color,
        Transform parent, bool glow = false)
    {
        GameObject ob = GameObject.CreatePrimitive(type);
        ob.name = name;
        ob.transform.SetParent(parent, false);
        ob.transform.localPosition = pos;
        ob.transform.localScale = scale;
        Collider c = ob.GetComponent<Collider>();
        if (c != null) UnityEngine.Object.DestroyImmediate(c);
        ob.GetComponent<Renderer>().sharedMaterial = Material(color, .38f, .6f, glow);
        return ob;
    }

    static void CreateSuit(Transform root)
    {
        Color red = new Color(.74f, .085f, .105f);
        Color gold = new Color(.94f, .52f, .15f);
        Color cyan = new Color(.13f, .95f, 1f);
        Color dark = new Color(.10f, .13f, .22f);
        MakePrimitive("Chestplate", PrimitiveType.Cube, new Vector3(0, .6f, 0),
            new Vector3(2.65f, 3.2f, 1.35f), red, root);
        MakePrimitive("Arc Reactor", PrimitiveType.Sphere, new Vector3(0, .85f, .8f),
            new Vector3(.86f, .86f, .3f), cyan, root, true);
        MakePrimitive("Neck", PrimitiveType.Cylinder, new Vector3(0, 2.3f, 0),
            new Vector3(.65f, .3f, .65f), dark, root);
        MakePrimitive("Helmet", PrimitiveType.Sphere, new Vector3(0, 3.05f, 0),
            new Vector3(1.75f, 1.9f, 1.7f), gold, root);
        MakePrimitive("Faceplate", PrimitiveType.Cube, new Vector3(0, 3.02f, .85f),
            new Vector3(1.35f, 1.05f, .24f), gold, root);
        MakePrimitive("Left Eye", PrimitiveType.Cube, new Vector3(-.39f, 3.21f, .99f),
            new Vector3(.44f, .18f, .10f), cyan, root, true);
        MakePrimitive("Right Eye", PrimitiveType.Cube, new Vector3(.39f, 3.21f, .99f),
            new Vector3(.44f, .18f, .10f), cyan, root, true);
        for (int sign = -1; sign <= 1; sign += 2)
        {
            float x = sign * 1.8f;
            MakePrimitive("Shoulder Armor", PrimitiveType.Sphere, new Vector3(x, 1.45f, 0),
                new Vector3(1.2f, .95f, 1.25f), red, root);
            MakePrimitive("Forearm / Repulsor", PrimitiveType.Capsule, new Vector3(x * 1.24f, -.15f, .3f),
                new Vector3(.8f, 2.1f, .9f), red, root);
            MakePrimitive("Palm emitter", PrimitiveType.Sphere, new Vector3(x * 1.24f, -1.28f, .6f),
                new Vector3(.48f, .26f, .48f), cyan, root, true);
            MakePrimitive("Thigh armor", PrimitiveType.Capsule, new Vector3(sign*.74f, -2.0f, 0),
                new Vector3(.95f, 2.3f, 1.0f), red, root);
            MakePrimitive("Boot armor", PrimitiveType.Capsule, new Vector3(sign*.74f, -3.85f, .25f),
                new Vector3(.94f, 1.65f, 1.14f), gold, root);
            MakePrimitive("Boot thruster", PrimitiveType.Sphere, new Vector3(sign*.74f, -4.8f, -.28f),
                new Vector3(.75f, .18f, .75f), cyan, root, true);
            GameObject thruster = new GameObject("Thruster Light");
            thruster.transform.SetParent(root, false);
            thruster.transform.localPosition = new Vector3(sign*.74f, -4.85f, -0.5f);
            Light light = thruster.AddComponent<Light>();
            light.type = LightType.Point; light.range = 7f; light.intensity = 3.2f; light.color = cyan;
        }
    }
}
