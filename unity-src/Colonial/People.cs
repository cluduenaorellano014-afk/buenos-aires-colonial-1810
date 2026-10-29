using UnityEngine;

namespace Colonial
{
    public enum Outfit { Caballero, Levita, Gaucho, Patricio, Fraile, Dama, Criolla, Vendedora, Peon, Virrey }

    public class PersonSpec
    {
        public Outfit outfit;
        public Color32 skin, hair, coat, vest, legs, skirt, accent;
        public string hat;    // tri, bi, galera, gorra, sombrero, none, peineta, mantilla, panuelo, capucha
        public string item;   // umbrella, basket, olla, candles, tray, brooms, fish, musket, cane, book, mate, none
        public float height = 1f;
        public bool Woman => outfit == Outfit.Dama || outfit == Outfit.Criolla || outfit == Outfit.Vendedora;

        static readonly Color32[] Skins = { Palette.Hex(0xe9c3a0), Palette.Hex(0xd8a77f), Palette.Hex(0xc28a62), Palette.Hex(0x9a6a45), Palette.Hex(0x6e4a32), Palette.Hex(0xf0cfb0) };
        static readonly Color32[] Hairs = { Palette.Hex(0x2a1d14), Palette.Hex(0x4a3020), Palette.Hex(0x1b1715), Palette.Hex(0xd8d4cc), Palette.Hex(0x6b4a2a) };
        static readonly Color32[] Coats = { Palette.Hex(0x2f3d5c), Palette.Hex(0x5a2b2b), Palette.Hex(0x2f4a35), Palette.Hex(0x3b3b3b), Palette.Hex(0x6b5233), Palette.Hex(0x1f2a44), Palette.Hex(0x7a3b22), Palette.Hex(0x4b3a5a) };
        static readonly Color32[] Vests = { Palette.Hex(0xe8d9b0), Palette.Hex(0xc9a24a), Palette.Hex(0xf2efe6), Palette.Hex(0x8a2d2d), Palette.Hex(0x9fb0b0) };
        static readonly Color32[] Dresses = { Palette.Hex(0x8a2d3c), Palette.Hex(0x2f4f7a), Palette.Hex(0x3e6b4a), Palette.Hex(0xd9c08a), Palette.Hex(0x6b3a6b), Palette.Hex(0xb8673a), Palette.Hex(0xe6dccb), Palette.Hex(0x23395b) };
        static readonly Color32[] Simple = { Palette.Hex(0xcbbd9e), Palette.Hex(0xa58a62), Palette.Hex(0x8c7a5c), Palette.Hex(0xe6dccb), Palette.Hex(0x7b6a4a) };

        public static PersonSpec Random(Rng r, Outfit o)
        {
            var p = new PersonSpec { outfit = o, skin = r.Pick(Skins), hair = r.Pick(Hairs), coat = r.Pick(Coats), vest = r.Pick(Vests), legs = Palette.Hex(0xece6d6), skirt = r.Pick(Dresses), accent = r.Pick(Vests), height = r.Range(0.94f, 1.05f), item = "none" };
            switch (o)
            {
                case Outfit.Caballero: p.hat = r.Chance(0.75f) ? "tri" : "bi"; if (r.Chance(0.15f)) p.item = r.Chance(0.5f) ? "umbrella" : "cane"; break;
                case Outfit.Levita: p.hat = r.Chance(0.6f) ? "galera" : "bi"; p.legs = r.Pick(Simple); if (r.Chance(0.25f)) p.item = "cane"; break;
                case Outfit.Gaucho: p.hat = r.Chance(0.6f) ? "sombrero" : "panuelo"; p.coat = r.Pick(Simple); p.vest = r.Pick(Dresses); p.legs = Palette.Hex(0xf2efe6); break;
                case Outfit.Peon: p.hat = r.Chance(0.6f) ? "gorra" : "none"; p.coat = r.Pick(Simple); p.vest = r.Pick(Simple); p.legs = r.Pick(Simple); break;
                case Outfit.Patricio: p.hat = "bi"; p.coat = Palette.Hex(0x1f2a44); p.vest = Palette.Hex(0xf2efe6); p.accent = Palette.Hex(0xa32a2a); p.legs = Palette.Hex(0xf2efe6); p.item = "musket"; break;
                case Outfit.Fraile: p.hat = "capucha"; p.coat = r.Chance(0.5f) ? Palette.Hex(0x5a4330) : Palette.Hex(0xf2efe6); p.accent = Palette.Hex(0x1b1715); p.item = r.Chance(0.4f) ? "book" : "none"; break;
                case Outfit.Dama: p.hat = r.Chance(0.6f) ? "peineta" : "mantilla"; if (r.Chance(0.3f)) p.item = "umbrella"; break;
                case Outfit.Criolla: p.hat = r.Chance(0.5f) ? "mantilla" : "panuelo"; p.skirt = r.Pick(Simple); if (r.Chance(0.3f)) p.item = "basket"; break;
                case Outfit.Vendedora: p.hat = "panuelo"; p.skirt = r.Pick(Dresses); p.vest = Palette.Hex(0xf2efe6); break;
                case Outfit.Virrey: p.hat = "none"; p.coat = Palette.Hex(0x7a1f22); p.vest = Palette.Hex(0xe8d9b0); p.accent = Palette.Hex(0xc9a24a); p.hair = Palette.Hex(0xe8e6e0); break;
            }
            return p;
        }
    }

    // Builds a rigidly skinned low-poly person: one mesh + 7 bones (hips, torso, head, arms, legs).
    // One draw call per person; PersonRig animates the bones procedurally.
    public static class PersonFactory
    {
        public const int Hips = 0, Torso = 1, Head = 2, ArmL = 3, ArmR = 4, LegL = 5, LegR = 6;
        static readonly Color32 Black = Palette.Hex(0x1b1715), Shoe = Palette.Hex(0x2a1d14), White = Palette.Hex(0xf2efe6), Gold = Palette.Hex(0xc9a24a), Iron = Palette.Hex(0x2a2a2c), Wood = Palette.Hex(0x5b3a1e);

        public static GameObject Build(PersonSpec p, Material mat, string name)
        {
            var root = new GameObject(name);
            var bones = new Transform[7];
            Transform B(string n, Transform parent, Vector3 pos) { var t = new GameObject(n).transform; t.SetParent(parent, false); t.localPosition = pos; return t; }
            bones[Hips] = B("Hips", root.transform, new Vector3(0, 0.95f, 0));
            bones[Torso] = B("Torso", bones[Hips], Vector3.zero);
            bones[Head] = B("Head", bones[Torso], new Vector3(0, 0.6f, 0));
            bones[ArmL] = B("ArmL", bones[Torso], new Vector3(-0.21f, 0.53f, 0));
            bones[ArmR] = B("ArmR", bones[Torso], new Vector3(0.21f, 0.53f, 0));
            bones[LegL] = B("LegL", bones[Hips], new Vector3(-0.1f, 0, 0));
            bones[LegR] = B("LegR", bones[Hips], new Vector3(0.1f, 0, 0));

            var lp = new LowPoly();
            BuildBody(lp, p);
            var mesh = lp.ToMesh(name);
            var bind = new Matrix4x4[7];
            for (int i = 0; i < 7; i++) bind[i] = bones[i].worldToLocalMatrix * root.transform.localToWorldMatrix;
            mesh.bindposes = bind;

            var smr = new GameObject("Body").AddComponent<SkinnedMeshRenderer>();
            smr.transform.SetParent(root.transform, false);
            smr.sharedMesh = mesh;
            smr.bones = bones;
            smr.rootBone = bones[Hips];
            smr.sharedMaterial = mat;
            smr.updateWhenOffscreen = false;
            smr.localBounds = new Bounds(new Vector3(0, -0.05f, 0.1f), new Vector3(1.6f, 2.4f, 1.8f));
            smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            smr.quality = SkinQuality.Bone1;

            var rig = root.AddComponent<PersonRig>();
            rig.bones = bones;
            rig.woman = p.Woman;
            rig.holdsItem = p.item != "none" && p.item != null;
            root.transform.localScale = Vector3.one * p.height;
            return root;
        }

        static void BuildBody(LowPoly g, PersonSpec p)
        {
            bool woman = p.Woman;
            // ---------------- legs (hip pivot at y 0.95) ----------------
            for (int s = -1; s <= 1; s += 2)
            {
                g.bone = s < 0 ? LegL : LegR;
                float x = s * 0.1f;
                if (!woman && p.outfit != Outfit.Fraile)
                {
                    bool breeches = p.outfit == Outfit.Caballero || p.outfit == Outfit.Patricio || p.outfit == Outfit.Virrey;
                    Color32 lower = breeches ? p.legs : (p.outfit == Outfit.Gaucho ? White : p.legs);
                    Color32 upper = breeches ? (p.outfit == Outfit.Patricio ? White : p.vest.r > 200 ? p.coat : Palette.Hex(0x3b3b3b)) : lower;
                    if (p.outfit == Outfit.Virrey) upper = p.coat;
                    g.Cyl(new Vector3(x, 0.08f, 0), 0.055f, 0.065f, 0.45f, 6, lower);
                    g.Cyl(new Vector3(x, 0.5f, 0), 0.075f, 0.09f, 0.45f, 6, upper);
                    if (p.outfit == Outfit.Patricio) g.Cyl(new Vector3(x, 0.08f, 0), 0.07f, 0.07f, 0.36f, 6, Black); // gaiters
                }
                // shoes
                g.Box(new Vector3(x, 0.04f, 0.04f), new Vector3(0.11f, 0.08f, 0.24f), p.outfit == Outfit.Gaucho ? Palette.Hex(0xc9b38a) : Shoe);
            }

            // ---------------- hips / lower garments (stay with hips) ----------------
            g.bone = Hips;
            if (woman)
            {
                // wide skirt from the waist down to the ankles
                g.Lathe(new Vector3(0, 0.95f, 0), new[] { new Vector2(0.0f, 0.12f - 0.95f), new Vector2(0.38f, 0.1f - 0.95f), new Vector2(0.33f, 0.45f - 0.95f), new Vector2(0.2f, 0.98f - 0.95f), new Vector2(0f, 0.98f - 0.95f) }, 9, p.skirt);
                if (p.outfit != Outfit.Dama) g.Box(new Vector3(0, 0.62f, 0.2f), new Vector3(0.36f, 0.62f, 0.04f), White); // apron
            }
            else if (p.outfit == Outfit.Fraile)
            {
                g.Lathe(new Vector3(0, 0.95f, 0), new[] { new Vector2(0f, -0.88f), new Vector2(0.3f, -0.9f), new Vector2(0.22f, 0.05f), new Vector2(0f, 0.05f) }, 8, p.coat);
                g.Cyl(new Vector3(0, 0.9f, 0), 0.23f, 0.23f, 0.05f, 8, Palette.Hex(0xd8c79e), false); // cord
            }
            else if (p.outfit == Outfit.Gaucho)
            {
                g.Lathe(new Vector3(0, 0.95f, 0), new[] { new Vector2(0f, -0.45f), new Vector2(0.26f, -0.47f), new Vector2(0.2f, 0.06f), new Vector2(0f, 0.06f) }, 8, p.vest); // chiripá
                g.Cyl(new Vector3(0, 0.95f, 0), 0.2f, 0.2f, 0.08f, 8, Palette.Hex(0x5b3a1e), false); // tirador
            }
            else
            {
                g.Cyl(new Vector3(0, 0.86f, 0), 0.17f, 0.19f, 0.16f, 8, p.outfit == Outfit.Patricio ? White : p.legs.r > 200 && p.outfit != Outfit.Levita ? Palette.Hex(0x3b3b3b) : p.legs);
                if (p.outfit == Outfit.Caballero || p.outfit == Outfit.Levita || p.outfit == Outfit.Patricio || p.outfit == Outfit.Virrey)
                {
                    // coat tails behind the legs
                    g.Push(new Vector3(0, 0.95f, -0.08f), 0, 8f);
                    g.Box(new Vector3(0, -0.28f, -0.05f), new Vector3(0.34f, 0.56f, 0.05f), p.coat, LowPoly.F.All);
                    g.Pop();
                }
            }

            // ---------------- torso ----------------
            g.bone = Torso;
            Color32 shirt = p.outfit == Outfit.Gaucho || p.outfit == Outfit.Peon ? p.vest : p.coat;
            if (woman)
            {
                Color32 bodice = p.outfit == Outfit.Dama ? p.skirt : p.vest;
                g.Lathe(new Vector3(0, 0.95f, 0), new[] { new Vector2(0f, 0f), new Vector2(0.16f, 0f), new Vector2(0.2f, 0.38f), new Vector2(0.17f, 0.52f), new Vector2(0.07f, 0.6f), new Vector2(0f, 0.6f) }, 8, bodice);
                if (p.outfit == Outfit.Dama) g.Box(new Vector3(0, 0.95f + 0.47f, 0.13f), new Vector3(0.22f, 0.12f, 0.06f), White); // lace
                else g.Lathe(new Vector3(0, 0.95f + 0.36f, 0), new[] { new Vector2(0.21f, 0f), new Vector2(0.21f, 0.06f), new Vector2(0.17f, 0.18f), new Vector2(0.08f, 0.24f) }, 8, p.accent); // shawl
            }
            else if (p.outfit == Outfit.Fraile)
            {
                g.Lathe(new Vector3(0, 0.95f, 0), new[] { new Vector2(0f, 0f), new Vector2(0.21f, 0f), new Vector2(0.21f, 0.45f), new Vector2(0.14f, 0.58f), new Vector2(0f, 0.6f) }, 8, p.coat);
            }
            else
            {
                g.Lathe(new Vector3(0, 0.95f, 0), new[] { new Vector2(0f, 0f), new Vector2(0.18f, 0f), new Vector2(0.21f, 0.35f), new Vector2(0.19f, 0.52f), new Vector2(0.08f, 0.6f), new Vector2(0f, 0.6f) }, 8, shirt);
                if (p.outfit == Outfit.Caballero || p.outfit == Outfit.Levita || p.outfit == Outfit.Virrey)
                {
                    g.Box(new Vector3(0, 0.95f + 0.3f, 0.17f), new Vector3(0.16f, 0.44f, 0.04f), p.vest);     // waistcoat
                    g.Box(new Vector3(0, 0.95f + 0.52f, 0.16f), new Vector3(0.1f, 0.1f, 0.06f), White);       // cravat
                }
                if (p.outfit == Outfit.Patricio)
                {
                    g.Box(new Vector3(0, 0.95f + 0.32f, 0.17f), new Vector3(0.18f, 0.44f, 0.04f), p.accent);  // red lapels
                    g.Push(new Vector3(0, 0.95f + 0.3f, 0), 0, 0, 35f); g.Box(new Vector3(0, 0, 0.19f), new Vector3(0.05f, 0.62f, 0.03f), White); g.Pop();
                    g.Push(new Vector3(0, 0.95f + 0.3f, 0), 0, 0, -35f); g.Box(new Vector3(0, 0, -0.19f), new Vector3(0.05f, 0.62f, 0.03f), White); g.Pop();
                }
                if (p.outfit == Outfit.Virrey)
                {
                    g.Push(new Vector3(0, 0.95f + 0.3f, 0), 0, 0, 30f); g.Box(new Vector3(0, 0, 0.2f), new Vector3(0.07f, 0.66f, 0.03f), Palette.Hex(0x2f6fa8)); g.Pop(); // sash
                }
                if (p.outfit == Outfit.Gaucho)
                {
                    // poncho: a wide low-poly pyramid over the shoulders
                    g.Lathe(new Vector3(0, 0.95f + 0.1f, 0), new[] { new Vector2(0.42f, 0f), new Vector2(0.42f, 0.04f), new Vector2(0.12f, 0.5f), new Vector2(0.08f, 0.52f) }, 4, p.vest, Mathf.PI / 4);
                }
            }
            // neck
            g.Cyl(new Vector3(0, 0.95f + 0.56f, 0), 0.055f, 0.05f, 0.08f, 6, p.skin, false);

            // ---------------- arms (pivot at shoulder) ----------------
            for (int s = -1; s <= 1; s += 2)
            {
                g.bone = s < 0 ? ArmL : ArmR;
                float x = s * 0.21f; float sy = 0.95f + 0.53f;
                Color32 sleeve = woman ? (p.outfit == Outfit.Dama ? p.skirt : p.vest) : (p.outfit == Outfit.Gaucho ? p.vest : shirt);
                if (p.outfit == Outfit.Gaucho) sleeve = White;
                g.Cyl(new Vector3(x, sy - 0.56f, 0), 0.05f, 0.065f, 0.56f, 6, sleeve);
                if (!woman && (p.outfit == Outfit.Caballero || p.outfit == Outfit.Patricio || p.outfit == Outfit.Virrey)) g.Cyl(new Vector3(x, sy - 0.5f, 0), 0.068f, 0.068f, 0.09f, 6, p.outfit == Outfit.Patricio ? p.accent : p.vest, false); // cuffs
                g.Sphere(new Vector3(x, sy - 0.6f, 0), new Vector3(0.048f, 0.06f, 0.048f), 5, 3, p.skin); // hand
            }
            // items in the right hand
            g.bone = ArmR;
            BuildItem(g, p, new Vector3(0.21f, 0.95f + 0.53f - 0.62f, 0));

            // ---------------- head ----------------
            g.bone = Head;
            float hy = 0.95f + 0.6f;
            g.Sphere(new Vector3(0, hy + 0.12f, 0.01f), new Vector3(0.105f, 0.13f, 0.11f), 7, 4, p.skin);
            g.Box(new Vector3(0, hy + 0.11f, 0.115f), new Vector3(0.03f, 0.045f, 0.04f), p.skin); // nose
            g.Box(new Vector3(-0.038f, hy + 0.15f, 0.1f), new Vector3(0.02f, 0.02f, 0.01f), Black, LowPoly.F.Front);
            g.Box(new Vector3(0.038f, hy + 0.15f, 0.1f), new Vector3(0.02f, 0.02f, 0.01f), Black, LowPoly.F.Front);
            // hair
            if (p.hat != "capucha") g.Sphere(new Vector3(0, hy + 0.16f, -0.02f), new Vector3(0.112f, 0.12f, 0.112f), 7, 3, p.hair);
            if (woman) g.Sphere(new Vector3(0, hy + 0.2f, -0.11f), new Vector3(0.06f, 0.06f, 0.05f), 6, 3, p.hair); // bun
            else if (p.outfit == Outfit.Caballero || p.outfit == Outfit.Virrey) g.Box(new Vector3(0, hy + 0.05f, -0.12f), new Vector3(0.04f, 0.12f, 0.03f), p.hair); // queue
            if (p.outfit == Outfit.Gaucho && p.hat != "sombrero") g.Box(new Vector3(0, hy + 0.04f, 0.1f), new Vector3(0.12f, 0.06f, 0.03f), p.hair); // moustache

            Color32 hatCol = p.outfit == Outfit.Patricio ? Black : (p.outfit == Outfit.Levita ? Black : Palette.Hex(0x1f1b18));
            switch (p.hat)
            {
                case "tri":
                    g.Cyl(new Vector3(0, hy + 0.22f, 0), 0.25f, 0.23f, 0.09f, 3, hatCol, true, true, Mathf.PI / 2);
                    g.Cyl(new Vector3(0, hy + 0.24f, 0), 0.12f, 0.11f, 0.1f, 8, hatCol);
                    break;
                case "bi":
                    g.Push(new Vector3(0, hy + 0.27f, 0), 90f);
                    g.Box(Vector3.zero, new Vector3(0.05f, 0.15f, 0.42f), hatCol, LowPoly.F.All);
                    if (p.outfit == Outfit.Patricio) g.Box(new Vector3(0.03f, 0.03f, 0.08f), new Vector3(0.02f, 0.06f, 0.06f), White); // escarapela-like cockade
                    g.Pop();
                    break;
                case "galera":
                    g.Cyl(new Vector3(0, hy + 0.22f, 0), 0.18f, 0.18f, 0.02f, 9, hatCol, true, true);
                    g.Cyl(new Vector3(0, hy + 0.23f, 0), 0.105f, 0.12f, 0.2f, 9, hatCol);
                    break;
                case "gorra":
                    g.Cyl(new Vector3(0, hy + 0.2f, 0), 0.115f, 0.08f, 0.09f, 7, p.accent);
                    break;
                case "sombrero":
                    g.Cyl(new Vector3(0, hy + 0.21f, 0), 0.26f, 0.26f, 0.02f, 9, Palette.Hex(0x3b2a1a), true, true);
                    g.Cyl(new Vector3(0, hy + 0.22f, 0), 0.115f, 0.1f, 0.11f, 9, Palette.Hex(0x3b2a1a));
                    break;
                case "peineta":
                    g.Push(new Vector3(0, hy + 0.26f, -0.07f), 0, -15f);
                    g.Lathe(Vector3.zero, new[] { new Vector2(0f, 0f), new Vector2(0.2f, 0f), new Vector2(0.24f, 0.12f), new Vector2(0.18f, 0.24f), new Vector2(0f, 0.26f) }, 10, Palette.Hex(0x3a2418));
                    g.Pop();
                    break;
                case "mantilla":
                    g.Lathe(new Vector3(0, hy - 0.08f, -0.02f), new[] { new Vector2(0.26f, 0f), new Vector2(0.17f, 0.18f), new Vector2(0.13f, 0.3f), new Vector2(0f, 0.34f) }, 8, Black);
                    break;
                case "panuelo":
                    g.Sphere(new Vector3(0, hy + 0.18f, -0.02f), new Vector3(0.12f, 0.1f, 0.12f), 7, 3, p.accent);
                    break;
                case "capucha":
                    g.Lathe(new Vector3(0, hy - 0.04f, -0.05f), new[] { new Vector2(0.2f, 0f), new Vector2(0.14f, 0.2f), new Vector2(0.12f, 0.26f), new Vector2(0f, 0.3f) }, 7, p.coat);
                    g.Sphere(new Vector3(0, hy + 0.2f, -0.01f), new Vector3(0.1f, 0.03f, 0.1f), 6, 2, p.skin); // tonsure
                    break;
            }
        }

        static void BuildItem(LowPoly g, PersonSpec p, Vector3 hand)
        {
            switch (p.item)
            {
                case "umbrella":
                    g.CylBetween(hand + new Vector3(0, -0.1f, 0.08f), hand + new Vector3(0, 0.95f, 0.08f), 0.012f, 0.012f, 4, Wood);
                    g.Lathe(hand + new Vector3(0, 0.7f, 0.08f), new[] { new Vector2(0.5f, 0f), new Vector2(0.38f, 0.14f), new Vector2(0f, 0.26f) }, 8, Palette.Hex(0x2f2f3a));
                    break;
                case "cane": g.CylBetween(hand + new Vector3(0, 0.05f, 0.06f), hand + new Vector3(0, -0.9f, 0.12f), 0.014f, 0.014f, 4, Black); break;
                case "basket":
                    g.Lathe(hand + new Vector3(0.05f, -0.28f, 0.05f), new[] { new Vector2(0f, 0f), new Vector2(0.15f, 0f), new Vector2(0.2f, 0.18f), new Vector2(0.17f, 0.2f), new Vector2(0f, 0.2f) }, 8, Palette.Hex(0xb08a4a));
                    g.Sphere(hand + new Vector3(0.05f, -0.08f, 0.05f), new Vector3(0.12f, 0.06f, 0.12f), 6, 3, Palette.Hex(0xd8b25a));
                    break;
                case "olla":
                    g.Lathe(hand + new Vector3(0.06f, -0.3f, 0.08f), new[] { new Vector2(0f, 0f), new Vector2(0.15f, 0.02f), new Vector2(0.19f, 0.16f), new Vector2(0.13f, 0.3f), new Vector2(0f, 0.3f) }, 8, Palette.Hex(0x8a4a2a));
                    break;
                case "candles":
                    for (int i = -2; i <= 2; i++) g.CylBetween(hand + new Vector3(i * 0.03f, 0f, 0.06f), hand + new Vector3(i * 0.03f, -0.4f, 0.06f), 0.012f, 0.012f, 4, Palette.Hex(0xf2e8c8), false);
                    break;
                case "tray":
                    g.Box(hand + new Vector3(0, 0.02f, 0.18f), new Vector3(0.42f, 0.04f, 0.32f), Wood, LowPoly.F.All);
                    for (int i = 0; i < 5; i++) g.Sphere(hand + new Vector3(-0.14f + i * 0.07f, 0.06f, 0.18f + (i % 2) * 0.06f), new Vector3(0.035f, 0.025f, 0.05f), 5, 2, Palette.Hex(0xd8a04a));
                    break;
                case "brooms":
                    for (int i = 0; i < 3; i++) { g.CylBetween(hand + new Vector3(i * 0.05f, -0.2f, 0.05f), hand + new Vector3(i * 0.05f, 1.0f, 0.05f), 0.012f, 0.012f, 4, Wood, false); g.Cyl(hand + new Vector3(i * 0.05f, 1.0f, 0.05f), 0.06f, 0.1f, 0.25f, 6, Palette.Hex(0xc9b06a)); }
                    break;
                case "fish":
                    g.CylBetween(hand, hand + new Vector3(0, -0.3f, 0), 0.008f, 0.008f, 3, Palette.Hex(0x8a7a5a), false);
                    for (int i = 0; i < 3; i++) g.Sphere(hand + new Vector3(-0.05f + i * 0.05f, -0.42f, 0.03f), new Vector3(0.03f, 0.13f, 0.06f), 5, 3, Palette.Hex(0x9fb0b0));
                    break;
                case "musket":
                    g.CylBetween(hand + new Vector3(0, -0.15f, 0.05f), hand + new Vector3(0, 1.3f, 0.05f), 0.018f, 0.014f, 5, Iron, false);
                    g.Box(hand + new Vector3(0, -0.2f, 0.05f), new Vector3(0.06f, 0.42f, 0.09f), Wood);
                    break;
                case "book": g.Box(hand + new Vector3(0, 0.02f, 0.09f), new Vector3(0.05f, 0.2f, 0.15f), Palette.Hex(0x3a2418), LowPoly.F.All); break;
                case "mate":
                    g.Sphere(hand + new Vector3(0, 0.03f, 0.08f), new Vector3(0.045f, 0.05f, 0.045f), 6, 3, Palette.Hex(0x7a5a2a));
                    g.CylBetween(hand + new Vector3(0, 0.05f, 0.08f), hand + new Vector3(0.02f, 0.16f, 0.06f), 0.005f, 0.005f, 3, Palette.Hex(0xc0c0c0), false);
                    break;
            }
        }
    }

    // Procedural animation: walking, idling, talking gestures. Cheap enough for dozens of people;
    // the crowd manager turns it off for far-away people.
    public class PersonRig : MonoBehaviour
    {
        public Transform[] bones;
        public bool woman, holdsItem;
        [System.NonSerialized] public float speed;       // m/s, 0 = standing
        [System.NonSerialized] public bool talking;      // gesticulate
        [System.NonSerialized] public bool seated;
        [System.NonSerialized] public float gestureBoost;
        float phase, blend, gestT, gestTarget, nod;
        Quaternion[] rest;

        void Awake()
        {
            phase = Random.value * 10f; gestT = Random.value * 3f;
            rest = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++) rest[i] = bones[i].localRotation;
        }

        public void Tick(float dt)
        {
            blend = Mathf.MoveTowards(blend, speed > 0.05f ? 1f : 0f, dt * 4f);
            float stride = Mathf.Clamp(speed, 0f, 3.5f);
            phase += dt * (2.2f + stride * 1.6f) * (speed > 0.05f ? 1f : 0.3f);
            float s = Mathf.Sin(phase * Mathf.PI);
            float legAmp = (woman ? 18f : 28f) * Mathf.Clamp01(stride / 1.4f) * blend;
            if (seated)
            {
                // a single leg segment: point it forward and down so the feet reach the floor
                bones[PersonFactory.LegL].localRotation = Quaternion.Euler(-58f, 0, 0);
                bones[PersonFactory.LegR].localRotation = Quaternion.Euler(-58f, 0, 0);
                bones[PersonFactory.Hips].localPosition = new Vector3(0, 0.52f, -0.05f);
            }
            else
            {
                bones[PersonFactory.LegL].localRotation = Quaternion.Euler(s * legAmp, 0, 0);
                bones[PersonFactory.LegR].localRotation = Quaternion.Euler(-s * legAmp, 0, 0);
                bones[PersonFactory.Hips].localPosition = new Vector3(0, 0.95f + Mathf.Abs(Mathf.Cos(phase * Mathf.PI)) * 0.03f * blend, 0);
            }

            // arms: counter-swing while walking, gestures while talking, breathing otherwise
            float armAmp = 22f * Mathf.Clamp01(stride / 1.4f) * blend;
            float breathe = Mathf.Sin(Time.time * 1.7f + phase) * 2f;
            float gestureL = 0f, gestureR = 0f, gestureSide = 0f;
            if (talking || gestureBoost > 0f)
            {
                gestT -= dt;
                if (gestT <= 0f) { gestTarget = Random.value < 0.55f ? Random.Range(25f, 70f) : 0f; gestT = Random.Range(0.6f, 1.8f); }
                nod = Mathf.Lerp(nod, gestTarget, dt * 3f);
                gestureR = nod; gestureL = nod * 0.35f; gestureSide = nod * 0.2f;
                gestureBoost = Mathf.Max(0f, gestureBoost - dt);
            }
            else nod = Mathf.Lerp(nod, 0f, dt * 3f);

            float rightSwing = holdsItem ? 6f : -s * armAmp;
            bones[PersonFactory.ArmL].localRotation = Quaternion.Euler(s * armAmp - gestureL + breathe, 0, 4f + gestureSide);
            bones[PersonFactory.ArmR].localRotation = Quaternion.Euler(rightSwing - gestureR - breathe, 0, -4f - gestureSide);
            bones[PersonFactory.Torso].localRotation = Quaternion.Euler(1.5f * blend, Mathf.Sin(phase * Mathf.PI) * 4f * blend, 0);
            bones[PersonFactory.Head].localRotation = Quaternion.Euler(talking ? Mathf.Sin(Time.time * 2.3f + phase) * 6f : 0f, talking ? Mathf.Sin(Time.time * 0.9f + phase) * 12f : 0f, 0);
        }
    }
}
