using System.Collections.Generic;
using UnityEngine;

namespace Colonial
{
    // Builds the city at startup from ciudad.json: blocks of low-poly houses (one mesh per block
    // with a cheap far LOD), ground, river, outskirts, trees and street props.
    // Generating at runtime keeps the web download tiny (no mesh data in the build).
    public class CityBuilder
    {
        public static readonly Color32 Tile = Palette.Hex(0xa8502f), TileDark = Palette.Hex(0x8e3f24), TileLight = Palette.Hex(0xb9643a),
            WoodDark = Palette.Hex(0x4a2e17), Wood = Palette.Hex(0x6b4426), WoodLight = Palette.Hex(0x8a5a32),
            Iron = Palette.Hex(0x232325), Whitewash = Palette.Hex(0xf4efe3), Zocalo = Palette.Hex(0x9c8f7a), Shutter = Palette.Hex(0x3d6150),
            Glass = Palette.Hex(0x1e2226), Brass = Palette.Hex(0xc9a24a), Brick = Palette.Hex(0xb8724a), Stone = Palette.Hex(0xb9b2a2),
            Leaf = Palette.Hex(0x4a6b2a), LeafDark = Palette.Hex(0x3a5822), Trunk = Palette.Hex(0x5a4330), Sand = Palette.Hex(0xd8c49a),
            Water = Palette.Hex(0x6e8a8c), Mud = Palette.Hex(0x8a7556), Grass = Palette.Hex(0x7d8a4a), Ochre = Palette.Hex(0xd8b878);

        readonly Material mat;
        readonly Transform root;
        readonly Rng rng = new Rng(1810);
        public readonly List<Rect> blockedRects = new List<Rect>();    // map rects kept free of trees (landmark yards)

        public CityBuilder(Material palette, Transform parent) { mat = palette; root = parent; }

        public static GameObject MeshObject(string name, Mesh mesh, Material m, Transform parent, bool collider = false, bool shadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = m;
            mr.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.isStatic = true;
            return go;
        }

        // ------------------------------------------------------------------ blocks of houses
        public void BuildBlocks(bool mobile)
        {
            var byBlock = new Dictionary<int, List<HouseData>>();
            var all = new List<HouseData>(City.Data.houses);
            // fill the stretches of street front left empty around the landmarks (map coordinates)
            all.Add(Filler(161.3f, 198f, 164f, 224f, "S")); all.Add(Filler(190f, 198f, 193.6f, 224f, "S"));
            all.Add(Filler(572f, 262f, 598f, 276f, "W")); all.Add(Filler(572f, 276f, 576f, 336f, "W")); all.Add(Filler(616f, 310f, 629.2f, 336f, "S"));
            all.Add(Filler(537.7f, 348f, 560f, 356f, "N")); all.Add(Filler(532.6f, 422f, 538f, 448f, "S")); all.Add(Filler(538f, 440f, 560f, 448f, "S"));
            all.Add(Filler(250.9f, 572f, 266f, 598f, "N")); all.Add(Filler(302f, 572f, 303.3f, 598f, "N"));
            foreach (var h in all)
            {
                int i = Mathf.FloorToInt((h.x + h.w / 2 - City.S) / City.PITCH), j = Mathf.FloorToInt((h.z + h.d / 2 - City.S) / City.PITCH);
                int key = j * 100 + i;
                if (!byBlock.TryGetValue(key, out var l)) byBlock[key] = l = new List<HouseData>();
                l.Add(h);
            }
            var hi = new LowPoly(); var lo = new LowPoly();
            for (int j = 0; j < City.ROWS; j++)
                for (int i = 0; i < City.COLS; i++)
                {
                    if (City.IsPlazaBlock(i, j) || City.IsFuerteBlock(i, j)) continue;
                    hi.Clear(); lo.Clear();
                    var go = new GameObject($"Manzana {i},{j}");
                    go.transform.SetParent(root, false);
                    go.isStatic = true;
                    float X = City.Bx(i), Z = City.Bz(j);
                    // inner garden / orchard
                    var g0 = City.W2U(X + City.D - 1, Z + City.B - City.D + 1, 0.04f); var g1 = City.W2U(X + City.B - City.D + 1, Z + City.D - 1, 0.04f);
                    // no lawn where a landmark has its patios and yards (it would cover their floors)
                    var inner = new Rect(X + City.D, Z + City.D, City.B - 2 * City.D, City.B - 2 * City.D);
                    if (!blockedRects.Exists(rr => rr.Overlaps(inner)))
                    {
                        hi.BoxMM(g0, new Vector3(g1.x, 0.06f, g1.z), Grass, LowPoly.F.Top);
                        lo.BoxMM(g0, new Vector3(g1.x, 0.06f, g1.z), Grass, LowPoly.F.Top);
                    }
                    if (byBlock.TryGetValue(j * 100 + i, out var list))
                        foreach (var h in list) { House(hi, h, true); House(lo, h, false); AddCollider(go, h); }
                    Trees(hi, lo, X, Z);
                    CornerLanterns(hi, X, Z);
                    var a = MeshObject("Detalle", hi.ToMesh(go.name + " hi"), mat, go.transform);
                    var b = MeshObject("Lejos", lo.ToMesh(go.name + " lo"), mat, go.transform, false, false);
                    var lod = go.AddComponent<LODGroup>();
                    lod.SetLODs(new[] {
                        // a block is ~100 m wide: 0.6 of the screen height ≈ 145 m away (0.8 ≈ 110 m on phones)
                        new LOD(mobile ? 0.8f : 0.6f, new Renderer[] { a.GetComponent<Renderer>() }),
                        new LOD(0.01f, new Renderer[] { b.GetComponent<Renderer>() }) });
                    lod.RecalculateBounds();
                }
        }

        HouseData Filler(float x0, float z0, float x1, float z1, string edge)
        {
            var h = new HouseData { x = x0, z = z0, w = x1 - x0, d = z1 - z0, edge = edge, floors = 1, roof = rng.Chance(0.5f) ? "teja" : "azotea" };
            h.nx = edge == "W" ? -1 : edge == "E" ? 1 : 0; h.nz = edge == "N" ? -1 : edge == "S" ? 1 : 0;
            h.len = edge == "N" || edge == "S" ? h.w : h.d;
            h.h = rng.Range(4.8f, 6f);
            h.color = "#f2e3cd";
            h.door = h.len >= 6f ? 0f : 99f;
            h.wins = h.len >= 11f ? new[] { -h.len / 4f - 0.5f, h.len / 4f + 0.5f } : new float[0];
            h.upWins = new float[0];
            return h;
        }

        static void AddCollider(GameObject go, HouseData h)
        {
            var c = go.AddComponent<BoxCollider>();
            var p = City.W2U(h.x + h.w / 2, h.z + h.d / 2, h.h / 2);
            c.center = p; c.size = new Vector3(h.w, h.h, h.d);
        }

        // Local frame of a facade: origin at the facade center on the ground, +z points to the street.
        public static Matrix4x4 FacadeFrame(Vector3 worldPos, Vector3 outward)
        {
            float yaw = Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg;
            return Matrix4x4.TRS(worldPos, Quaternion.Euler(0, yaw, 0), Vector3.one);
        }

        void House(LowPoly g, HouseData h, bool detail)
        {
            // facade center in map coords and outward normal in Unity coords
            float fx = h.edge == "W" ? h.x : h.edge == "E" ? h.x + h.w : h.x + h.w / 2;
            float fz = h.edge == "N" ? h.z : h.edge == "S" ? h.z + h.d : h.z + h.d / 2;
            var outward = new Vector3(h.nx, 0, -h.nz);
            g.PushM(FacadeFrame(City.W2U(fx, fz), outward));
            float L = h.len, Dd = h.edge == "N" || h.edge == "S" ? h.d : h.w, Hh = h.h;
            var wall = Palette.Hex(h.color);
            var r = new Rng((uint)(h.x * 73 + h.z * 131 + 7));
            // body
            g.BoxMM(new Vector3(-L / 2, 0, -Dd), new Vector3(L / 2, Hh, 0), wall, detail ? LowPoly.F.NoBottom : LowPoly.F.Front | LowPoly.F.Top | LowPoly.F.Back | LowPoly.F.Left | LowPoly.F.Right);
            bool tiled = h.roof == "teja";
            if (tiled)
            {
                var tc = r.Pick(new[] { Tile, TileDark, TileLight });
                g.GableRoof(new Vector3(0, Hh, -Dd / 2), L + 0.3f, Dd + 1.1f, r.Range(2.4f, 3.4f), tc, wall);
            }
            if (!detail)
            {
                if (!tiled) g.BoxMM(new Vector3(-L / 2, Hh, -0.5f), new Vector3(L / 2, Hh + 0.7f, 0.15f), wall, LowPoly.F.Front | LowPoly.F.Top);
                // door and windows as flat dark patches
                if (h.door < 50f) g.Quad(new Vector3(-h.door - 1, 0, 0.03f), new Vector3(-h.door - 1, 2.9f, 0.03f), new Vector3(-h.door + 1, 2.9f, 0.03f), new Vector3(-h.door + 1, 0, 0.03f), WoodDark);
                g.Pop();
                return;
            }
            // base (zócalo)
            g.BoxMM(new Vector3(-L / 2, 0, 0), new Vector3(L / 2, 0.85f, 0.1f), Zocalo, LowPoly.F.Front | LowPoly.F.Top);
            // cornice / parapet
            if (tiled) g.BoxMM(new Vector3(-L / 2, Hh - 0.35f, 0), new Vector3(L / 2, Hh, 0.3f), Whitewash, LowPoly.F.Front | LowPoly.F.Bottom);
            else
            {
                g.BoxMM(new Vector3(-L / 2, Hh - 0.3f, 0), new Vector3(L / 2, Hh, 0.28f), Whitewash, LowPoly.F.Front | LowPoly.F.Bottom | LowPoly.F.Top);
                g.BoxMM(new Vector3(-L / 2, Hh, -0.4f), new Vector3(L / 2, Hh + 0.75f, 0.12f), wall, LowPoly.F.Front | LowPoly.F.Top | LowPoly.F.Back);
                g.BoxMM(new Vector3(-L / 2, Hh + 0.75f, -0.45f), new Vector3(L / 2, Hh + 0.85f, 0.18f), Whitewash, LowPoly.F.Front | LowPoly.F.Top | LowPoly.F.Bottom);
                if (r.Chance(0.35f)) for (int k = 0; k < 2; k++) { float px = (k == 0 ? -1 : 1) * (L / 2 - 0.5f); g.Box(new Vector3(px, Hh + 1.2f, -0.15f), new Vector3(0.45f, 0.7f, 0.45f), Whitewash); g.Sphere(new Vector3(px, Hh + 1.68f, -0.15f), new Vector3(0.2f, 0.22f, 0.2f), 6, 3, Whitewash); }
            }
            bool big = h.floors == 2;
            // door
            if (h.door < 50f) Door(g, -h.door, big ? 2.6f : 2.1f, big ? 3.5f : 3.0f, r);
            // ground floor windows with protruding iron grilles (rejas)
            foreach (var u in h.wins) Reja(g, -u, 1.0f, 1.35f, 1.9f);
            if (big)
                foreach (var u in h.upWins)
                {
                    Balcony(g, -u, Hh * 0.55f);
                }
            g.Pop();
        }

        public static void Door(LowPoly g, float x, float w, float hgt, Rng r)
        {
            var frame = r.Chance(0.5f) ? Whitewash : Ochre;
            g.BoxMM(new Vector3(x - w / 2 - 0.3f, 0, 0), new Vector3(x + w / 2 + 0.3f, hgt + 0.35f, 0.12f), frame, LowPoly.F.Front | LowPoly.F.Top | LowPoly.F.Left | LowPoly.F.Right);
            g.BoxMM(new Vector3(x - w / 2 - 0.45f, hgt + 0.35f, 0), new Vector3(x + w / 2 + 0.45f, hgt + 0.6f, 0.22f), frame, LowPoly.F.Front | LowPoly.F.Top | LowPoly.F.Bottom | LowPoly.F.Left | LowPoly.F.Right);
            var wood = r.Chance(0.25f) ? Palette.Hex(0x2f5d73) : r.Chance(0.5f) ? WoodDark : Wood;
            g.BoxMM(new Vector3(x - w / 2, 0, 0.12f), new Vector3(x - 0.02f, hgt, 0.17f), wood, LowPoly.F.Front | LowPoly.F.Top | LowPoly.F.Left);
            g.BoxMM(new Vector3(x + 0.02f, 0, 0.12f), new Vector3(x + w / 2, hgt, 0.17f), wood, LowPoly.F.Front | LowPoly.F.Top | LowPoly.F.Right);
            // panels and studs
            for (int s = -1; s <= 1; s += 2)
            {
                float cx = x + s * w / 4;
                g.BoxMM(new Vector3(cx - w / 6, hgt * 0.55f, 0.17f), new Vector3(cx + w / 6, hgt * 0.9f, 0.2f), WoodDark, LowPoly.F.Front | LowPoly.F.Top | LowPoly.F.Bottom);
                g.BoxMM(new Vector3(cx - w / 6, hgt * 0.1f, 0.17f), new Vector3(cx + w / 6, hgt * 0.45f, 0.2f), WoodDark, LowPoly.F.Front | LowPoly.F.Top | LowPoly.F.Bottom);
            }
            g.Box(new Vector3(x + 0.18f, hgt * 0.5f, 0.21f), new Vector3(0.1f, 0.14f, 0.05f), Brass, LowPoly.F.Front | LowPoly.F.Top);
            g.Box(new Vector3(x - 0.18f, hgt * 0.5f, 0.21f), new Vector3(0.1f, 0.14f, 0.05f), Brass, LowPoly.F.Front | LowPoly.F.Top);
            // threshold step
            g.BoxMM(new Vector3(x - w / 2 - 0.2f, 0, 0), new Vector3(x + w / 2 + 0.2f, 0.18f, 0.55f), Stone, LowPoly.F.Front | LowPoly.F.Top | LowPoly.F.Left | LowPoly.F.Right);
        }

        public static void Reja(LowPoly g, float x, float y0, float w, float hgt)
        {
            // window recess (dark) + green shutters + projecting iron grille on a sill
            g.BoxMM(new Vector3(x - w / 2 - 0.15f, y0 - 0.15f, 0), new Vector3(x + w / 2 + 0.15f, y0 + hgt + 0.15f, 0.06f), WoodDark, LowPoly.F.Front | LowPoly.F.Left | LowPoly.F.Right | LowPoly.F.Top);
            g.BoxMM(new Vector3(x - w / 2, y0, 0.06f), new Vector3(x + w / 2, y0 + hgt, 0.08f), Glass, LowPoly.F.Front);
            g.BoxMM(new Vector3(x - w / 2 - 0.62f, y0, 0.06f), new Vector3(x - w / 2 - 0.1f, y0 + hgt, 0.1f), Shutter, LowPoly.F.Front | LowPoly.F.Left | LowPoly.F.Right);
            g.BoxMM(new Vector3(x + w / 2 + 0.1f, y0, 0.06f), new Vector3(x + w / 2 + 0.62f, y0 + hgt, 0.1f), Shutter, LowPoly.F.Front | LowPoly.F.Left | LowPoly.F.Right);
            g.BoxMM(new Vector3(x - w / 2 - 0.1f, y0 - 0.22f, 0), new Vector3(x + w / 2 + 0.1f, y0 - 0.05f, 0.38f), Stone, LowPoly.F.Front | LowPoly.F.Top | LowPoly.F.Left | LowPoly.F.Right | LowPoly.F.Bottom);
            float z = 0.32f;
            for (int i = 0; i < 4; i++)
            {
                float bx = x - w / 2 + (i + 0.5f) * w / 4;
                g.BoxMM(new Vector3(bx - 0.025f, y0 - 0.05f, z - 0.025f), new Vector3(bx + 0.025f, y0 + hgt + 0.1f, z + 0.025f), Iron, LowPoly.F.Front | LowPoly.F.Left | LowPoly.F.Right);
            }
            g.BoxMM(new Vector3(x - w / 2 - 0.05f, y0 + hgt * 0.5f, 0.02f), new Vector3(x + w / 2 + 0.05f, y0 + hgt * 0.5f + 0.05f, z + 0.03f), Iron, LowPoly.F.Front | LowPoly.F.Top);
            g.BoxMM(new Vector3(x - w / 2 - 0.05f, y0 + hgt + 0.1f, 0.02f), new Vector3(x + w / 2 + 0.05f, y0 + hgt + 0.18f, z + 0.03f), Iron, LowPoly.F.Front | LowPoly.F.Top | LowPoly.F.Bottom);
        }

        public static void Balcony(LowPoly g, float x, float y)
        {
            float w = 1.3f;
            g.BoxMM(new Vector3(x - w / 2 - 0.15f, y, 0), new Vector3(x + w / 2 + 0.15f, y + 2.5f, 0.06f), WoodDark, LowPoly.F.Front | LowPoly.F.Top);
            g.BoxMM(new Vector3(x - w / 2, y, 0.06f), new Vector3(x + w / 2, y + 2.3f, 0.1f), Shutter, LowPoly.F.Front);
            g.BoxMM(new Vector3(x - w / 2 + 0.6f, y, 0.1f), new Vector3(x + w / 2 - 0.6f, y + 2.3f, 0.12f), WoodDark, LowPoly.F.Front);
            // slab
            g.BoxMM(new Vector3(x - 1.15f, y - 0.16f, 0), new Vector3(x + 1.15f, y, 1.05f), Stone, LowPoly.F.All & ~LowPoly.F.Back);
            // railing
            g.BoxMM(new Vector3(x - 1.1f, y + 0.95f, 0.95f), new Vector3(x + 1.1f, y + 1.02f, 1.02f), Iron, LowPoly.F.Front | LowPoly.F.Top | LowPoly.F.Bottom);
            g.BoxMM(new Vector3(x - 1.1f, y + 0.95f, 0), new Vector3(x - 1.04f, y + 1.02f, 1.0f), Iron, LowPoly.F.Left | LowPoly.F.Top);
            g.BoxMM(new Vector3(x + 1.04f, y + 0.95f, 0), new Vector3(x + 1.1f, y + 1.02f, 1.0f), Iron, LowPoly.F.Right | LowPoly.F.Top);
            for (int i = 0; i <= 6; i++)
            {
                float bx = x - 1.05f + i * 2.1f / 6;
                g.BoxMM(new Vector3(bx - 0.02f, y, 0.96f), new Vector3(bx + 0.02f, y + 0.95f, 1.0f), Iron, LowPoly.F.Front | LowPoly.F.Left | LowPoly.F.Right);
            }
            // console brackets
            g.Box(new Vector3(x - 0.9f, y - 0.35f, 0.25f), new Vector3(0.12f, 0.35f, 0.5f), Whitewash, LowPoly.F.Front | LowPoly.F.Left | LowPoly.F.Right | LowPoly.F.Bottom);
            g.Box(new Vector3(x + 0.9f, y - 0.35f, 0.25f), new Vector3(0.12f, 0.35f, 0.5f), Whitewash, LowPoly.F.Front | LowPoly.F.Left | LowPoly.F.Right | LowPoly.F.Bottom);
        }

        // ------------------------------------------------------------------ vegetation
        public static void Tree(LowPoly g, Vector3 p, float s, Rng r, bool detail = true)
        {
            int kind = r.Int(0, 2);
            var leaf = r.Chance(0.5f) ? Leaf : LeafDark;
            if (kind == 0) // higuera / ombú: wide crown
            {
                g.Cyl(p, 0.45f * s, 0.32f * s, 2.6f * s, 5, Trunk, false);
                if (!detail) { g.Cyl(p + Vector3.up * 2.4f * s, 2.8f * s, 0.6f * s, 3.2f * s, 5, leaf); return; }
                g.Sphere(p + new Vector3(0, 3.6f, 0) * s, new Vector3(2.9f, 1.8f, 2.9f) * s, 7, 4, leaf);
                g.Sphere(p + new Vector3(1.2f, 4.4f, 0.6f) * s, new Vector3(1.6f, 1.3f, 1.6f) * s, 6, 3, leaf);
            }
            else if (kind == 1) // naranjo: round and small
            {
                g.Cyl(p, 0.18f * s, 0.14f * s, 1.4f * s, 5, Trunk, false);
                if (!detail) { g.Cyl(p + Vector3.up * 1.2f * s, 1.5f * s, 0.3f * s, 2.2f * s, 5, leaf); return; }
                g.Sphere(p + new Vector3(0, 2.2f, 0) * s, new Vector3(1.5f, 1.4f, 1.5f) * s, 6, 4, leaf);
                for (int i = 0; i < 4; i++) g.Box(p + new Vector3(Mathf.Cos(i * 1.7f) * 1.3f, 2.0f + (i % 2) * 0.5f, Mathf.Sin(i * 1.7f) * 1.3f) * s, Vector3.one * 0.16f * s, Palette.Hex(0xe58a2a), LowPoly.F.All);
            }
            else // álamo / sauce: tall
            {
                g.Cyl(p, 0.3f * s, 0.18f * s, 3.5f * s, 5, Trunk, false);
                if (!detail) { g.Cyl(p + Vector3.up * 2.5f * s, 1.6f * s, 0.2f * s, 5.5f * s, 5, leaf); return; }
                g.Lathe(p + Vector3.up * 2.2f * s, new[] { new Vector2(0f, 0f), new Vector2(1.5f * s, 0.6f * s), new Vector2(1.7f * s, 2.6f * s), new Vector2(1.0f * s, 4.8f * s), new Vector2(0f, 6.0f * s) }, 7, leaf);
            }
        }

        void Trees(LowPoly hi, LowPoly lo, float X, float Z)
        {
            int n = rng.Int(3, 6);
            for (int k = 0; k < n; k++)
            {
                float mx = X + rng.Range(City.D + 4, City.B - City.D - 4), mz = Z + rng.Range(City.D + 4, City.B - City.D - 4);
                bool blocked = false;
                foreach (var rr in blockedRects) if (rr.Contains(new Vector2(mx, mz))) blocked = true;
                if (blocked) continue;
                float s = rng.Range(0.85f, 1.4f);
                var seed = rng.Next();
                var p = City.W2U(mx, mz);
                Tree(hi, p, s, new Rng((uint)(seed * 1e6f) + 1));
                Tree(lo, p, s, new Rng((uint)(seed * 1e6f) + 1), false);
            }
        }

        void CornerLanterns(LowPoly g, float X, float Z)
        {
            // oil lanterns on iron brackets at the block corners
            foreach (var (cx, cz, ox, oz) in new[] { (X, Z, -1f, -1f), (X + City.B, Z, 1f, -1f), (X, Z + City.B, -1f, 1f), (X + City.B, Z + City.B, 1f, 1f) })
            {
                var p = City.W2U(cx + ox * 0.15f, cz + oz * 0.15f, 3.6f);
                var d = new Vector3(ox, 0, -oz).normalized * 0.6f;
                g.CylBetween(p, p + d, 0.03f, 0.03f, 4, Iron, false);
                g.Cyl(p + d + Vector3.down * 0.45f, 0.12f, 0.18f, 0.4f, 4, Glass, false, true, Mathf.PI / 4);
                g.Cyl(p + d - Vector3.down * -0.05f + Vector3.down * 0.05f, 0.2f, 0.02f, 0.2f, 4, Iron, true, false, Mathf.PI / 4);
            }
        }

        // ------------------------------------------------------------------ ground, river, outskirts
        public void BuildGround(Material dirt, Material grassMat, Material waterMat)
        {
            float W = City.W, H = City.H;
            var g = new LowPoly();
            // the city ground (streets) and a wide band of countryside around it
            var go = MeshObject("Suelo", Plane(new Vector3((W - 420) / 2, 0, H / 2), W + 420, H + 600, 14f), dirt, root, true, false);
            go.GetComponent<MeshRenderer>().receiveShadows = true;
            var far = MeshObject("Pampa", Plane(new Vector3(W / 2 - 900, -0.05f, H / 2), 3000, 3600, 30f), grassMat, root, false, false);
            far.transform.position = new Vector3(0, -0.05f, 0);
            // barranca down to the river
            var bank = new LowPoly();
            bank.Quad(new Vector3(W, 0, -300), new Vector3(W, 0, H + 300), new Vector3(W + 22, -2.9f, H + 300), new Vector3(W + 22, -2.9f, -300), Sand);
            bank.Quad(new Vector3(W + 22, -2.9f, -300), new Vector3(W + 22, -2.9f, H + 300), new Vector3(W + 70, -2.95f, H + 300), new Vector3(W + 70, -2.95f, -300), Palette.Hex(0xcdb88a));
            MeshObject("Barranca", bank.ToMesh("Barranca"), mat, root, true, false);
            var water = MeshObject("Rio de la Plata", Plane(new Vector3(W + 1300, -2.6f, H / 2), 2500, 4000, 40f), waterMat, root, false, false);
            water.isStatic = false;
            water.AddComponent<WaterDrift>();
            // invisible wall at the water's edge so nobody walks into the river
            var wallGo = new GameObject("Orilla"); wallGo.transform.SetParent(root, false);
            var bc = wallGo.AddComponent<BoxCollider>(); bc.center = new Vector3(W + 60, 0, H / 2); bc.size = new Vector3(2, 20, H + 600);
        }

        static Mesh Plane(Vector3 c, float w, float d, float tile)
        {
            var m = new Mesh { name = "plane" };
            m.vertices = new[] { new Vector3(-w / 2, 0, -d / 2), new Vector3(-w / 2, 0, d / 2), new Vector3(w / 2, 0, d / 2), new Vector3(w / 2, 0, -d / 2) };
            var vs = m.vertices; for (int i = 0; i < 4; i++) vs[i] += c; m.vertices = vs;
            m.uv = new[] { new Vector2(0, 0), new Vector2(0, d / tile), new Vector2(w / tile, d / tile), new Vector2(w / tile, 0) };
            m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        public void BuildOutskirts()
        {
            // quintas and ranchos around the city: low adobe walls, a few huts and lots of trees
            var g = new LowPoly();
            for (int j = -1; j <= City.ROWS; j++)
                for (int i = -1; i < City.COLS; i++)
                {
                    if (i >= 0 && j >= 0 && j < City.ROWS) continue;
                    float X = City.Bx(i), Z = City.Bz(j);
                    var a = City.W2U(X + 2, Z + City.B - 2); var b = City.W2U(X + City.B - 2, Z + 2);
                    float hgt = rng.Range(1.6f, 2.2f);
                    var wallC = Palette.Hex(0xe2d6bd);
                    g.BoxMM(new Vector3(a.x, 0, a.z), new Vector3(b.x, hgt, a.z + 0.6f), wallC, LowPoly.F.All & ~LowPoly.F.Bottom);
                    g.BoxMM(new Vector3(a.x, 0, b.z - 0.6f), new Vector3(b.x, hgt, b.z), wallC, LowPoly.F.All & ~LowPoly.F.Bottom);
                    g.BoxMM(new Vector3(a.x, 0, a.z), new Vector3(a.x + 0.6f, hgt, b.z), wallC, LowPoly.F.All & ~LowPoly.F.Bottom);
                    g.BoxMM(new Vector3(b.x - 0.6f, 0, a.z), new Vector3(b.x, hgt, b.z), wallC, LowPoly.F.All & ~LowPoly.F.Bottom);
                    g.BoxMM(new Vector3(a.x + 0.6f, 0.02f, a.z + 0.6f), new Vector3(b.x - 0.6f, 0.05f, b.z - 0.6f), Grass, LowPoly.F.Top);
                    int huts = rng.Int(1, 2);
                    for (int k = 0; k < huts; k++)
                    {
                        var p = new Vector3(rng.Range(a.x + 10, b.x - 10), 0, rng.Range(a.z + 10, b.z - 10));
                        g.Push(p, rng.Range(0, 90));
                        float w = rng.Range(7, 11), d = rng.Range(5, 7);
                        g.BoxMM(new Vector3(-w / 2, 0, -d / 2), new Vector3(w / 2, 2.6f, d / 2), Palette.Hex(0xd9c9a8));
                        g.GableRoof(new Vector3(0, 2.6f, 0), w + 0.6f, d + 0.9f, 2.0f, Palette.Hex(0xb79a5a), Palette.Hex(0xd9c9a8));  // thatch
                        g.BoxMM(new Vector3(-0.6f, 0, d / 2), new Vector3(0.6f, 2.0f, d / 2 + 0.05f), WoodDark, LowPoly.F.Front);
                        g.Pop();
                    }
                    for (int k = 0; k < 7; k++) Tree(g, new Vector3(rng.Range(a.x + 4, b.x - 4), 0, rng.Range(a.z + 4, b.z - 4)), rng.Range(0.9f, 1.5f), new Rng((uint)(k * 977 + i * 31 + j * 7 + 5)), false);
                    if (g.VertexCount > 50000) { MeshObject("Quintas", g.ToMesh("quintas"), mat, root, false, false); g.Clear(); }
                }
            if (g.VertexCount > 0) MeshObject("Quintas", g.ToMesh("quintas"), mat, root, false, false);
        }

        public void BuildShips()
        {
            float W = City.W;
            foreach (var (x, z, s, yaw) in new[] { (W + 120, 200f, 1f, 20f), (W + 220, 520f, 1.2f, -35f), (W + 90, 700f, 0.9f, 80f), (W + 380, 330f, 1.4f, 10f), (W + 600, 620f, 1.6f, -60f) })
            {
                var g = new LowPoly();
                g.PushM(Matrix4x4.Scale(new Vector3(1, 1, 3.4f)));
                g.Lathe(new Vector3(0, -1.2f, 0), new[] { new Vector2(0f, 0f), new Vector2(1.4f, 0.2f), new Vector2(2.2f, 1.6f), new Vector2(2.3f, 2.6f), new Vector2(0f, 2.6f) }, 8, WoodDark);
                g.Pop();
                g.Box(new Vector3(0, 1.6f, -5.5f), new Vector3(3.6f, 1.2f, 3f), Wood);
                foreach (var (mz, mh) in new[] { (-2.5f, 13f), (3f, 15f) })
                {
                    g.CylBetween(new Vector3(0, 1.3f, mz), new Vector3(0, 1.3f + mh, mz), 0.18f, 0.12f, 5, Wood, false);
                    g.Box(new Vector3(0, mh - 3.5f, mz), new Vector3(5.5f, 5f, 0.1f), Palette.Hex(0xe9e0cb), LowPoly.F.Front | LowPoly.F.Back);
                    g.Box(new Vector3(0, mh - 8.5f, mz), new Vector3(6.5f, 4f, 0.1f), Palette.Hex(0xe9e0cb), LowPoly.F.Front | LowPoly.F.Back);
                }
                var go = MeshObject("Barco", g.ToMesh("barco"), mat, root, false, true);
                go.isStatic = false;
                go.transform.position = new Vector3(x, -2.4f, City.H - z);
                go.transform.rotation = Quaternion.Euler(0, yaw, 0);
                go.transform.localScale = Vector3.one * s;
                go.AddComponent<ShipBob>();
            }
        }
    }

    public class ShipBob : MonoBehaviour
    {
        Vector3 p0; Quaternion r0; float ph;
        void Start() { p0 = transform.position; r0 = transform.rotation; ph = Random.value * 10f; }
        void Update()
        {
            float t = Time.time + ph;
            transform.position = p0 + Vector3.up * Mathf.Sin(t * 0.8f) * 0.12f;
            transform.rotation = r0 * Quaternion.Euler(Mathf.Sin(t * 0.6f) * 1.5f, 0, Mathf.Sin(t * 0.7f) * 2.2f);
        }
    }

    public class WaterDrift : MonoBehaviour
    {
        Material m;
        void Start() { m = GetComponent<Renderer>().material; }
        void Update() { if (m != null) m.SetTextureOffset("_BaseMap", new Vector2(Time.time * 0.004f, Time.time * 0.002f)); }
    }
}
