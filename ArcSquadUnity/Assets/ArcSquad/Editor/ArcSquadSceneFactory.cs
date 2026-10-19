using System;
using System.Collections.Generic;
using System.Linq;
using ArcSquad;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ArcSquadSceneFactory
{
    const string Base="Assets/ArcSquad";
    const string SceneFile=Base+"/Scenes/NeonVillage.unity";
    static readonly Dictionary<string,Material> Palette=new Dictionary<string,Material>();
    static readonly System.Random Random=new System.Random(4060);

    static ArcSquadSceneFactory()
    {
        EditorApplication.delayCall+=()=>{
            if(!EditorApplication.isPlayingOrWillChangePlaymode && !System.IO.File.Exists(SceneFile))
                Build();
        };
    }

    [MenuItem("Tools/Arc Squad/Rebuild Stylized Village")]
    public static void Build()
    {
        Folder(Base+"/Scenes");Folder(Base+"/Prefabs");Folder(Base+"/Materials");
        Palette.Clear();
        Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Random random=new System.Random(4060);

        RenderSettings.ambientMode=AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.27f,.4f,.59f);
        RenderSettings.fog=true;
        RenderSettings.fogMode=FogMode.ExponentialSquared;
        RenderSettings.fogDensity=.003f;
        RenderSettings.fogColor=new Color(.13f,.18f,.36f);

        Material sky=AssetDatabase.LoadAssetAtPath<Material>(Base+"/Materials/DuskSky.mat");
        if (!sky) {
            sky=new Material(Shader.Find("Skybox/Procedural"));
            sky.SetColor("_SkyTint",new Color(.24f,.34f,.64f));
            sky.SetColor("_GroundColor",new Color(.21f,.14f,.24f));
            sky.SetFloat("_AtmosphereThickness",1.24f);
            AssetDatabase.CreateAsset(sky,Base+"/Materials/DuskSky.mat");
        }
        RenderSettings.skybox=sky;

        var light=new GameObject("SUN / GOLDEN HOUR");
        light.transform.rotation=Quaternion.Euler(39,-43,0);
        Light sun=light.AddComponent<Light>();
        sun.type=LightType.Directional;sun.intensity=1.38f;sun.shadows=LightShadows.Soft;
        sun.shadowStrength=.63f;sun.color=new Color(1f,.78f,.63f);

        var world=new GameObject("01 • NEON VILLAGE / MINI HOUSES");
        Ground(world.transform);
        BuildVillage(world.transform,random);
        BuildGardens(world.transform,random);
        BuildCityBackdrop(world.transform,random);

        var player=CreatePlayerPrefab();
        var drone=CreateDronePrefab();

        var managerGO=new GameObject("02 • MULTIPLAYER NETWORK");
        var manager=managerGO.AddComponent<NetworkManager>();
        UnityTransport transport=managerGO.AddComponent<UnityTransport>();
        manager.NetworkConfig.NetworkTransport=transport;
        manager.NetworkConfig.PlayerPrefab=player;
        managerGO.AddComponent<ArcLobby>().Configure(drone);

        var missionGO=new GameObject("03 • SHARED CO-OP MISSIONS");
        missionGO.AddComponent<NetworkObject>();
        missionGO.AddComponent<ArcMission>().Configure(drone);

        var cameraGo=new GameObject("Main Camera");
        cameraGo.tag="MainCamera";
        Camera camera=cameraGo.AddComponent<Camera>();
        camera.clearFlags=CameraClearFlags.Skybox;
        camera.fieldOfView=68;
        camera.farClipPlane=850;
        camera.allowHDR=true;
        cameraGo.transform.position=new Vector3(0,22,-30);
        cameraGo.AddComponent<AudioListener>();
        cameraGo.AddComponent<ArcCameraRig>();

        EditorSceneManager.SaveScene(scene,SceneFile);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(SceneFile,true)};
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(SceneFile);
        Debug.Log("ARC SQUAD: scene generated with village, multiplayer prefabs and Netcode.");
    }

    static void Folder(string path)
    {
        string[] segments=path.Split('/');
        string current=segments[0];
        for(int i=1;i<segments.Length;i++)
        {
            string next=current+"/"+segments[i];
            if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(current,segments[i]);
            current=next;
        }
    }

    static Material Mat(string label,Color color,bool glow=false)
    {
        if(Palette.TryGetValue(label,out var existing)&&existing)return existing;
        string path=Base+"/Materials/"+label+".mat";
        var cached=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(cached){Palette[label]=cached;return cached;}
        Shader shader=Shader.Find("ArcSquad/NeonToon");
        if(!shader)shader=Shader.Find("Standard");
        Material m=new Material(shader);
        m.SetColor("_Color",color);
        if(m.HasProperty("_EmissionColor"))m.SetColor("_EmissionColor",glow?color*2.6f:Color.black);
        if(m.HasProperty("_RimStrength"))m.SetFloat("_RimStrength",glow?.45f:.24f);
        AssetDatabase.CreateAsset(m,path);Palette[label]=m;return m;
    }
    static GameObject Block(string label,Transform parent, Vector3 pos,Vector3 scale,Material mat,PrimitiveType primitive=PrimitiveType.Cube)
    {
        GameObject g=GameObject.CreatePrimitive(primitive);g.name=label;
        g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;
        var r=g.GetComponent<Renderer>();r.sharedMaterial=mat;
        var c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);
        return g;
    }

    static Mesh RoofMesh(float width,float height,float depth)
    {
        float x=width/2,z=depth/2;
        var m=new Mesh();
        m.vertices=new []{
          new Vector3(-x,0,-z),new Vector3(x,0,-z),new Vector3(0,height,-z),
          new Vector3(-x,0,z),new Vector3(x,0,z),new Vector3(0,height,z)};
        m.triangles=new[]{0,2,1,3,4,5,0,3,5,0,5,2,1,2,5,1,5,4,0,1,4,0,4,3};
        m.RecalculateNormals();return m;
    }
    static GameObject PitchedRoof(Transform parent,float w,float h,float d,Material mat)
    {
        GameObject roof=new GameObject("PITCHED ROOF");
        roof.transform.SetParent(parent,false);roof.transform.localPosition=new Vector3(0,h,0);
        string file=Base+"/Materials/SharedPitchedRoof.asset";
        Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(file);
        if(!mesh) { mesh=RoofMesh(1,1,1); AssetDatabase.CreateAsset(mesh,file); }
        roof.transform.localScale=new Vector3(w,3.2f,d);
        roof.AddComponent<MeshFilter>().sharedMesh=mesh;
        roof.AddComponent<MeshRenderer>().sharedMaterial=mat;return roof;
    }

    static void Ground(Transform root)
    {
        Material grass=Mat("MossyTeal",new Color(.14f,.38f,.34f));
        Material road=Mat("SlateRoad",new Color(.075f,.13f,.22f));
        Material line=Mat("NeonRoadLine",new Color(.10f,.87f,1f),true);
        Material stone=Mat("Sidewalk",new Color(.39f,.51f,.65f));
        Material fountain=Mat("FountainWater",new Color(.15f,.78f,.95f),true);
        Block("MEADOW • ISLAND",root,new Vector3(0,-.95f,260),new Vector3(500,1.5f,750),grass);
        for(int x=-4;x<=4;x++)
        {
            Block("Residential Road X"+x,root,new Vector3(x*44,.02f,260),new Vector3(11,.12f,740),road);
            Block("Transit Edge Cyan",root,new Vector3(x*44+5.55f,.1f,260),new Vector3(.2f,.06f,740),line);
        }
        for(int z=0;z<=12;z++)
        {
            float rz=z*44;
            Block("Cross Street #"+z,root,new Vector3(0,.05f,rz),new Vector3(470,.15f,9),road);
            for(int x=-4;x<=4;x++)Block("Curb Walk",root,new Vector3(x*44+9,.06f,rz),new Vector3(1,.16f,8),stone);
        }
        Block("Village plaza",root,new Vector3(0,.13f,175),new Vector3(34,.35f,37),stone);
        Block("Fountain base",root,new Vector3(0,1,175),new Vector3(11,1.2f,11),Mat("DeepRoyal",new Color(.15f,.18f,.43f)),PrimitiveType.Cylinder);
        Block("Fountain water",root,new Vector3(0,1.8f,175),new Vector3(8,.5f,8),fountain,PrimitiveType.Cylinder);
    }

    static void BuildVillage(Transform root,System.Random rng)
    {
        Material[] colors={
          Mat("HouseMint",new Color(.5f,.86f,.75f)),
          Mat("HousePeach",new Color(1f,.66f,.57f)),
          Mat("HouseLavender",new Color(.73f,.63f,.98f)),
          Mat("HouseCream",new Color(1f,.86f,.65f)),
          Mat("HouseBlue",new Color(.42f,.73f,.95f))};
        Material[] roofs={
          Mat("RoofCherry",new Color(.9f,.28f,.41f)),
          Mat("RoofOcean",new Color(.15f,.54f,.85f)),
          Mat("RoofViolet",new Color(.5f,.32f,.78f)),
          Mat("RoofHoney",new Color(.92f,.54f,.18f))};
        var glass=Mat("GlowingWindows",new Color(.41f,.96f,1f),true);
        var wood=Mat("Doors",new Color(.23f,.21f,.43f));
        Material edge=Mat("SoftFoundation",new Color(.72f,.8f,.9f));

        for(int z=0;z<11;z++)for(int x=-4;x<4;x++)
        {
            if((x==0||x==-1)&&(z==3||z==4))continue; // town square
            if(rng.NextDouble()<.12)continue;
            float bx=x*44+22,bz=z*44+22;
            float w=11+(float)rng.NextDouble()*5,d=12+(float)rng.NextDouble()*6;
            float h=5+(float)rng.NextDouble()*4;
            var house=new GameObject("MINI HOME / District "+z+"-"+x);
            house.transform.SetParent(root,false);
            house.transform.localPosition=new Vector3(bx,0,bz);
            Material wall=colors[rng.Next(colors.Length)],roof=roofs[rng.Next(roofs.Length)];
            Block("foundation",house.transform,new Vector3(0,.25f,0),new Vector3(w+2,.5f,d+2),edge);
            Block("facade",house.transform,new Vector3(0,h/2,0),new Vector3(w,h,d),wall);
            PitchedRoof(house.transform,w+3,h,d+3,roof);
            Block("front door",house.transform,new Vector3(0,1.75f,-d/2-.09f),new Vector3(2.0f,3.5f,.18f),wood);
            Block("door neon",house.transform,new Vector3(.7f,2.1f,-d/2-.23f),new Vector3(.12f,.15f,.12f),glass,PrimitiveType.Sphere);
            for(int sign=-1;sign<=1;sign+=2)
            {
                Block("window glow",house.transform,new Vector3(sign*3,3.5f,-d/2-.1f),new Vector3(1.7f,1.85f,.16f),glass);
                Block("window ledge",house.transform,new Vector3(sign*3,2.44f,-d/2-.26f),new Vector3(2,.25f,.55f),edge);
            }
            if(rng.NextDouble()>.6f)
            {
                Block("porch side awning",house.transform,new Vector3(0,4,-d/2-1.4f),new Vector3(4,.24f,2.9f),roof);
            }
        }
    }

    static void BuildGardens(Transform root,System.Random rng)
    {
        var bark=Mat("TreeBark",new Color(.28f,.27f,.35f));
        Material[] leaves={Mat("PinkTree",new Color(.94f,.43f,.72f)),
            Mat("AquaTree",new Color(.3f,.87f,.67f)),Mat("RoyalTree",new Color(.4f,.59f,.91f))};
        var neon=Mat("LampAqua",new Color(.26f,.97f,1f),true);
        var steel=Mat("Lightposts",new Color(.2f,.27f,.4f));
        var shrub=Mat("BushGreen",new Color(.17f,.62f,.49f));
        for(int i=0;i<95;i++)
        {
            float x=rng.Next(-210,210),z=rng.Next(15,540);
            if(Math.Abs(x%44)<8||Math.Abs(z%44)<8)continue;
            var tree=new GameObject("Candy tree");tree.transform.SetParent(root,false);
            tree.transform.localPosition=new Vector3(x,0,z);
            float ht=3.8f+(float)rng.NextDouble()*2;
            Block("trunk",tree.transform,new Vector3(0,ht*.5f,0),new Vector3(.65f,ht,.65f),bark,PrimitiveType.Cylinder);
            var leaf=leaves[rng.Next(leaves.Length)];
            Block("canopy",tree.transform,new Vector3(0,ht+1.3f,0),new Vector3(4.2f,3.4f,4.2f),leaf,PrimitiveType.Sphere);
            Block("low shrub",tree.transform,new Vector3(1.9f,.8f,.1f),new Vector3(2,1.5f,2),shrub,PrimitiveType.Sphere);
        }
        for(int z=1;z<12;z++)for(int x=-4;x<5;x++)
        {
            float px=x*44+6,pz=z*44-6;
            var lamp=new GameObject("NEON POST");lamp.transform.SetParent(root,false);
            lamp.transform.localPosition=new Vector3(px,0,pz);
            Block("stem",lamp.transform,new Vector3(0,4,0),new Vector3(.23f,8,.23f),steel,PrimitiveType.Cylinder);
            Block("light housing",lamp.transform,new Vector3(0,8,0),new Vector3(2.5f,.6f,2.5f),neon,PrimitiveType.Sphere);
        }
    }

    static void BuildCityBackdrop(Transform root,System.Random rng)
    {
        Material[] skyscrapers={
            Mat("BackgroundIndigo",new Color(.18f,.27f,.46f)),
            Mat("BackgroundMidnight",new Color(.11f,.21f,.39f)),
            Mat("BackgroundPlum",new Color(.37f,.24f,.52f))};
        Material windows=Mat("BackgroundLED",new Color(.37f,.89f,1f),true);
        var assetPaths=AssetDatabase.FindAssets("t:Model",new[]{Base+"/Art/Kenney"})
            .Select(AssetDatabase.GUIDToAssetPath).Where(x=>x.EndsWith(".obj",StringComparison.OrdinalIgnoreCase)).ToArray();
        for(int i=0;i<55;i++)
        {
            float x=i%2==0?rng.Next(-290,-210):rng.Next(210,290);
            float z=rng.Next(-20,540);
            var tower=new GameObject("Back skyline #"+i);tower.transform.SetParent(root,false);
            tower.transform.localPosition=new Vector3(x,0,z);
            if(i<22 && assetPaths.Length>0)
            {
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(assetPaths[i%assetPaths.Length]);
                var instance=asset?(GameObject)PrefabUtility.InstantiatePrefab(asset):null;
                if(instance)
                {
                    instance.name="KENNEY CC0 • REAL 3D MESH";
                    instance.transform.SetParent(tower.transform,false);
                    var r=instance.GetComponentInChildren<Renderer>();
                    float height=r ? r.bounds.size.y : 1f;
                    instance.transform.localScale=Vector3.one * Mathf.Clamp(31/Mathf.Max(.5f,height),1f,40f);
                    continue;
                }
            }
            float height=rng.Next(28,105);
            float w=rng.Next(16,31);
            Material body=skyscrapers[rng.Next(skyscrapers.Length)];
            Block("tower volume",tower.transform,new Vector3(0,height/2,0),new Vector3(w,height,w),body);
            Block("skyline LED strip",tower.transform,new Vector3(0,height+.15f,0),new Vector3(w+.8f,.32f,w+.8f),windows);
        }
    }

    static GameObject CreatePlayerPrefab()
    {
        string file=Base+"/Prefabs/ArcPilot.prefab";
        var old=AssetDatabase.LoadAssetAtPath<GameObject>(file);if(old)return old;
        var root=new GameObject("ARC SUIT / NETWORK PLAYER");
        root.AddComponent<NetworkObject>();
        root.AddComponent<NetworkTransform>();
        root.AddComponent<ArcPilot>();

        Material red=Mat("SuitCrimson",new Color(.89f,.18f,.28f));
        Material gold=Mat("SuitGolden",new Color(1f,.72f,.27f));
        Material charcoal=Mat("SuitCharcoal",new Color(.12f,.14f,.24f));
        Material plasma=Mat("SuitPlasma",new Color(.31f,.96f,1f),true);
        Material face=Mat("SuitVisor",new Color(.99f,.84f,.65f));

        Block("ARMOR CHEST",root.transform,new Vector3(0,.5f,0),new Vector3(2.7f,3.1f,1.4f),red);
        Block("ARC POWER CORE",root.transform,new Vector3(0,.7f,.8f),new Vector3(.8f,.8f,.23f),plasma,PrimitiveType.Sphere);
        Block("ARMOR NECK",root.transform,new Vector3(0,2.25f,0),new Vector3(.5f,.5f,.5f),charcoal,PrimitiveType.Cylinder);
        Block("ARMOR HELMET",root.transform,new Vector3(0,3,0),new Vector3(1.72f,2,1.63f),gold,PrimitiveType.Sphere);
        Block("VISOR PLATE",root.transform,new Vector3(0,3.07f,.84f),new Vector3(1.4f,.83f,.22f),face);
        for(int sign=-1;sign<=1;sign+=2)
        {
            Block("LUMINOUS EYE",root.transform,new Vector3(sign*.39f,3.23f,.985f),new Vector3(.46f,.15f,.09f),plasma);
            Block("ARMOR SHOULDER",root.transform,new Vector3(sign*1.8f,1.15f,0),new Vector3(1.32f,1.06f,1.46f),red,PrimitiveType.Sphere);
            Block("ARMOR UPPER ARM",root.transform,new Vector3(sign*2.0f,-.18f,0),new Vector3(.87f,2.2f,.9f),red,PrimitiveType.Capsule);
            Block("ARMOR GLOVE",root.transform,new Vector3(sign*2.02f,-1.53f,.12f),new Vector3(.95f,.85f,.95f),gold,PrimitiveType.Sphere);
            Block("REPULSOR PALM",root.transform,new Vector3(sign*2.02f,-1.82f,.51f),new Vector3(.48f,.18f,.45f),plasma,PrimitiveType.Sphere);
            Block("ARMOR LEG",root.transform,new Vector3(sign*.72f,-1.92f,0),new Vector3(1.12f,2.7f,1.06f),red,PrimitiveType.Capsule);
            Block("ARMOR BOOT",root.transform,new Vector3(sign*.72f,-3.55f,.32f),new Vector3(1.06f,1.62f,1.36f),gold,PrimitiveType.Capsule);
            Block("TURBO JET",root.transform,new Vector3(sign*.72f,-4.6f,-.35f),new Vector3(.8f,.25f,.8f),plasma,PrimitiveType.Sphere);
            var point=new GameObject("THRUSTER POINT LIGHT");point.transform.SetParent(root.transform,false);
            point.transform.localPosition=new Vector3(sign*.72f,-4.8f,-.5f);
            var lt=point.AddComponent<Light>();lt.color=Color.cyan;lt.intensity=2.5f;lt.range=5f;lt.type=LightType.Point;
        }
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,file);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject CreateDronePrefab()
    {
        string path=Base+"/Prefabs/ArcDrone.prefab";
        var old=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(old)return old;
        var root=new GameObject("SKY RAID DRONE");
        root.AddComponent<NetworkObject>();root.AddComponent<NetworkTransform>();
        root.AddComponent<ArcDrone>();
        Material shade=Mat("DronePurple",new Color(.42f,.17f,.7f));
        Material edge=Mat("DronePink",new Color(.93f,.33f,.7f));
        Material lens=Mat("DroneEye",new Color(1f,.22f,.48f),true);
        Block("drone chassis",root.transform,Vector3.zero,new Vector3(3,1.4f,3.4f),shade);
        Block("drone lens",root.transform,new Vector3(0,.25f,-1.83f),new Vector3(1,.6f,.18f),lens,PrimitiveType.Sphere);
        for(int sign=-1;sign<=1;sign+=2)
        {
            Block("wing",root.transform,new Vector3(sign*3.35f,0,.25f),new Vector3(4.3f,.35f,1.8f),edge);
            Block("rotor",root.transform,new Vector3(sign*4.4f,.45f,.25f),new Vector3(1.8f,.3f,1.8f),shade,PrimitiveType.Cylinder);
        }
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }
}
