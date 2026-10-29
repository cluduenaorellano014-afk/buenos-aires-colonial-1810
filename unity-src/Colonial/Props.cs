using UnityEngine;

namespace Colonial
{
    // Low-poly furniture and props shared by the interiors. All take a position on the floor and a yaw.
    public static class Props
    {
        public static readonly Color32 Cloth = Palette.Hex(0x2f5a3a), Velvet = Palette.Hex(0x8a1f22), Gold = Palette.Hex(0xd4a83a), Wax = Palette.Hex(0xf2e8c8),
            Flame = Palette.Hex(0xffc35a), Paper = Palette.Hex(0xefe6cf), Copper = Palette.Hex(0xb8693a), BrickFloor = Palette.Hex(0xa8644a),
            FloorTile = Palette.Hex(0xb87a56), FloorTile2 = Palette.Hex(0x9a5e40), Soap = Palette.Hex(0xf1ead2), Soap2 = Palette.Hex(0xd9cfa8),
            Ash = Palette.Hex(0x8c8a84), Linen = Palette.Hex(0xe9e2d0), Rug = Palette.Hex(0x7a2a2a), Rug2 = Palette.Hex(0xc9a24a), Canvas = Palette.Hex(0x2b241d);
        static Color32 Wood => CityBuilder.Wood; static Color32 WoodDark => CityBuilder.WoodDark; static Color32 Iron => CityBuilder.Iron;

        public static void Table(LowPoly g, Vector3 p, float yaw, float w, float d, Color32 top, float h = 0.78f, bool cloth = false)
        {
            g.Push(p, yaw);
            g.BoxMM(new Vector3(-w / 2, h - 0.06f, -d / 2), new Vector3(w / 2, h, d / 2), cloth ? top : Wood, LowPoly.F.All);
            if (cloth) g.BoxMM(new Vector3(-w / 2 - 0.02f, h - 0.35f, -d / 2 - 0.02f), new Vector3(w / 2 + 0.02f, h - 0.02f, d / 2 + 0.02f), top, LowPoly.F.Front | LowPoly.F.Back | LowPoly.F.Left | LowPoly.F.Right);
            foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 })
                    g.Box(new Vector3(sx * (w / 2 - 0.08f), (h - 0.06f) / 2, sz * (d / 2 - 0.08f)), new Vector3(0.08f, h - 0.06f, 0.08f), WoodDark);
            g.Pop();
        }

        public static void Chair(LowPoly g, Vector3 p, float yaw, Color32 seat)
        {
            g.Push(p, yaw);
            g.Box(new Vector3(0, 0.46f, 0), new Vector3(0.46f, 0.06f, 0.44f), seat, LowPoly.F.All);
            foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 }) g.Box(new Vector3(sx * 0.19f, 0.22f, sz * 0.18f), new Vector3(0.05f, 0.44f, 0.05f), WoodDark);
            g.Box(new Vector3(0, 0.85f, -0.2f), new Vector3(0.46f, 0.7f, 0.05f), seat, LowPoly.F.All);
            g.Box(new Vector3(-0.2f, 0.8f, -0.2f), new Vector3(0.06f, 0.75f, 0.06f), WoodDark); g.Box(new Vector3(0.2f, 0.8f, -0.2f), new Vector3(0.06f, 0.75f, 0.06f), WoodDark);
            g.Pop();
        }

        public static void Bench(LowPoly g, Vector3 p, float yaw, float len, bool back = true)
        {
            g.Push(p, yaw);
            g.Box(new Vector3(0, 0.45f, 0), new Vector3(len, 0.07f, 0.42f), Wood, LowPoly.F.All);
            for (int i = 0; i < 2; i++) g.Box(new Vector3((i == 0 ? -1 : 1) * (len / 2 - 0.15f), 0.22f, 0), new Vector3(0.08f, 0.44f, 0.38f), WoodDark);
            if (back) g.Box(new Vector3(0, 0.8f, -0.19f), new Vector3(len, 0.6f, 0.06f), Wood, LowPoly.F.All);
            g.Pop();
        }

        public static void Candle(LowPoly g, Vector3 p, float h = 0.25f, bool stick = true)
        {
            if (stick) { g.Cyl(p, 0.07f, 0.03f, 0.05f, 6, Gold); g.Cyl(p + Vector3.up * 0.05f, 0.015f, 0.015f, 0.15f, 4, Gold, false); p += Vector3.up * 0.2f; }
            g.Cyl(p, 0.025f, 0.025f, h, 5, Wax);
            g.Cyl(p + Vector3.up * h, 0.018f, 0f, 0.06f, 4, Flame);
        }

        public static void Candelabra(LowPoly g, Vector3 p)
        {
            g.Cyl(p, 0.12f, 0.05f, 0.08f, 6, Gold); g.Cyl(p + Vector3.up * 0.08f, 0.02f, 0.02f, 0.3f, 4, Gold, false);
            for (int i = -1; i <= 1; i++) Candle(g, p + new Vector3(i * 0.14f, 0.38f, 0), 0.18f, false);
            g.Box(p + new Vector3(0, 0.38f, 0), new Vector3(0.3f, 0.02f, 0.02f), Gold);
        }

        public static void Shelf(LowPoly g, Vector3 p, float yaw, float w, float h, int levels, System.Func<int, int, Color32> item = null)
        {
            g.Push(p, yaw);
            g.BoxMM(new Vector3(-w / 2, 0, -0.4f), new Vector3(-w / 2 + 0.06f, h, 0), WoodDark, LowPoly.F.All & ~LowPoly.F.Bottom);
            g.BoxMM(new Vector3(w / 2 - 0.06f, 0, -0.4f), new Vector3(w / 2, h, 0), WoodDark, LowPoly.F.All & ~LowPoly.F.Bottom);
            for (int l = 0; l < levels; l++)
            {
                float y = 0.15f + l * (h - 0.2f) / levels;
                g.BoxMM(new Vector3(-w / 2, y, -0.4f), new Vector3(w / 2, y + 0.04f, 0), Wood, LowPoly.F.All);
                if (item != null)
                {
                    int n = Mathf.FloorToInt(w / 0.22f);
                    for (int k = 0; k < n; k++)
                    {
                        var c = item(l, k);
                        if (c.a == 0) continue;
                        float ih = 0.12f + ((k * 7 + l * 3) % 5) * 0.03f;
                        g.Box(new Vector3(-w / 2 + 0.15f + k * 0.21f, y + 0.04f + ih / 2, -0.2f), new Vector3(0.16f, ih, 0.26f), c, LowPoly.F.All & ~LowPoly.F.Bottom);
                    }
                }
            }
            g.Pop();
        }

        public static void Barrel(LowPoly g, Vector3 p, float s = 1f)
        {
            g.Lathe(p, new[] { new Vector2(0f, 0f), new Vector2(0.3f * s, 0f), new Vector2(0.37f * s, 0.45f * s), new Vector2(0.3f * s, 0.9f * s), new Vector2(0f, 0.9f * s) }, 8, Wood);
            g.Cyl(p + Vector3.up * 0.2f * s, 0.35f * s, 0.36f * s, 0.05f * s, 8, Iron, false);
            g.Cyl(p + Vector3.up * 0.66f * s, 0.355f * s, 0.34f * s, 0.05f * s, 8, Iron, false);
        }

        public static void Sack(LowPoly g, Vector3 p, Color32 c) => g.Sphere(p + Vector3.up * 0.28f, new Vector3(0.3f, 0.3f, 0.24f), 6, 3, c);

        public static void Crate(LowPoly g, Vector3 p, float yaw, float s = 0.6f)
        {
            g.Push(p, yaw); g.Box(new Vector3(0, s / 2, 0), Vector3.one * s, Wood, LowPoly.F.All & ~LowPoly.F.Bottom);
            g.Box(new Vector3(0, s / 2, 0), new Vector3(s + 0.02f, 0.08f, s + 0.02f), WoodDark, LowPoly.F.Front | LowPoly.F.Back | LowPoly.F.Left | LowPoly.F.Right); g.Pop();
        }

        public static void Painting(LowPoly g, Vector3 p, float yaw, float w, float h, Color32 canvas, Color32 figure)
        {
            g.Push(p, yaw);
            g.BoxMM(new Vector3(-w / 2 - 0.12f, -h / 2 - 0.12f, 0), new Vector3(w / 2 + 0.12f, h / 2 + 0.12f, 0.08f), Gold, LowPoly.F.All & ~LowPoly.F.Back);
            g.BoxMM(new Vector3(-w / 2, -h / 2, 0.08f), new Vector3(w / 2, h / 2, 0.1f), canvas, LowPoly.F.Front);
            // a simple portrait silhouette: shoulders and head
            g.BoxMM(new Vector3(-w * 0.32f, -h / 2, 0.1f), new Vector3(w * 0.32f, -h * 0.05f, 0.12f), figure, LowPoly.F.Front | LowPoly.F.Top);
            g.Sphere(new Vector3(0, h * 0.15f, 0.11f), new Vector3(w * 0.16f, h * 0.17f, 0.02f), 6, 3, Palette.Hex(0xd8b08a));
            g.Pop();
        }

        public static void Rugs(LowPoly g, Vector3 c, float w, float d, Color32 a, Color32 b)
        {
            g.BoxMM(c + new Vector3(-w / 2, 0, -d / 2), c + new Vector3(w / 2, 0.02f, d / 2), a, LowPoly.F.Top);
            g.BoxMM(c + new Vector3(-w / 2 + 0.3f, 0, -d / 2 + 0.3f), c + new Vector3(w / 2 - 0.3f, 0.025f, d / 2 - 0.3f), b, LowPoly.F.Top);
            g.BoxMM(c + new Vector3(-w / 2 + 0.45f, 0, -d / 2 + 0.45f), c + new Vector3(w / 2 - 0.45f, 0.03f, d / 2 - 0.45f), a, LowPoly.F.Top);
        }

        public static void Checker(LowPoly g, float x0, float x1, float z0, float z1, float y, float s, Color32 a, Color32 b)
        {
            int nx = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / s)), nz = Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / s));
            float sx = (x1 - x0) / nx, sz = (z1 - z0) / nz;
            for (int i = 0; i < nx; i++) for (int j = 0; j < nz; j++)
                {
                    float xa = x0 + i * sx, za = z0 + j * sz;
                    g.Quad(new Vector3(xa, y, za), new Vector3(xa, y, za + sz), new Vector3(xa + sx, y, za + sz), new Vector3(xa + sx, y, za), (i + j) % 2 == 0 ? a : b);
                }
        }

        public static void Floor(LowPoly g, float x0, float x1, float z0, float z1, float y, Color32 c) => g.Quad(new Vector3(x0, y, z0), new Vector3(x0, y, z1), new Vector3(x1, y, z1), new Vector3(x1, y, z0), c);

        public static void Ceiling(LowPoly g, float x0, float x1, float z0, float z1, float y, Color32 c, Color32 beam, bool beamsAlongX = true)
        {
            g.Quad(new Vector3(x0, y, z0), new Vector3(x1, y, z0), new Vector3(x1, y, z1), new Vector3(x0, y, z1), c);
            if (beamsAlongX) for (float z = z0 + 0.6f; z < z1 - 0.3f; z += 1.2f) g.BoxMM(new Vector3(x0, y - 0.22f, z - 0.1f), new Vector3(x1, y, z + 0.1f), beam, LowPoly.F.Bottom | LowPoly.F.Front | LowPoly.F.Back);
            else for (float x = x0 + 0.6f; x < x1 - 0.3f; x += 1.2f) g.BoxMM(new Vector3(x - 0.1f, y - 0.22f, z0), new Vector3(x + 0.1f, y, z1), beam, LowPoly.F.Bottom | LowPoly.F.Left | LowPoly.F.Right);
        }

        public static void Aljibe(LowPoly g, Vector3 p)
        {
            g.Cyl(p, 0.9f, 0.9f, 0.85f, 10, CityBuilder.Whitewash, true);
            g.Cyl(p + Vector3.up * 0.85f, 0.95f, 0.95f, 0.08f, 10, CityBuilder.Stone);
            g.Cyl(p + Vector3.up * 0.86f, 0.65f, 0.65f, 0.02f, 10, Palette.Hex(0x1a1d1e));
            // wrought-iron arch with pulley
            g.CylBetween(p + new Vector3(-0.8f, 0.9f, 0), p + new Vector3(-0.8f, 2.4f, 0), 0.03f, 0.03f, 4, Iron, false);
            g.CylBetween(p + new Vector3(0.8f, 0.9f, 0), p + new Vector3(0.8f, 2.4f, 0), 0.03f, 0.03f, 4, Iron, false);
            for (int i = 0; i < 6; i++)
            {
                float a0 = Mathf.PI * i / 6, a1 = Mathf.PI * (i + 1) / 6;
                g.CylBetween(p + new Vector3(Mathf.Cos(a0) * 0.8f, 2.4f + Mathf.Sin(a0) * 0.5f, 0), p + new Vector3(Mathf.Cos(a1) * 0.8f, 2.4f + Mathf.Sin(a1) * 0.5f, 0), 0.03f, 0.03f, 4, Iron, false);
            }
            g.Cyl(p + new Vector3(0, 2.55f, 0), 0.12f, 0.12f, 0.05f, 6, Iron, true, true);
            g.Lathe(p + new Vector3(0.3f, 0.93f, 0.2f), new[] { new Vector2(0f, 0f), new Vector2(0.14f, 0f), new Vector2(0.16f, 0.22f), new Vector2(0f, 0.22f) }, 6, Wood);
        }

        public static void Pot(LowPoly g, Vector3 p, float s = 1f, bool plant = true)
        {
            g.Lathe(p, new[] { new Vector2(0f, 0f), new Vector2(0.22f * s, 0f), new Vector2(0.3f * s, 0.45f * s), new Vector2(0.26f * s, 0.55f * s), new Vector2(0f, 0.55f * s) }, 7, Palette.Hex(0xa8502f));
            if (plant) g.Sphere(p + Vector3.up * 0.85f * s, new Vector3(0.45f, 0.4f, 0.45f) * s, 6, 3, CityBuilder.Leaf);
        }

        public static void Bed(LowPoly g, Vector3 p, float yaw)
        {
            g.Push(p, yaw);
            g.BoxMM(new Vector3(-0.8f, 0.25f, -1.0f), new Vector3(0.8f, 0.6f, 1.0f), Linen, LowPoly.F.All);
            g.BoxMM(new Vector3(-0.85f, 0, -1.05f), new Vector3(0.85f, 0.3f, 1.05f), WoodDark, LowPoly.F.All);
            g.BoxMM(new Vector3(-0.75f, 0.6f, -0.95f), new Vector3(0.75f, 0.72f, -0.55f), CityBuilder.Whitewash, LowPoly.F.All);
            g.BoxMM(new Vector3(-0.8f, 0.6f, -0.3f), new Vector3(0.8f, 0.64f, 1.0f), Velvet, LowPoly.F.All);
            foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 }) g.Box(new Vector3(sx * 0.82f, 1.2f, sz * 1.02f), new Vector3(0.07f, 2.4f, 0.07f), WoodDark);
            g.BoxMM(new Vector3(-0.9f, 2.3f, -1.1f), new Vector3(0.9f, 2.5f, 1.1f), Velvet, LowPoly.F.All);
            g.BoxMM(new Vector3(-0.9f, 1.2f, -1.1f), new Vector3(0.9f, 2.3f, -1.06f), Velvet, LowPoly.F.All);
            g.Pop();
        }

        public static void Cannon(LowPoly g, Vector3 p, float yaw)
        {
            g.Push(p, yaw);
            g.BoxMM(new Vector3(-0.45f, 0.25f, -0.9f), new Vector3(0.45f, 0.6f, 0.6f), WoodDark, LowPoly.F.All & ~LowPoly.F.Bottom);
            foreach (var sz in new[] { -0.6f, 0.35f }) foreach (var sx in new[] { -0.5f, 0.5f })
                {
                    g.Push(new Vector3(sx, 0.3f, sz), 0, 0, 90); g.Cyl(Vector3.zero - Vector3.up * 0.05f, 0.3f, 0.3f, 0.1f, 8, Wood, true, true); g.Pop();
                }
            g.CylBetween(new Vector3(0, 0.75f, -1.0f), new Vector3(0, 0.82f, 1.7f), 0.22f, 0.15f, 8, Palette.Hex(0x3a3a36));
            g.Pop();
        }

        public static void CannonBalls(LowPoly g, Vector3 p)
        {
            for (int l = 0; l < 3; l++) for (int i = 0; i < 3 - l; i++) for (int j = 0; j < 3 - l; j++)
                        g.Sphere(p + new Vector3((i - (2 - l) / 2f) * 0.24f, 0.12f + l * 0.19f, (j - (2 - l) / 2f) * 0.24f), Vector3.one * 0.12f, 5, 3, Palette.Hex(0x2a2a2a));
        }

        public static void Cauldron(LowPoly g, Vector3 p)
        {
            // brick stove with a copper boiling pan (paila) and glowing fire mouth
            g.BoxMM(p + new Vector3(-1.1f, 0, -1.1f), p + new Vector3(1.1f, 1.0f, 1.1f), CityBuilder.Brick, LowPoly.F.All & ~LowPoly.F.Bottom);
            g.BoxMM(p + new Vector3(-0.35f, 0.1f, 1.1f), p + new Vector3(0.35f, 0.55f, 1.12f), Palette.Hex(0xff7a2a), LowPoly.F.Front);
            g.Lathe(p + Vector3.up * 0.9f, new[] { new Vector2(0f, 0f), new Vector2(0.55f, 0f), new Vector2(0.85f, 0.45f), new Vector2(0.9f, 0.5f), new Vector2(0.78f, 0.5f), new Vector2(0f, 0.38f) }, 10, Copper);
            g.Cyl(p + Vector3.up * 1.28f, 0.78f, 0.78f, 0.01f, 10, Soap2);
            g.CylBetween(p + new Vector3(0.3f, 1.2f, 0), p + new Vector3(0.9f, 2.4f, 0.3f), 0.03f, 0.03f, 4, CityBuilder.Wood, false);
        }

        public static void Cart(LowPoly g, Vector3 p, float yaw, bool oxen)
        {
            g.Push(p, yaw);
            // carreta: two huge wheels, a box and a curved hide roof
            foreach (var sx in new[] { -1.15f, 1.15f })
            {
                g.Push(new Vector3(sx, 1.25f, 0), 0, 0, 90);
                g.Cyl(new Vector3(0, -0.06f, 0), 1.25f, 1.25f, 0.12f, 10, CityBuilder.WoodDark, true, true);
                g.Pop();
            }
            g.BoxMM(new Vector3(-1.0f, 1.2f, -1.6f), new Vector3(1.0f, 1.3f, 1.6f), CityBuilder.Wood, LowPoly.F.All);
            g.BoxMM(new Vector3(-1.0f, 1.3f, -1.6f), new Vector3(-0.94f, 2.0f, 1.6f), CityBuilder.Wood, LowPoly.F.All);
            g.BoxMM(new Vector3(0.94f, 1.3f, -1.6f), new Vector3(1.0f, 2.0f, 1.6f), CityBuilder.Wood, LowPoly.F.All);
            for (int i = 0; i < 6; i++)
            {
                float a0 = Mathf.PI * i / 6, a1 = Mathf.PI * (i + 1) / 6;
                Vector3 A(float a, float z) => new Vector3(Mathf.Cos(a) * 1.0f, 2.0f + Mathf.Sin(a) * 0.9f, z);
                g.Quad(A(a0, -1.6f), A(a1, -1.6f), A(a1, 1.6f), A(a0, 1.6f), Palette.Hex(0xb08a5a));
                g.Quad(A(a0, 1.6f), A(a1, 1.6f), A(a1, -1.6f), A(a0, -1.6f), Palette.Hex(0x9a7a4a));
            }
            g.CylBetween(new Vector3(0, 1.25f, 1.6f), new Vector3(0, 0.9f, 5.0f), 0.07f, 0.07f, 5, CityBuilder.Wood);
            if (oxen) foreach (var sx in new[] { -0.75f, 0.75f }) Ox(g, new Vector3(sx, 0, 4.6f), 0);
            g.Pop();
        }

        public static void Ox(LowPoly g, Vector3 p, float yaw)
        {
            g.Push(p, yaw);
            var hide = Palette.Hex(0x7a5236);
            g.BoxMM(new Vector3(-0.42f, 0.8f, -1.1f), new Vector3(0.42f, 1.55f, 1.0f), hide, LowPoly.F.All);
            foreach (var sx in new[] { -0.28f, 0.28f }) foreach (var sz in new[] { -0.85f, 0.75f }) g.Box(new Vector3(sx, 0.4f, sz), new Vector3(0.16f, 0.8f, 0.18f), hide);
            g.BoxMM(new Vector3(-0.22f, 1.0f, 1.0f), new Vector3(0.22f, 1.5f, 1.65f), hide, LowPoly.F.All);
            g.CylBetween(new Vector3(-0.2f, 1.45f, 1.3f), new Vector3(-0.55f, 1.75f, 1.4f), 0.04f, 0.01f, 4, Palette.Hex(0xe8dcc0));
            g.CylBetween(new Vector3(0.2f, 1.45f, 1.3f), new Vector3(0.55f, 1.75f, 1.4f), 0.04f, 0.01f, 4, Palette.Hex(0xe8dcc0));
            g.Pop();
        }

        public static void Stall(LowPoly g, Vector3 p, float yaw, Color32 awning, Rng r)
        {
            g.Push(p, yaw);
            foreach (var sx in new[] { -1.4f, 1.4f }) { g.Box(new Vector3(sx, 1.2f, 0.9f), new Vector3(0.1f, 2.4f, 0.1f), CityBuilder.Wood); g.Box(new Vector3(sx, 1.0f, -0.9f), new Vector3(0.1f, 2.0f, 0.1f), CityBuilder.Wood); }
            g.Quad(new Vector3(-1.6f, 2.0f, -1.2f), new Vector3(-1.6f, 2.45f, 1.2f), new Vector3(1.6f, 2.45f, 1.2f), new Vector3(1.6f, 2.0f, -1.2f), awning);
            g.Quad(new Vector3(1.6f, 2.0f, -1.2f), new Vector3(1.6f, 2.45f, 1.2f), new Vector3(-1.6f, 2.45f, 1.2f), new Vector3(-1.6f, 2.0f, -1.2f), awning);
            g.BoxMM(new Vector3(-1.4f, 0.75f, 0.2f), new Vector3(1.4f, 0.85f, 1.0f), CityBuilder.Wood, LowPoly.F.All);
            g.BoxMM(new Vector3(-1.4f, 0, 0.95f), new Vector3(1.4f, 0.75f, 1.0f), CityBuilder.WoodDark, LowPoly.F.Front | LowPoly.F.Back);
            var goods = new[] { Palette.Hex(0xe58a2a), Palette.Hex(0xd8b25a), Palette.Hex(0x8a2d2d), Palette.Hex(0x6b8a3a), Palette.Hex(0xc9b38a), Palette.Hex(0xf1ead2) };
            for (int i = 0; i < 5; i++)
            {
                var c = r.Pick(goods);
                float x = -1.1f + i * 0.55f;
                if (r.Chance(0.5f)) g.Lathe(new Vector3(x, 0.85f, 0.6f), new[] { new Vector2(0f, 0f), new Vector2(0.2f, 0f), new Vector2(0.24f, 0.18f), new Vector2(0f, 0.18f) }, 6, Palette.Hex(0xb08a4a));
                for (int k = 0; k < 4; k++) g.Sphere(new Vector3(x + (k % 2 - 0.5f) * 0.14f, 1.0f + (k / 2) * 0.08f, 0.6f + (k % 2) * 0.05f), Vector3.one * 0.08f, 5, 3, c);
            }
            g.Pop();
        }

        public static void Statue(LowPoly g, Vector3 p, Color32 robe, float s = 1f)
        {
            g.Lathe(p, new[] { new Vector2(0f, 0f), new Vector2(0.3f * s, 0f), new Vector2(0.22f * s, 1.0f * s), new Vector2(0.14f * s, 1.3f * s), new Vector2(0f, 1.35f * s) }, 7, robe);
            g.Sphere(p + Vector3.up * 1.48f * s, Vector3.one * 0.13f * s, 6, 3, Palette.Hex(0xe2c4a0));
            g.Cyl(p + Vector3.up * 1.62f * s, 0.2f * s, 0.2f * s, 0.02f * s, 8, Gold, true, true);
        }

        public static void Cross(LowPoly g, Vector3 p, float h, Color32 c)
        {
            g.Box(p + Vector3.up * h / 2, new Vector3(0.1f, h, 0.1f), c, LowPoly.F.All);
            g.Box(p + Vector3.up * h * 0.72f, new Vector3(h * 0.5f, 0.1f, 0.1f), c, LowPoly.F.All);
        }

        public static void Papers(LowPoly g, Vector3 p, Rng r)
        {
            for (int i = 0; i < 4; i++) { g.Push(p + new Vector3(r.Range(-0.3f, 0.3f), 0.005f * i, r.Range(-0.2f, 0.2f)), r.Range(-30, 30)); g.Box(new Vector3(0, 0.003f, 0), new Vector3(0.21f, 0.006f, 0.29f), Paper, LowPoly.F.Top); g.Pop(); }
            g.Cyl(p + new Vector3(0.4f, 0, 0.1f), 0.04f, 0.04f, 0.07f, 6, Palette.Hex(0x1a1a1a));
            g.CylBetween(p + new Vector3(0.4f, 0.07f, 0.1f), p + new Vector3(0.48f, 0.3f, 0.16f), 0.008f, 0.002f, 3, CityBuilder.Whitewash, false);
        }

        public static void Flag(LowPoly g, Vector3 p, float h)
        {
            // Spanish ensign of 1785: red-yellow-red
            g.CylBetween(p, p + Vector3.up * h, 0.1f, 0.06f, 6, CityBuilder.WoodDark);
            float w = 4.2f, fh = 2.8f, y = p.y + h - fh - 0.3f;
            var x0 = p.x + 0.1f;
            Color32 red = Palette.Hex(0xb02020), yellow = Palette.Hex(0xe8c030);
            float[] bands = { 0, 0.25f, 0.75f, 1f };
            for (int b = 0; b < 3; b++)
            {
                float y0 = y + fh * bands[b], y1 = y + fh * bands[b + 1];
                var c = b == 1 ? yellow : red;
                g.Quad(new Vector3(x0, y0, p.z), new Vector3(x0, y1, p.z), new Vector3(x0 + w, y1, p.z), new Vector3(x0 + w, y0, p.z), c);
                g.Quad(new Vector3(x0 + w, y0, p.z), new Vector3(x0 + w, y1, p.z), new Vector3(x0, y1, p.z), new Vector3(x0, y0, p.z), c);
            }
        }
    }
}
