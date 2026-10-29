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

        // rain streaks: unlit, alpha-blended particles (the streak texture is made at runtime)
        var rain = Mat("Lluvia", "Universal Render Pipeline/Particles/Unlit", Color.white, 0f);
        rain.SetFloat("_Surface", 1f); rain.SetFloat("_Blend", 0f);
        rain.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); rain.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); rain.SetFloat("_ZWrite", 0f);
        rain.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        rain.SetOverrideTag("RenderType", "Transparent");
        rain.renderQueue = (int)RenderQueue.Transparent;
        rain.enableInstancing = false;
        EditorUtility.SetDirty(rain);

        var game = new GameObject("Buenos Aires 1810").AddComponent<ColonialGame>();
        game.palette = palette; game.dirt = dirt; game.grass = grass; game.water = water; game.sun = sun; game.rain = rain;

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

    // ------------------------------------------------------------------ z-fighting scan
    // Finds pairs of triangles that face the same way, lie within 2.5 cm of the same plane and
    // overlap: the GPU can't decide which one is in front, so they flicker. Writes a report.
    [MenuItem("Buenos Aires 1810/4. Buscar superficies superpuestas")]
    public static void ZFightScan()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var game = Object.FindFirstObjectByType<ColonialGame>();
        var world = game.BuildWorld();
        var skip = new System.Collections.Generic.HashSet<Renderer>();
        foreach (var lg in world.GetComponentsInChildren<LODGroup>())
        {
            var lods = lg.GetLODs();
            for (int i = 1; i < lods.Length; i++) foreach (var r in lods[i].renderers) if (r) skip.Add(r);
        }
        var tris = new System.Collections.Generic.List<(Vector3 a, Vector3 b, Vector3 c, Vector3 n, float d, int src, Vector2 uv)>();
        var names = new System.Collections.Generic.List<string>();
        var srcRoot = new System.Collections.Generic.List<Transform>();
        foreach (var mf in world.GetComponentsInChildren<MeshFilter>())
        {
            var mr = mf.GetComponent<MeshRenderer>();
            if (mr == null || skip.Contains(mr) || mf.sharedMesh == null) continue;
            var m = mf.sharedMesh; var v = m.vertices; var t = m.triangles; var uvs = m.uv; var M = mf.transform.localToWorldMatrix;
            int src = names.Count; names.Add(NodePath(mf.transform));
            var rt = mf.transform; while (rt.parent != null && rt.parent != world) rt = rt.parent; srcRoot.Add(rt);
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 a = M.MultiplyPoint3x4(v[t[i]]), b = M.MultiplyPoint3x4(v[t[i + 1]]), c = M.MultiplyPoint3x4(v[t[i + 2]]);
                var cr = Vector3.Cross(b - a, c - a);
                if (cr.magnitude < 1e-4f) continue;
                var n = cr.normalized;
                tris.Add((a, b, c, n, Vector3.Dot(n, a), src, uvs.Length > 0 ? uvs[t[i]] : Vector2.zero));
            }
        }
        // bucket by quantized normal and plane offset
        const float DQ = 0.025f;
        var buckets = new System.Collections.Generic.Dictionary<(int, int, int, long), System.Collections.Generic.List<int>>();
        (int, int, int) NK(Vector3 n) => (Mathf.RoundToInt(n.x * 40), Mathf.RoundToInt(n.y * 40), Mathf.RoundToInt(n.z * 40));
        for (int i = 0; i < tris.Count; i++)
        {
            var (x, y, z) = NK(tris[i].n);
            var k = (x, y, z, (long)Mathf.FloorToInt(tris[i].d / DQ));
            if (!buckets.TryGetValue(k, out var l)) buckets[k] = l = new System.Collections.Generic.List<int>();
            l.Add(i);
        }
        var hits = new System.Collections.Generic.Dictionary<(int, int, int), (int count, float area, string a, string b, Vector3 p, float dd, Vector3 n, Transform root, int ta, int tb)>();

        foreach (var kv in buckets)
        {
            var (x, y, z, dk) = kv.Key;
            var cand = new System.Collections.Generic.List<int>(kv.Value);
            if (buckets.TryGetValue((x, y, z, dk + 1), out var next)) cand.AddRange(next);
            int own = kv.Value.Count;
            if (cand.Count < 2) continue;
            var n = tris[kv.Value[0]].n;
            var u = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized; var w = Vector3.Cross(n, u);
            var P = cand.Select(i => { var tr = tris[i]; return new[] { new Vector2(Vector3.Dot(tr.a, u), Vector3.Dot(tr.a, w)), new Vector2(Vector3.Dot(tr.b, u), Vector3.Dot(tr.b, w)), new Vector2(Vector3.Dot(tr.c, u), Vector3.Dot(tr.c, w)) }; }).ToArray();
            var order = Enumerable.Range(0, cand.Count).OrderBy(i => P[i].Min(p => p.x)).ToArray();
            for (int oi = 0; oi < order.Length; oi++)
            {
                int i = order[oi]; float maxX = P[i].Max(p => p.x);
                for (int oj = oi + 1; oj < order.Length; oj++)
                {
                    int j = order[oj];
                    if (P[j].Min(p => p.x) >= maxX - 0.01f) break;
                    if (i >= own && j >= own) continue;   // both from the next bucket: counted there
                    var ti = tris[cand[i]]; var tj = tris[cand[j]];
                    if (Vector3.Dot(ti.n, tj.n) < 0.999f || Mathf.Abs(ti.d - tj.d) > DQ) continue;
                    if ((ti.uv - tj.uv).sqrMagnitude < 1e-6f) continue;   // same color, same plane: invisible
                    float area = OverlapArea(P[i], P[j]);
                    if (area < 0.02f) continue;
                    var c = (ti.a + ti.b + ti.c) / 3f;
                    var cell = (Mathf.FloorToInt(c.x / 4f), Mathf.FloorToInt(c.y / 4f), Mathf.FloorToInt(c.z / 4f));
                    hits.TryGetValue(cell, out var h);
                    hits[cell] = (h.count + 1, h.area + area, h.a ?? names[ti.src], h.b ?? names[tj.src], c, Mathf.Max(h.dd, Mathf.Abs(ti.d - tj.d)), ti.n, srcRoot[ti.src], h.count == 0 ? cand[i] : h.ta, h.count == 0 ? cand[j] : h.tb);
                }
            }
        }
        var lines = hits.Values.OrderByDescending(h => h.area).Select(h =>
        {
            var m = City.U2Map(h.p);
            var lp = h.root.InverseTransformPoint(h.p); var ln = h.root.InverseTransformDirection(h.n);
            string Bx(int k) { var tr = tris[k]; var a = h.root.InverseTransformPoint(tr.a); var b = h.root.InverseTransformPoint(tr.b); var c = h.root.InverseTransformPoint(tr.c); var mn = Vector3.Min(a, Vector3.Min(b, c)); var mx = Vector3.Max(a, Vector3.Max(b, c)); return $"[{mn.x:F2},{mn.y:F2},{mn.z:F2}..{mx.x:F2},{mx.y:F2},{mx.z:F2}]"; }
            return $"{Bx(h.ta)} vs {Bx(h.tb)}  n({ln.x:F2},{ln.y:F2},{ln.z:F2})  {h.area,8:F2} m²  {h.count,4} pares  Δ{h.dd * 100f:F1}cm  mapa({m.x:F0},{m.y:F0})  local({lp.x:F1},{lp.y:F2},{lp.z:F1}) n({ln.x:F0},{ln.y:F0},{ln.z:F0})  {h.a}  |  {h.b}";
        }).ToList();
        Directory.CreateDirectory("_work/colonial");
        File.WriteAllLines("_work/colonial/zfight.txt", lines);
        Debug.Log($"[Colonial] z-fight: {tris.Count} triángulos, {lines.Count} zonas -> _work/colonial/zfight.txt");
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    static string NodePath(Transform t) => t.parent == null ? t.name : NodePath(t.parent) + "/" + t.name;

    // area of the intersection of two 2D triangles (Sutherland-Hodgman clip)
    static float OverlapArea(Vector2[] a, Vector2[] b)
    {
        var poly = new System.Collections.Generic.List<Vector2>(a);
        float sb = Mathf.Sign(Cross2(b[1] - b[0], b[2] - b[0]));
        for (int e = 0; e < 3 && poly.Count > 0; e++)
        {
            Vector2 p0 = b[e], p1 = b[(e + 1) % 3];
            var outp = new System.Collections.Generic.List<Vector2>();
            for (int i = 0; i < poly.Count; i++)
            {
                var cur = poly[i]; var prev = poly[(i + poly.Count - 1) % poly.Count];
                bool cin = sb * Cross2(p1 - p0, cur - p0) >= 0, pin = sb * Cross2(p1 - p0, prev - p0) >= 0;
                if (cin != pin)
                {
                    float da = Cross2(p1 - p0, prev - p0), db = Cross2(p1 - p0, cur - p0);
                    outp.Add(prev + (cur - prev) * (da / (da - db)));
                }
                if (cin) outp.Add(cur);
            }
            poly = outp;
        }
        float ar = 0;
        for (int i = 0; i < poly.Count; i++) ar += Cross2(poly[i], poly[(i + 1) % poly.Count]);
        return Mathf.Abs(ar) / 2f;
    }
    static float Cross2(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    public static void ZFightFromCommandLine()
    {
        ZFightScan();
        EditorApplication.Exit(0);
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
