using System.Collections.Generic;
using UnityEngine;

namespace Colonial
{
    // Flat-shaded low-poly mesh builder. Every face gets its own vertices (hard edges) and its UV
    // points at a cell of the shared palette texture, so the whole city uses ONE material.
    public class LowPoly
    {
        public readonly List<Vector3> V = new List<Vector3>();
        public readonly List<Vector2> UV = new List<Vector2>();
        public readonly List<int> T = new List<int>();
        public readonly List<BoneWeight> BW = new List<BoneWeight>();
        public int bone = -1;              // when >= 0 new vertices are skinned to this bone
        Matrix4x4 m = Matrix4x4.identity;
        readonly Stack<Matrix4x4> stack = new Stack<Matrix4x4>();

        public int VertexCount => V.Count;

        // ---------------------------------------------------------------- transform stack
        public LowPoly Push(Vector3 pos, float yawDeg = 0f, float pitchDeg = 0f, float rollDeg = 0f)
        {
            stack.Push(m);
            m = m * Matrix4x4.TRS(pos, Quaternion.Euler(pitchDeg, yawDeg, rollDeg), Vector3.one);
            return this;
        }
        public LowPoly PushM(Matrix4x4 mm) { stack.Push(m); m = m * mm; return this; }
        public LowPoly Pop() { m = stack.Pop(); return this; }

        // ---------------------------------------------------------------- primitives
        public void Tri(Vector3 a, Vector3 b, Vector3 c, Color32 col)
        {
            int i = V.Count;
            Vector2 uv = Palette.UV(col);
            V.Add(m.MultiplyPoint3x4(a)); V.Add(m.MultiplyPoint3x4(b)); V.Add(m.MultiplyPoint3x4(c));
            UV.Add(uv); UV.Add(uv); UV.Add(uv);
            if (bone >= 0) { var w = new BoneWeight { boneIndex0 = bone, weight0 = 1f }; BW.Add(w); BW.Add(w); BW.Add(w); }
            T.Add(i); T.Add(i + 1); T.Add(i + 2);
        }

        // clockwise when seen from the front (Unity winding)
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color32 col)
        {
            Tri(a, b, c, col); Tri(a, c, d, col);
        }

        public void Poly(IList<Vector3> pts, Color32 col)
        {
            for (int i = 1; i < pts.Count - 1; i++) Tri(pts[0], pts[i], pts[i + 1], col);
        }

        [System.Flags] public enum F { None = 0, Top = 1, Bottom = 2, Front = 4, Back = 8, Left = 16, Right = 32, All = 63, NoBottom = 61 }

        // axis-aligned box (in the current local frame), center c, size s
        public void Box(Vector3 c, Vector3 s, Color32 col, F faces = F.NoBottom)
        {
            Vector3 h = s * 0.5f;
            Vector3 p000 = c + new Vector3(-h.x, -h.y, -h.z), p100 = c + new Vector3(h.x, -h.y, -h.z),
                    p010 = c + new Vector3(-h.x, h.y, -h.z), p110 = c + new Vector3(h.x, h.y, -h.z),
                    p001 = c + new Vector3(-h.x, -h.y, h.z), p101 = c + new Vector3(h.x, -h.y, h.z),
                    p011 = c + new Vector3(-h.x, h.y, h.z), p111 = c + new Vector3(h.x, h.y, h.z);
            if ((faces & F.Top) != 0) Quad(p010, p011, p111, p110, col);
            if ((faces & F.Bottom) != 0) Quad(p000, p100, p101, p001, col);
            if ((faces & F.Back) != 0) Quad(p000, p010, p110, p100, col);    // -z
            if ((faces & F.Front) != 0) Quad(p101, p111, p011, p001, col);   // +z
            if ((faces & F.Left) != 0) Quad(p001, p011, p010, p000, col);    // -x
            if ((faces & F.Right) != 0) Quad(p100, p110, p111, p101, col);   // +x
        }

        // box from min corner to max corner
        public void BoxMM(Vector3 min, Vector3 max, Color32 col, F faces = F.NoBottom) => Box((min + max) * 0.5f, max - min, col, faces);

        // gable roof prism along local x: width w (x), depth d (z), ridge height rh, eaves at y0
        public void GableRoof(Vector3 c, float w, float d, float rh, Color32 col, Color32 gable, bool gableEnds = true)
        {
            float x0 = c.x - w / 2, x1 = c.x + w / 2, z0 = c.z - d / 2, z1 = c.z + d / 2, y0 = c.y, y1 = c.y + rh;
            var a = new Vector3(x0, y0, z0); var b = new Vector3(x1, y0, z0);
            var r0 = new Vector3(x0, y1, c.z); var r1 = new Vector3(x1, y1, c.z);
            var e = new Vector3(x0, y0, z1); var f = new Vector3(x1, y0, z1);
            Quad(a, r0, r1, b, col);   // -z slope
            Quad(f, r1, r0, e, col);   // +z slope
            if (gableEnds) { Tri(e, r0, a, gable); Tri(b, r1, f, gable); }
        }

        // hip/pyramid roof over a rectangle
        public void HipRoof(Vector3 c, float w, float d, float rh, Color32 col)
        {
            float x0 = c.x - w / 2, x1 = c.x + w / 2, z0 = c.z - d / 2, z1 = c.z + d / 2, y0 = c.y;
            float inset = Mathf.Min(w, d) / 2;
            var top0 = new Vector3(Mathf.Min(c.x, x0 + inset), y0 + rh, c.z);
            var top1 = new Vector3(Mathf.Max(c.x, x1 - inset), y0 + rh, c.z);
            var a = new Vector3(x0, y0, z0); var b = new Vector3(x1, y0, z0); var cc = new Vector3(x1, y0, z1); var dd = new Vector3(x0, y0, z1);
            Quad(a, top0, top1, b, col);
            Quad(cc, top1, top0, dd, col);
            Tri(dd, top0, a, col);
            Tri(b, top1, cc, col);
        }

        // vertical cylinder / truncated cone along +y from y0, n sides
        public void Cyl(Vector3 c, float r0, float r1, float h, int n, Color32 col, bool top = true, bool bottom = false, float phase = 0f)
        {
            for (int i = 0; i < n; i++)
            {
                float a0 = phase + i * Mathf.PI * 2 / n, a1 = phase + (i + 1) * Mathf.PI * 2 / n;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                Vector3 b0 = c + d0 * r0, b1 = c + d1 * r0, t0 = c + d0 * r1 + Vector3.up * h, t1 = c + d1 * r1 + Vector3.up * h;
                Quad(b0, t0, t1, b1, col);
                if (top && r1 > 0.0001f) Tri(c + Vector3.up * h, t1, t0, col);
                if (bottom && r0 > 0.0001f) Tri(c, b0, b1, col);
            }
        }

        // cylinder between two points (for poles, cannon barrels, legs of furniture)
        public void CylBetween(Vector3 a, Vector3 b, float r0, float r1, int n, Color32 col, bool caps = true)
        {
            Vector3 dir = b - a; float len = dir.magnitude;
            PushM(Matrix4x4.TRS(a, Quaternion.FromToRotation(Vector3.up, dir / len), Vector3.one));
            Cyl(Vector3.zero, r0, r1, len, n, col, caps, caps);
            Pop();
        }

        // low-poly sphere (UV sphere with few segments), squashed by scale
        public void Sphere(Vector3 c, Vector3 r, int seg, int rings, Color32 col)
        {
            for (int j = 0; j < rings; j++)
            {
                float v0 = Mathf.PI * j / rings - Mathf.PI / 2, v1 = Mathf.PI * (j + 1) / rings - Mathf.PI / 2;
                for (int i = 0; i < seg; i++)
                {
                    float u0 = i * Mathf.PI * 2 / seg, u1 = (i + 1) * Mathf.PI * 2 / seg;
                    Vector3 P(float u, float v) => c + Vector3.Scale(new Vector3(Mathf.Cos(v) * Mathf.Cos(u), Mathf.Sin(v), Mathf.Cos(v) * Mathf.Sin(u)), r);
                    Vector3 a = P(u0, v0), b = P(u1, v0), cc = P(u1, v1), d = P(u0, v1);
                    if (j == 0) Tri(a, d, cc, col);
                    else if (j == rings - 1) Tri(a, d, b, col);
                    else Quad(a, d, cc, b, col);
                }
            }
        }

        // lathe a (radius, y) profile around the y axis
        public void Lathe(Vector3 c, IList<Vector2> prof, int n, Color32 col, float phase = 0f)
        {
            for (int i = 0; i < n; i++)
            {
                float a0 = phase + i * Mathf.PI * 2 / n, a1 = phase + (i + 1) * Mathf.PI * 2 / n;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                for (int k = 0; k < prof.Count - 1; k++)
                {
                    Vector3 b0 = c + d0 * prof[k].x + Vector3.up * prof[k].y, b1 = c + d1 * prof[k].x + Vector3.up * prof[k].y;
                    Vector3 t0 = c + d0 * prof[k + 1].x + Vector3.up * prof[k + 1].y, t1 = c + d1 * prof[k + 1].x + Vector3.up * prof[k + 1].y;
                    if (prof[k].x < 0.0001f) Tri(b0, t0, t1, col);
                    else if (prof[k + 1].x < 0.0001f) Tri(b0, t0, b1, col);
                    else Quad(b0, t0, t1, b1, col);
                }
            }
        }

        // Wall slab in the local XY plane (thickness along z, centered on z=0) from x0..x1, y0..y1,
        // with rectangular / round-arched openings. Built as solid pieces around the openings.
        public struct Opening { public float x0, x1, y0, y1; public bool arch; public Opening(float a, float b, float c, float d, bool ar = false) { x0 = a; x1 = b; y0 = c; y1 = d; arch = ar; } }

        public void Wall(float x0, float x1, float y0, float y1, float t, Color32 col, params Opening[] ops)
        {
            var list = new List<Opening>(ops);
            list.Sort((a, b) => a.x0.CompareTo(b.x0));
            float cur = x0;
            foreach (var o in list)
            {
                if (o.x0 > cur) BoxMM(new Vector3(cur, y0, -t / 2), new Vector3(o.x0, y1, t / 2), col, F.All & ~F.Bottom);
                // below the opening (sill)
                if (o.y0 > y0) BoxMM(new Vector3(o.x0, y0, -t / 2), new Vector3(o.x1, o.y0, t / 2), col, F.Top | F.Front | F.Back);
                if (o.arch)
                {
                    // semicircular arch: springing at y1 - r
                    float r = (o.x1 - o.x0) / 2, cx = (o.x0 + o.x1) / 2, sy = o.y1 - r;
                    const int N = 6;
                    for (int i = 0; i < N; i++)
                    {
                        float a0 = Mathf.PI * i / N, a1 = Mathf.PI * (i + 1) / N;
                        Vector3 p0 = new Vector3(cx + Mathf.Cos(a0) * r, sy + Mathf.Sin(a0) * r, 0), p1 = new Vector3(cx + Mathf.Cos(a1) * r, sy + Mathf.Sin(a1) * r, 0);
                        // fill between the arc segment and the top corner (spandrel) as a fan to the corner
                        Vector3 corner0 = new Vector3(Mathf.Cos((a0 + a1) / 2) >= 0 ? o.x1 : o.x0, y1, 0);
                        Vector3 z = new Vector3(0, 0, t / 2);
                        Tri(p0 + z, corner0 + z, p1 + z, col);          // front (+z faces toward viewer at +z)
                        Tri(p1 - z, corner0 - z, p0 - z, col);
                        // intrados (soffit)
                        Quad(p0 - z, p0 + z, p1 + z, p1 - z, col);
                    }
                    // the two triangles at the crown between the corners and the apex
                    Vector3 apex = new Vector3(cx, o.y1, 0), zz = new Vector3(0, 0, t / 2);
                    Tri(new Vector3(o.x1, y1, 0) + zz, new Vector3(o.x0, y1, 0) + zz, apex + zz, col);
                    Tri(new Vector3(o.x0, y1, 0) - zz, new Vector3(o.x1, y1, 0) - zz, apex - zz, col);
                    // the jambs are the side faces of the neighbouring wall pieces
                }
                else if (o.y1 < y1) BoxMM(new Vector3(o.x0, o.y1, -t / 2), new Vector3(o.x1, y1, t / 2), col, F.Bottom | F.Front | F.Back | F.Top);
                cur = o.x1;
            }
            if (cur < x1) BoxMM(new Vector3(cur, y0, -t / 2), new Vector3(x1, y1, t / 2), col, F.All & ~F.Bottom);
        }

        // straight staircase rising along +z from local origin
        public void Stairs(Vector3 start, float width, float rise, float run, int steps, Color32 col, Color32 side)
        {
            for (int i = 0; i < steps; i++)
            {
                float y = rise * (i + 1) / steps, z0 = run * i / steps;
                BoxMM(start + new Vector3(-width / 2, 0, z0), start + new Vector3(width / 2, y, z0 + run / steps), i % 2 == 0 ? col : side, F.Top | F.Back | F.Left | F.Right);
            }
        }

        // ---------------------------------------------------------------- output
        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (V.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(V);
            mesh.SetUVs(0, UV);
            mesh.SetTriangles(T, 0);
            if (BW.Count == V.Count && BW.Count > 0) mesh.boneWeights = BW.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public void Clear() { V.Clear(); UV.Clear(); T.Clear(); BW.Clear(); m = Matrix4x4.identity; stack.Clear(); bone = -1; }
    }

    // Shared color palette: every distinct color gets a 4x4-pixel cell in a 256x256 texture.
    public static class Palette
    {
        public const int Size = 256, Cell = 4, PerRow = Size / Cell;
        static readonly Dictionary<int, int> index = new Dictionary<int, int>();
        static readonly List<Color32> colors = new List<Color32>();

        public static void Reset() { index.Clear(); colors.Clear(); }

        public static Vector2 UV(Color32 c)
        {
            int key = (c.r << 16) | (c.g << 8) | c.b;
            if (!index.TryGetValue(key, out int i))
            {
                i = colors.Count;
                if (i >= PerRow * PerRow) { Debug.LogWarning("Palette full"); i = 0; }
                else { index[key] = i; colors.Add(c); }
            }
            int cx = i % PerRow, cy = i / PerRow;
            return new Vector2((cx * Cell + Cell * 0.5f) / Size, (cy * Cell + Cell * 0.5f) / Size);
        }

        public static Texture2D BuildTexture()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "ColonialPalette" };
            var px = new Color32[Size * Size];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 0, 255, 255);
            for (int i = 0; i < colors.Count; i++)
            {
                int cx = i % PerRow, cy = i / PerRow;
                for (int y = 0; y < Cell; y++) for (int x = 0; x < Cell; x++) px[(cy * Cell + y) * Size + cx * Cell + x] = colors[i];
            }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        public static int Count => colors.Count;
        public static Color32 Hex(string hex) { ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var c); return c; }
        public static Color32 Hex(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
    }
}
