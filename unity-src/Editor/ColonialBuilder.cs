// Buenos Aires 1810: creates the scene, its materials and lighting, builds WebGL, and renders
// verification shots. The city itself is generated at runtime by Colonial.ColonialGame.
// Menu: Buenos Aires 1810/...  — or headless:
//   Unity -batchmode -quit -projectPath . -executeMethod ColonialBuilder.BuildSceneAndShots
//   Unity -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod ColonialBuilder.BuildWebFromCommandLine
using System.IO;
using System.Linq;
using Colonial;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class ColonialBuilder
{
    const string ScenePath = "Assets/Scenes/BuenosAires1810.unity";
    const string MatDir = "Assets/Colonial/Materials";
    const string OutputDir = "Builds/BuenosAires1810";
    const string ShotDir = "_work/colonial/shots";

    [MenuItem("Buenos Aires 1810/1. Crear escena")]
    public static void BuildScene()
    {
        Directory.CreateDirectory(MatDir);
        var palette = Mat("Paleta", "Universal Render Pipeline/Simple Lit", Color.white, 0f);
        var dirt = Mat("Tierra", "Universal Render Pipeline/Simple Lit", Color.white, 0f);
        var grass = Mat("Pasto", "Universal Render Pipeline/Simple Lit", Color.white, 0f);
        var water = Mat("Agua", "Universal Render Pipeline/Lit", new Color(0.85f, 0.95f, 1f), 0.85f);
        var sky = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Cielo.mat");
        if (sky == null) { sky = new Material(Shader.Find("Skybox/Procedural")); AssetDatabase.CreateAsset(sky, MatDir + "/Cielo.mat"); }
        sky.SetFloat("_SunSize", 0.035f); sky.SetFloat("_AtmosphereThickness", 0.75f); sky.SetFloat("_Exposure", 1.15f);
        sky.SetColor("_SkyTint", new Color(0.62f, 0.66f, 0.72f)); sky.SetColor("_GroundColor", new Color(0.78f, 0.8f, 0.79f));   // = fog color, hides the far clip
        EditorUtility.SetDirty(sky);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        camGo.AddComponent<UniversalAdditionalCameraData>();
        var sunGo = new GameObject("Sol", typeof(Light));
        var sun = sunGo.GetComponent<Light>();
        sun.type = LightType.Directional;
        sunGo.transform.rotation = Quaternion.Euler(38f, -128f, 0f);   // late afternoon light from the north-west
        sun.shadows = LightShadows.Soft;
        RenderSettings.skybox = sky;
        RenderSettings.sun = sun;

        var game = new GameObject("Buenos Aires 1810").AddComponent<ColonialGame>();
        game.palette = palette; game.dirt = dirt; game.grass = grass; game.water = water; game.sun = sun;

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[Colonial] escena creada: " + ScenePath);
    }

    static Material Mat(string name, string shader, Color c, float smooth)
    {
        string path = $"{MatDir}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, path); }
        m.shader = Shader.Find(shader);
        m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_SpecularHighlights")) m.SetFloat("_SpecularHighlights", smooth > 0.1f ? 1f : 0f);
        if (shader.EndsWith("Simple Lit")) { m.SetFloat("_SpecularHighlights", 0f); m.DisableKeyword("_SPECULAR_COLOR"); m.SetColor("_SpecColor", Color.black); }
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    // ------------------------------------------------------------------ verification shots
    [MenuItem("Buenos Aires 1810/2. Capturas de verificación")]
    public static void Shots()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var game = Object.FindFirstObjectByType<ColonialGame>();
        var world = game.BuildWorld();
        var crowd = new GameObject("Gente").AddComponent<Crowd>();
        crowd.Init(game.palette, null, false);
        crowd.Populate(game.Sites);
        game.ApplyPalette();
        foreach (var a in crowd.agents) { a.rig.Tick(0.016f); }
        Physics.SyncTransforms();
        var cam = Camera.main;
        cam.nearClipPlane = 0.08f; cam.farClipPlane = 800f; cam.fieldOfView = 68f;
        Directory.CreateDirectory(ShotDir);
        var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        cam.targetTexture = rt;
        cam.Render();   // warm-up frame (the first render after loading can miss materials)
        int n = 0;
        void Shot(string name, Vector3 pos, Vector3 look)
        {
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.LookRotation(look - pos);
            cam.Render();
            Save(rt, $"{ShotDir}/{n++:00}_{name}.png");
        }
        Vector3 M(float x, float z, float y = 1.62f) => City.W2U(x, z, y);
        foreach (var s in game.Sites)
        {
            var e = s.entrances[0];
            var dir = (e.inside - e.outside).normalized;
            Shot(s.key + "_fachada", e.outside - dir * 16f + Vector3.up * 3.5f, e.outside + Vector3.up * 3f);
            Shot(s.key + "_puerta", e.outside - dir * 2.5f + Vector3.up * 1.62f, e.inside + Vector3.up * 1.5f);
            Shot(s.key + "_interior", e.inside + Vector3.up * 1.62f, e.inside + dir * 10f + Vector3.up * 1.4f);
        }
        var cab = game.Sites[0];
        Shot("cabildo_sala", cab.World(new Vector3(0, 5.4f + 1.62f, -6f)), cab.World(new Vector3(0, 5.4f + 1f, -14f)));
        Shot("cabildo_balcon", cab.World(new Vector3(0, 5.4f + 1.62f, 0.8f)), M(640, 398, 2f));
        Shot("cabildo_patio", cab.World(new Vector3(8, 1.62f, -60f)), cab.World(new Vector3(-12, 3f, -26f)));
        var fu = game.Sites[2];
        Shot("fuerte_salon", fu.World(new Vector3(0, 1.62f, -68f)), fu.World(new Vector3(0, 2.5f, -84f)));
        Shot("fuerte_muralla", fu.World(new Vector3(-38f, 9.6f, -2f)), fu.World(new Vector3(20f, 6f, -60f)));
        var jab = game.Sites[3];
        Shot("jaboneria_secreta", jab.World(new Vector3(-6.5f, 1.62f, -18f)), jab.World(new Vector3(-11f, 1.0f, -21.5f)));
        var db = game.Sites[4];
        Shot("donblas_sala", db.World(new Vector3(-6f, 1.62f, -11f)), db.World(new Vector3(-9f, 1f, -18f)));
        Shot("donblas_patio2", db.World(new Vector3(0f, 1.62f, -33f)), db.World(new Vector3(0f, 1.5f, -48f)));
        Shot("plaza", M(660, 414), M(560, 398, 6f));
        Shot("plaza_recova", M(640, 398), M(678, 398, 4f));
        Shot("mercado", M(690, 400), M(760, 400, 2f));
        Shot("calle", M(180, 342), M(400, 342, 2f));
        Shot("aerea", M(400, 900, 220f), M(560, 398, 0f));
        Shot("rio", M(870, 300, 12f), M(1100, 400, 0f));
        cam.targetTexture = null;
        Debug.Log($"[Colonial] {n} capturas en {Path.GetFullPath(ShotDir)} · paleta {Palette.Count} colores");
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);   // discard the generated objects
    }

    static void Save(RenderTexture rt, string path)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    public static void BuildSceneAndShots()
    {
        BuildScene();
        Shots();
        EditorApplication.Exit(0);
    }

    // ------------------------------------------------------------------ WebGL
    [MenuItem("Buenos Aires 1810/3. Compilar WebGL")]
    public static void BuildWeb()
    {
        SalaWebBuild.Configure();   // shared web settings (compression, memory, WebGL 2, ASTC, ...)
        PlayerSettings.WebGL.template = "PROJECT:BuenosAires1810";
        PlayerSettings.productName = "Buenos Aires 1810";
        PlayerSettings.WebGL.initialMemorySize = 256;
        PlayerSettings.SplashScreen.show = false;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = OutputDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        });
        var s = report.summary;
        var msg = $"[Colonial] WebGL: {s.result}  {s.totalSize / (1024f * 1024f):F1} MB  {s.totalTime:mm\\:ss}  -> {Path.GetFullPath(OutputDir)}";
        if (s.result == BuildResult.Succeeded) Debug.Log(msg); else Debug.LogError(msg);
    }

    // scene + shots + WebGL in one headless run (launch Unity with -buildTarget WebGL)
    public static void AllFromCommandLine()
    {
        BuildScene();
        Shots();
        BuildWeb();
        var ok = Directory.Exists(Path.Combine(OutputDir, "Build")) && Directory.GetFiles(Path.Combine(OutputDir, "Build")).Any();
        EditorApplication.Exit(ok ? 0 : 1);
    }

    public static void BuildWebFromCommandLine()
    {
        if (!File.Exists(ScenePath)) BuildScene();
        BuildWeb();
        var ok = Directory.Exists(Path.Combine(OutputDir, "Build")) && Directory.GetFiles(Path.Combine(OutputDir, "Build")).Any();
        EditorApplication.Exit(ok ? 0 : 1);
    }
}
