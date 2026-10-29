using System.Collections.Generic;
using UnityEngine;

namespace Colonial
{
    // A landmark under construction: geometry is written in a local frame (facade on z = 0,
    // +z pointing to the street, x along the facade) and converted to world space here.
    public class Site
    {
        public string key, title;
        public GameObject root;
        public Matrix4x4 frame;
        public float yaw;
        public readonly LowPoly solid = new LowPoly();   // walls, floors, stairs: gets a MeshCollider
        public readonly LowPoly deco = new LowPoly();    // small props: render only
        public readonly List<Entrance> entrances = new List<Entrance>();
        public readonly List<ActorSpot> actors = new List<ActorSpot>();
        public readonly List<(Vector3 p, Color c, float range, float intensity, bool flicker)> lights = new List<(Vector3, Color, float, float, bool)>();
        public readonly List<Bounds> interiors = new List<Bounds>();     // local-space boxes counted as "inside"
        public readonly List<(Vector3 c, Vector3 s)> blockers = new List<(Vector3, Vector3)>(); // invisible local boxes
        public Rect mapRect;     // ground area in map coords (kept free of block trees)

        public Site(string key, string title, Vector3 facadeWorld, Vector3 outward, Transform parent)
        {
            this.key = key; this.title = title;
            yaw = Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg;
            frame = Matrix4x4.TRS(facadeWorld, Quaternion.Euler(0, yaw, 0), Vector3.one);
            root = new GameObject(title);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(facadeWorld, Quaternion.Euler(0, yaw, 0));
        }

        public Vector3 World(Vector3 local) => frame.MultiplyPoint3x4(local);
        public float WorldYaw(float localYaw) => yaw + localYaw;

        public Entrance AddEntrance(string title, string subtitle, Vector3 outside, Vector3 inside, string exitTitle = null)
        {
            var e = new Entrance { site = this, title = title, subtitle = subtitle, outsideLocal = outside, insideLocal = inside, exitTitle = exitTitle ?? "Salir a la calle" };
            entrances.Add(e);
            return e;
        }

        // A door leaf hinged at `hinge` (local). The leaf extends along +x when right=false (hinge on
        // the left as seen from the street) or -x when right=true. Opens inward (toward -z).
        public DoorLeaf AddLeaf(Entrance e, Vector3 hinge, float width, float height, float thick, bool right, Color32 wood, float openAngle = 100f, bool iron = false)
        {
            var g = new LowPoly();
            float s = right ? -1f : 1f;
            float x0 = right ? -width : 0f, x1 = right ? 0f : width;
            g.BoxMM(new Vector3(x0, 0, -thick / 2), new Vector3(x1, height, thick / 2), wood, LowPoly.F.All);
            if (!iron)
            {
                // raised panels and bronze studs on both faces
                for (int side = -1; side <= 1; side += 2)
                {
                    float z = side * (thick / 2 + 0.02f);
                    for (int k = 0; k < 2; k++)
                    {
                        float y0 = k == 0 ? height * 0.08f : height * 0.52f, y1 = k == 0 ? height * 0.46f : height * 0.9f;
                        g.BoxMM(new Vector3(x0 + width * 0.15f, y0, Mathf.Min(z, z - side * 0.04f)), new Vector3(x1 - width * 0.15f, y1, Mathf.Max(z, z - side * 0.04f)), CityBuilder.WoodDark, LowPoly.F.All & ~LowPoly.F.Bottom);
                    }
                    for (int k = 0; k < 4; k++)
                        g.Box(new Vector3((x0 + x1) / 2 + s * 0, height * (0.2f + k * 0.2f), z + side * 0.03f), new Vector3(0.08f, 0.08f, 0.04f), CityBuilder.Brass, LowPoly.F.All);
                }
                g.Box(new Vector3(right ? -width + 0.18f : width - 0.18f, height * 0.5f, thick / 2 + 0.06f), new Vector3(0.08f, 0.2f, 0.08f), CityBuilder.Iron, LowPoly.F.All);
            }
            else
            {
                for (int k = 0; k <= 5; k++) { float bx = x0 + width * k / 5f; g.BoxMM(new Vector3(bx - 0.03f, 0, -0.03f), new Vector3(bx + 0.03f, height, 0.03f), CityBuilder.Iron, LowPoly.F.All); }
            }
            var go = new GameObject(right ? "Hoja der." : "Hoja izq.");
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = hinge;
            go.AddComponent<MeshFilter>().sharedMesh = g.ToMesh("puerta");
            go.AddComponent<MeshRenderer>();
            var bc = go.AddComponent<BoxCollider>();
            bc.center = new Vector3((x0 + x1) / 2, height / 2, 0); bc.size = new Vector3(width, height, Mathf.Max(thick, 0.2f));
            var leaf = go.AddComponent<DoorLeaf>();
            leaf.openAngle = right ? -openAngle : openAngle;  // positive yaw turns +x toward -z: both leaves swing inward
            e?.leaves.Add(leaf);
            return leaf;
        }

        public void Actor(Vector3 local, float localYaw, Outfit o, string name, int group = -1, string[] lines = null, bool seated = false, string item = null, string hat = null)
        {
            actors.Add(new ActorSpot { pos = World(local), yaw = WorldYaw(localYaw), outfit = o, name = name, group = group, lines = lines, seated = seated, item = item, hat = hat, site = this });
        }

        public void Light(Vector3 local, Color c, float range, float intensity, bool flicker = false) => lights.Add((World(local), c, range, intensity, flicker));
        public void Interior(Vector3 min, Vector3 max) => interiors.Add(new Bounds((min + max) / 2, max - min));

        public bool IsInside(Vector3 world)
        {
            var l = frame.inverse.MultiplyPoint3x4(world);
            foreach (var b in interiors) if (b.Contains(l)) return true;
            return false;
        }

        public void Finish(Material mat, string groupName)
        {
            var s = CityBuilder.MeshObject("Estructura", solid.ToMesh(title + " estructura"), mat, root.transform, true);
            s.transform.localPosition = Vector3.zero; s.transform.localRotation = Quaternion.identity;
            if (deco.VertexCount > 0) CityBuilder.MeshObject("Detalles", deco.ToMesh(title + " detalles"), mat, root.transform, false);
            foreach (var t in root.GetComponentsInChildren<MeshRenderer>()) if (t.sharedMaterial == null) t.sharedMaterial = mat;
            foreach (var (c, sz) in blockers)
            {
                var b = new GameObject("Limite"); b.transform.SetParent(root.transform, false);
                var bc = b.AddComponent<BoxCollider>(); bc.center = c; bc.size = sz;
            }
            foreach (var (p, c, range, intensity, flicker) in lights)
            {
                var l = new GameObject("Luz").AddComponent<Light>();
                l.transform.SetParent(root.transform, true);
                l.transform.position = p;
                l.type = LightType.Point; l.color = c; l.range = range; l.intensity = intensity; l.shadows = LightShadows.None;
                l.renderMode = LightRenderMode.ForcePixel;
                if (flicker) l.gameObject.AddComponent<Flicker>();
            }
            foreach (var e in entrances) e.Resolve();
        }
    }

    public class ActorSpot
    {
        public Vector3 pos; public float yaw; public Outfit outfit; public string name; public int group; public string[] lines; public bool seated; public string item, hat; public Site site;
    }

    public class Entrance
    {
        public Site site;
        public string title, subtitle, exitTitle;
        public Vector3 outsideLocal, insideLocal, outside, inside;
        public float outsideYaw, insideYaw;   // facing the door from each side
        public readonly List<DoorLeaf> leaves = new List<DoorLeaf>();

        public void Resolve()
        {
            outside = site.World(outsideLocal); inside = site.World(insideLocal);
            var d = inside - outside; d.y = 0;
            outsideYaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            insideYaw = outsideYaw + 180f;
        }
    }

    public class DoorLeaf : MonoBehaviour
    {
        public float openAngle = 100f;
        Quaternion closed;
        float t, target;
        void Awake() { closed = transform.localRotation; }
        public void Open(bool open) { target = open ? 1f : 0f; }
        public bool Moving => Mathf.Abs(t - target) > 0.001f;
        void Update()
        {
            if (!Moving) return;
            t = Mathf.MoveTowards(t, target, Time.deltaTime / 1.1f);
            float e = t * t * (3f - 2f * t);
            transform.localRotation = closed * Quaternion.Euler(0, openAngle * e, 0);
        }
    }

    public class Flicker : MonoBehaviour
    {
        Light l; float baseI, ph;
        void Start() { l = GetComponent<Light>(); baseI = l.intensity; ph = Random.value * 100f; }
        void Update() { float t = Time.time * 7f + ph; l.intensity = baseI * (0.85f + 0.1f * Mathf.Sin(t) + 0.05f * Mathf.Sin(t * 2.7f + 1f)); }
    }
}
