using UnityEngine;
using O = Colonial.LowPoly.Opening;
using F = Colonial.LowPoly.F;

namespace Colonial
{
    // The explorable landmarks. Each is modeled in its own local frame: facade on z = 0, street at +z,
    // interior toward -z, x along the facade. Interiors are real geometry inside the shell, so
    // entering only needs the door animation (no scene loading).
    public static partial class Landmarks
    {
        static Color32 Wash => CityBuilder.Whitewash; static Color32 Ochre => CityBuilder.Ochre; static Color32 Tile => CityBuilder.Tile;
        static Color32 Wood => CityBuilder.Wood; static Color32 WoodDark => CityBuilder.WoodDark; static Color32 Iron => CityBuilder.Iron; static Color32 Stone => CityBuilder.Stone;

        // walls in the local frame
        public static void WallX(LowPoly g, float x0, float x1, float z, float y0, float y1, float t, Color32 c, params O[] o) { g.Push(new Vector3(0, 0, z)); g.Wall(x0, x1, y0, y1, t, c, o); g.Pop(); }
        public static void WallZ(LowPoly g, float z0, float z1, float x, float y0, float y1, float t, Color32 c, params O[] o) { g.Push(new Vector3(x, 0, 0), -90f); g.Wall(z0, z1, y0, y1, t, c, o); g.Pop(); }
        static O A(float a, float b, float y0, float y1) => new O(a, b, y0, y1, true);
        static O R(float a, float b, float y0, float y1) => new O(a, b, y0, y1, false);

        static void Belfry(LowPoly g, Vector3 c, float half, float y0, Color32 wall, float cupola)
        {
            // open bell chamber with 4 arches, cornice and a cupola with a cross
            WallX(g, c.x - half, c.x + half, c.z + half - 0.3f, y0, y0 + 4.2f, 0.6f, wall, A(c.x - 1.2f, c.x + 1.2f, y0 + 0.6f, y0 + 3.6f));
            WallX(g, c.x - half, c.x + half, c.z - half + 0.3f, y0, y0 + 4.2f, 0.6f, wall, A(c.x - 1.2f, c.x + 1.2f, y0 + 0.6f, y0 + 3.6f));
            WallZ(g, c.z - half + 0.6f, c.z + half - 0.6f, c.x - half + 0.3f, y0, y0 + 4.2f, 0.6f, wall, A(c.z - 1.2f, c.z + 1.2f, y0 + 0.6f, y0 + 3.6f));
            WallZ(g, c.z - half + 0.6f, c.z + half - 0.6f, c.x + half - 0.3f, y0, y0 + 4.2f, 0.6f, wall, A(c.z - 1.2f, c.z + 1.2f, y0 + 0.6f, y0 + 3.6f));
            g.Lathe(new Vector3(c.x, y0 + 1.6f, c.z), new[] { new Vector2(0f, 0f), new Vector2(0.75f, 0f), new Vector2(0.55f, 0.6f), new Vector2(0.45f, 1.3f), new Vector2(0f, 1.45f) }, 8, Palette.Hex(0x8a6a2a));
            g.BoxMM(new Vector3(c.x - half - 0.4f, y0 + 4.2f, c.z - half - 0.4f), new Vector3(c.x + half + 0.4f, y0 + 4.7f, c.z + half + 0.4f), wall, F.All);
            g.Lathe(new Vector3(c.x, y0 + 4.7f, c.z), new[] { new Vector2(half, 0f), new Vector2(half * 0.92f, cupola * 0.45f), new Vector2(half * 0.6f, cupola * 0.85f), new Vector2(0f, cupola) }, 8, Tile, Mathf.PI / 8);
            Props.Cross(g, new Vector3(c.x, y0 + 4.7f + cupola, c.z), 1.4f, Iron);
        }

        // ======================================================================== CABILDO
        public static Site Cabildo(Material mat, Transform parent)
        {
            var place = City.Data.places.cabildo;
            var s = new Site("cabildo", "Cabildo", City.W2U(place.foot.x + place.foot.w, place.foot.z + place.foot.d / 2), Vector3.right, parent);
            s.mapRect = new Rect(486, 356, 74, 84);
            var g = s.solid; var d = s.deco;
            const float G = 5.4f, U = 10.6f;
            var r = new Rng(25);

            // ---- arcaded front (recova) of 11 arches on both floors ----
            var arches = new O[11]; var archesUp = new O[11];
            for (int k = 0; k < 11; k++)
            {
                float x0 = -21.5f + 4f * k;
                arches[k] = A(x0, x0 + 3f, 0f, 4.4f);
                archesUp[k] = A(x0, x0 + 3f, k == 5 ? G : G + 1.0f, G + 4.0f);
            }
            WallX(g, -22.5f, 22.5f, -0.4f, 0f, G, 0.8f, Wash, arches);
            WallX(g, -22.5f, 22.5f, -0.4f, G, U, 0.8f, Wash, archesUp);
            // side wings (closed) with grilles and balconies on the facade
            g.BoxMM(new Vector3(-42, 0, -22), new Vector3(-22.5f, U, 0), Wash);
            g.BoxMM(new Vector3(22.5f, 0, -22), new Vector3(42, U, 0), Wash);
            foreach (var x in new[] { -37f, -31.5f, -26.5f, 26.5f, 31.5f, 37f }) { CityBuilder.Reja(d, x, 1.0f, 1.35f, 2.0f); CityBuilder.Balcony(d, x, G + 0.5f); }
            // string course, cornice and pilasters
            g.BoxMM(new Vector3(-42.2f, G - 0.25f, 0), new Vector3(42.2f, G + 0.15f, 0.35f), Ochre, F.All & ~F.Back);
            g.BoxMM(new Vector3(-42.4f, U - 0.35f, 0), new Vector3(42.4f, U + 0.1f, 0.5f), Ochre, F.All & ~F.Back);
            for (int k = 0; k <= 11; k++) { float x = -22f + 4f * k; d.BoxMM(new Vector3(x - 0.5f, 0, 0), new Vector3(x + 0.5f, U - 0.35f, 0.18f), Palette.Hex(0xf7f2e8), F.Front | F.Left | F.Right); }
            // floors and ceilings
            Props.Floor(g, -22.5f, 22.5f, -4.9f, 0.05f, 0.05f, Props.BrickFloor);
            g.BoxMM(new Vector3(-22.5f, G - 0.35f, -22f), new Vector3(22.5f, G, 0), Palette.Hex(0xe7dfcf), F.Top | F.Bottom | F.Front);
            Props.Ceiling(d, -22.5f, 22.5f, -4.5f, 0f, G - 0.36f, Palette.Hex(0xe7dfcf), WoodDark, false);
            // balcony over the plaza
            g.BoxMM(new Vector3(-3f, G - 0.3f, 0f), new Vector3(3f, G, 1.8f), Stone, F.All);
            for (int i = 0; i <= 12; i++) { float x = -2.9f + i * 5.8f / 12f; g.BoxMM(new Vector3(x - 0.025f, G, 1.7f), new Vector3(x + 0.025f, G + 1.0f, 1.75f), Iron, F.Front | F.Back | F.Left | F.Right); }
            g.BoxMM(new Vector3(-3f, G + 1.0f, 1.68f), new Vector3(3f, G + 1.08f, 1.78f), Iron, F.All);
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                g.BoxMM(new Vector3(sgn * 2.95f - 0.04f, G + 1.0f, 0f), new Vector3(sgn * 2.95f + 0.04f, G + 1.08f, 1.78f), Iron, F.All);
                for (int i = 0; i <= 6; i++) g.BoxMM(new Vector3(sgn * 2.95f - 0.025f, G, i * 0.28f), new Vector3(sgn * 2.95f + 0.025f, G + 1.0f, i * 0.28f + 0.05f), Iron, F.Left | F.Right);
            }
            // roof, tower and clock
            g.BoxMM(new Vector3(-42f, U - 0.3f, -22f), new Vector3(42f, U, 0f), Palette.Hex(0xe7dfcf), F.Bottom);
            g.HipRoof(new Vector3(0, U, -11f), 85.2f, 23.4f, 3.2f, Tile);
            g.BoxMM(new Vector3(-3.5f, U, -12f), new Vector3(3.5f, U + 6f, -5f), Wash, F.All & ~F.Bottom);
            g.BoxMM(new Vector3(-3.8f, U + 5.8f, -12.3f), new Vector3(3.8f, U + 6.2f, -4.7f), Ochre, F.All);
            Belfry(g, new Vector3(0, 0, -8.5f), 3.5f, U + 6.2f, Wash, 3.4f);
            d.Push(new Vector3(0, U + 3.2f, -4.95f), 0, 90f); d.Cyl(Vector3.zero, 1.15f, 1.15f, 0.08f, 12, Palette.Hex(0xf4efe3), true, false); d.Cyl(Vector3.zero, 1.25f, 1.25f, 0.05f, 12, Ochre, false); d.Pop();
            d.Box(new Vector3(0, U + 3.55f, -4.85f), new Vector3(0.08f, 0.75f, 0.04f), Iron, F.Front); d.Box(new Vector3(0.25f, U + 3.2f, -4.84f), new Vector3(0.55f, 0.07f, 0.04f), Iron, F.Front);

            // ---- ground floor: gallery back wall with the main door, zaguán, notary office, jail ----
            WallX(g, -22.5f, 22.5f, -4.5f, 0f, G, 0.8f, Wash, R(-1.6f, 1.6f, 0f, 3.9f));
            d.Push(new Vector3(0, 0, -4.1f));
            foreach (var x in new[] { -15f, -8f, 8f, 15f }) CityBuilder.Reja(d, x, 1.1f, 1.3f, 1.9f);
            d.BoxMM(new Vector3(-2.3f, 0, 0), new Vector3(2.3f, 4.6f, 0.15f), Ochre, F.Front | F.Top | F.Left | F.Right);
            d.Pop();
            var main = s.AddEntrance("Cabildo de Buenos Aires", "Planta baja · zaguán, escribanía y cárcel", new Vector3(0, 0, -1.6f), new Vector3(0, 0, -9f));
            s.AddLeaf(main, new Vector3(-1.6f, 0, -4.5f), 1.6f, 3.9f, 0.16f, false, WoodDark);
            s.AddLeaf(main, new Vector3(1.6f, 0, -4.5f), 1.6f, 3.9f, 0.16f, true, WoodDark);

            WallZ(g, -21.6f, -4.9f, -2f, 0f, G - 0.35f, 0.5f, Wash, R(-12.5f, -10.9f, 0f, 2.8f));
            WallZ(g, -21.6f, -4.9f, 2f, 0f, G - 0.35f, 0.5f, Wash, R(-12.5f, -10.9f, 0f, 2.8f));
            WallX(g, -22.5f, 22.5f, -22f, 0f, G, 0.8f, Wash, A(-2f, 2f, 0f, 4.2f), R(-15f, -13.4f, 1.2f, 2.9f), R(13.4f, 15f, 1.2f, 2.9f));
            Props.Floor(g, -22.5f, 22.5f, -21.6f, -4.9f, 0.05f, Props.FloorTile);
            Props.Ceiling(d, -22.5f, 22.5f, -21.6f, -4.9f, G - 0.36f, Palette.Hex(0xe7dfcf), WoodDark);
            // escribanía (left)
            Props.Table(d, new Vector3(-12f, 0.05f, -9f), 0, 2.4f, 1.1f, Props.Cloth, 0.8f, true);
            Props.Papers(d, new Vector3(-12f, 0.86f, -9f), r);
            Props.Candelabra(d, new Vector3(-11f, 0.86f, -8.8f));
            Props.Chair(d, new Vector3(-12f, 0.05f, -10.1f), 0, Props.Velvet);
            foreach (var z in new[] { -7f, -11f, -15f, -19f }) Props.Shelf(d, new Vector3(-21.9f, 0.05f, z), 90, 3.2f, 3.2f, 5, (l, k) => (k + l) % 6 == 0 ? new Color32(0, 0, 0, 0) : (k % 3 == 0 ? Palette.Hex(0x6b2a1a) : k % 3 == 1 ? Palette.Hex(0x3a2418) : Props.Paper));
            Props.Table(d, new Vector3(-8f, 0.05f, -17f), 90, 2f, 1f, Wood);
            Props.Papers(d, new Vector3(-8f, 0.84f, -17f), r);
            Props.Painting(d, new Vector3(-12f, 3.1f, -21.55f), 0, 1.4f, 1.8f, Props.Canvas, Palette.Hex(0x7a1f22));
            s.Light(new Vector3(-11f, 3.4f, -11f), new Color(1f, 0.82f, 0.6f), 11f, 1.6f, true);
            // cárcel (right): iron bars
            for (float z = -21.4f; z <= -5.2f; z += 0.32f) d.BoxMM(new Vector3(13.97f, 0.05f, z - 0.03f), new Vector3(14.03f, G - 0.4f, z + 0.03f), Iron, F.All & ~F.Bottom);
            s.blockers.Add((new Vector3(14f, 2.5f, -13.2f), new Vector3(0.2f, 5f, 16.6f)));
            d.BoxMM(new Vector3(13.9f, G - 0.6f, -21.6f), new Vector3(14.1f, G - 0.4f, -4.9f), Iron, F.All);
            Props.Bench(d, new Vector3(20.5f, 0.05f, -13f), -90, 3f, false);
            d.BoxMM(new Vector3(19.2f, 0.05f, -20f), new Vector3(21.6f, 0.35f, -17f), Palette.Hex(0xc9b38a), F.All & ~F.Bottom);
            Props.Pot(d, new Vector3(16f, 0.05f, -6f), 0.8f, false);
            Props.Bench(d, new Vector3(8f, 0.05f, -19f), 0, 2.5f);
            s.Light(new Vector3(9f, 3.4f, -13f), new Color(1f, 0.8f, 0.55f), 10f, 1.1f, true);

            // ---- patio, stairs ----
            WallZ(g, -70f, -22f, -24f, 0f, 6.5f, 0.8f, Wash);
            WallZ(g, -70f, -22f, 24f, 0f, 6.5f, 0.8f, Wash);
            WallX(g, -24f, 24f, -70f, 0f, 6.5f, 0.8f, Wash, R(-1.4f, 1.4f, 0f, 3.2f));
            d.BoxMM(new Vector3(-1.4f, 0f, -70.2f), new Vector3(1.4f, 3.2f, -69.8f), WoodDark, F.Front | F.Back);
            Props.Floor(g, -23.6f, 23.6f, -69.6f, -22.4f, 0.04f, Props.BrickFloor);
            Props.Aljibe(d, new Vector3(0, 0.04f, -46f));
            for (int i = 0; i < 4; i++) { Props.Pot(d, new Vector3(-8f + i * 5.3f, 0.04f, -34f), 1.1f); Props.Pot(d, new Vector3(-8f + i * 5.3f, 0.04f, -58f), 1.1f); }
            CityBuilder.Tree(d, new Vector3(-14f, 0, -54f), 1.1f, new Rng(3));
            CityBuilder.Tree(d, new Vector3(14f, 0, -40f), 1.0f, new Rng(9));
            Props.Bench(d, new Vector3(12f, 0.04f, -62f), 0, 2.6f);
            Props.Bench(d, new Vector3(-12f, 0.04f, -30f), 180, 2.6f);
            // patio colonnades along both sides
            foreach (var sx in new[] { -1f, 1f })
            {
                for (float z = -26f; z >= -66f; z -= 5f) g.Cyl(new Vector3(sx * 19.5f, 0.04f, z), 0.28f, 0.24f, 3.6f, 8, Wash, false);
                g.BoxMM(new Vector3(sx > 0 ? 19.2f : -23.6f, 3.6f, -67f), new Vector3(sx > 0 ? 23.6f : -19.2f, 3.9f, -24f), Wash, F.All);
                d.BoxMM(new Vector3(sx > 0 ? 19f : -23.8f, 3.9f, -67.2f), new Vector3(sx > 0 ? 23.8f : -19f, 4.1f, -23.8f), Tile, F.Top);
            }
            g.Stairs(new Vector3(-17.8f, 0.04f, -31.6f), 2.8f, G - 0.04f, 9.6f, 30, Stone, Palette.Hex(0xa8a091));
            g.BoxMM(new Vector3(-19.4f, 0f, -31.6f), new Vector3(-19.2f, G + 1f, -22f), Wash, F.Left | F.Right | F.Top);   // stair wall
            g.BoxMM(new Vector3(-16.4f, 0f, -31.6f), new Vector3(-16.2f, 1.0f, -22f), Wash, F.Left | F.Right | F.Top);     // low parapet start
            for (int i = 0; i < 8; i++) { float z = -31.6f + i * 1.2f, y = (i + 1) * G / 8f; g.BoxMM(new Vector3(-16.4f, y - 0.3f, z), new Vector3(-16.2f, y + 0.9f, z + 1.2f), Wash, F.Left | F.Right | F.Top); }
            s.Light(new Vector3(0, 4f, -46f), new Color(1f, 0.9f, 0.75f), 18f, 0.6f);

            // ---- upper floor: open gallery, Sala Capitular, loggia to the plaza ----
            WallX(g, -22.5f, 22.5f, -4.5f, G, U, 0.6f, Wash, R(-1.1f, 1.1f, G, G + 3.0f), R(-9.2f, -7.6f, G, G + 3.0f), R(7.6f, 9.2f, G, G + 3.0f));
            WallX(g, -22.5f, 22.5f, -18f, G, U, 0.6f, Wash, R(-1.2f, 1.2f, G, G + 3.2f));
            WallZ(g, -17.7f, -4.8f, -16f, G, U, 0.5f, Wash);
            WallZ(g, -17.7f, -4.8f, 16f, G, U, 0.5f, Wash);
            Props.Checker(d, -15.75f, 15.75f, -17.7f, -4.8f, G + 0.01f, 1.4f, Props.FloorTile, Props.FloorTile2);
            Props.Ceiling(d, -22.5f, 22.5f, -22f, 0f, U - 0.31f, Palette.Hex(0xe7dfcf), WoodDark);
            // gallery parapet toward the patio (gap where the stairs arrive) and wooden posts
            g.BoxMM(new Vector3(-16.3f, G, -22.2f), new Vector3(22.5f, G + 1.0f, -21.9f), Wash, F.All & ~F.Bottom);
            for (float x = -15f; x <= 22f; x += 4.4f) g.Cyl(new Vector3(x, G + 1f, -22.05f), 0.16f, 0.14f, U - G - 1f, 6, Wood, false);
            g.BoxMM(new Vector3(-24f, U - 0.6f, -22.4f), new Vector3(24f, U - 0.3f, -21.7f), Wood, F.All);
            // Sala Capitular
            Props.Rugs(d, new Vector3(0, G, -11.2f), 16f, 7f, Props.Rug, Props.Rug2);
            Props.Table(d, new Vector3(0, G, -11.2f), 0, 11f, 2.2f, Props.Cloth, 0.8f, true);
            for (int i = 0; i < 6; i++) { float x = -4.6f + i * 1.85f; Props.Chair(d, new Vector3(x, G, -12.8f), 0, Props.Velvet); Props.Chair(d, new Vector3(x, G, -9.6f), 180, Props.Velvet); }
            Props.Chair(d, new Vector3(-6.4f, G, -11.2f), 90, Props.Velvet);
            Props.Papers(d, new Vector3(-2f, G + 0.82f, -11.2f), r); Props.Papers(d, new Vector3(3f, G + 0.82f, -11f), r);
            foreach (var x in new[] { -4f, 0f, 4f }) Props.Candelabra(d, new Vector3(x, G + 0.82f, -11.2f));
            Props.Painting(d, new Vector3(15.68f, G + 2.6f, -11.2f), -90, 1.8f, 2.4f, Props.Canvas, Palette.Hex(0x7a1f22));   // Fernando VII
            Props.Cross(d, new Vector3(-15.6f, G + 1.6f, -11.2f), 1.2f, Props.Gold);
            d.BoxMM(new Vector3(-15.7f, G, -17.4f), new Vector3(-13f, G + 1.0f, -16.6f), WoodDark, F.All & ~F.Bottom); // chest of the archive
            Props.Shelf(d, new Vector3(13f, G, -17.6f), 0, 4f, 2.8f, 4, (l, k) => k % 4 == 3 ? new Color32(0, 0, 0, 0) : Palette.Hex(k % 2 == 0 ? 0x5a2b1a : 0x3a2418));
            s.Light(new Vector3(0, G + 3.2f, -11f), new Color(1f, 0.84f, 0.62f), 14f, 1.8f, true);
            s.Light(new Vector3(0, G + 2.8f, -2.2f), new Color(1f, 0.9f, 0.75f), 12f, 0.7f);

            // ---- people ----
            string[] acta = {
                "Señores, el Cabildo Abierto del 22 votó que el virrey cese en el mando.",
                "Y ayer quisimos una Junta presidida por el mismo Cisneros… el pueblo no la aceptó.",
                "Los comandantes dicen que no pueden sostenerla. Saavedra fue muy claro.",
                "Entonces no queda otra: hay que aceptar la lista que trae el pueblo.",
                "Saavedra presidente; Moreno y Paso, secretarios…",
                "…y de vocales Belgrano, Castelli, Azcuénaga, Alberti, Matheu y Larrea.",
                "Que conste en el acta: veinticinco de mayo de mil ochocientos diez." };
            s.Actor(new Vector3(-6.4f, G, -11.2f), 90, Outfit.Caballero, "Alcalde Lezica", 1, acta, true);
            s.Actor(new Vector3(-2.75f, G, -12.8f), 0, Outfit.Levita, "Síndico Leiva", 1, null, true);
            s.Actor(new Vector3(0.95f, G, -9.6f), 180, Outfit.Caballero, "Regidor del Cabildo", 1, null, true);
            s.Actor(new Vector3(2.8f, G, -12.8f), 0, Outfit.Caballero, "Regidor del Cabildo", 1, null, true);
            s.Actor(new Vector3(-0.9f, G, -9.6f), 180, Outfit.Levita, "Regidor del Cabildo", 1, null, true);
            s.Actor(new Vector3(4.65f, G, -9.6f), 180, Outfit.Caballero, "Regidor del Cabildo", 1, null, true);
            s.Actor(new Vector3(-12f, 0.05f, -10.1f), 0, Outfit.Levita, "Escribano", 2, new[] { "Copio el acta del Cabildo Abierto… ¡doscientos votos!", "Pluma, tinta y paciencia: la historia no se escribe sola.", "Cuidado con la vela, que estos papeles valen oro." }, true, "none", "none");
            s.Actor(new Vector3(-8f, 0.05f, -15.6f), 180, Outfit.Peon, "Amanuense", 2);
            s.Actor(new Vector3(18.5f, 0.05f, -13f), -90, Outfit.Gaucho, "Preso", 3, new[] { "¡Yo sólo grité «viva la Junta» antes de tiempo!", "Oiga, ¿no tendrá un pedazo de pan?", "Cuando haya gobierno nuevo me sueltan, ya va a ver." }, true);
            s.Actor(new Vector3(12.6f, 0.05f, -9f), 90, Outfit.Patricio, "Guardia", 3, new[] { "Silencio ahí adentro.", "Nadie entra a la cárcel sin orden del alcalde." });
            s.Actor(new Vector3(-6f, 0.04f, -44f), 60, Outfit.Caballero, "Vecino", 4, new[] { "¿Usted votó el 22, don Martín?", "Voté que cese el virrey, como la mayoría.", "Dicen que el obispo Lué se opuso con todas sus fuerzas.", "Y Castelli le contestó que el poder volvía al pueblo." });
            s.Actor(new Vector3(-4.2f, 0.04f, -45.2f), -110, Outfit.Levita, "Don Martín", 4);
            s.Actor(new Vector3(-2.6f, 0.05f, -2.2f), 180, Outfit.Patricio, "Centinela", 5, new[] { "Paso, vecino. El Cabildo está en sesión.", "Por orden del comandante Saavedra, aquí no se arma alboroto." });
            s.Actor(new Vector3(2.6f, 0.05f, -2.2f), 180, Outfit.Patricio, "Centinela", 5);

            s.Interior(new Vector3(-22.5f, -1f, -21.6f), new Vector3(22.5f, G, -4.5f));
            s.Interior(new Vector3(-24f, -1f, -70f), new Vector3(24f, 12f, -21.6f));
            s.Interior(new Vector3(-22.5f, G - 0.2f, -22f), new Vector3(22.5f, U, 2f));
            s.Finish(mat, "Cabildo");
            return s;
        }

        // ======================================================================== CATEDRAL
        public static Site Catedral(Material mat, Transform parent)
        {
            var place = City.Data.places.catedral;
            var s = new Site("catedral", "Catedral", City.W2U(place.foot.x + place.foot.w / 2, place.foot.z + place.foot.d), Vector3.back, parent);
            s.mapRect = new Rect(place.foot.x - 2, place.foot.z - 4, place.foot.w + 4, place.foot.d + 4);
            var g = s.solid; var d = s.deco; var r = new Rng(1791);
            var raw = Palette.Hex(0xc99a74);   // unfinished brick facade
            const float AISLE = 10f, NAVE = 14f;

            // outer walls with high arched windows
            var wins = new O[7];
            for (int k = 0; k < 7; k++) { float zc = -6f - 7f * k; wins[k] = A(zc - 1f, zc + 1f, 6f, 8.6f); }
            WallZ(g, -56f, -1.4f, -18.6f, 0f, AISLE, 1.4f, Wash, wins);
            WallZ(g, -56f, -1.4f, 18.6f, 0f, AISLE, 1.4f, Wash, wins);
            WallX(g, -19.3f, -7f, -56.7f, 0f, AISLE, 1.4f, Wash);
            WallX(g, 7f, 19.3f, -56.7f, 0f, AISLE, 1.4f, Wash);
            // facade (unfinished in 1810: bare brick above a plastered base)
            WallX(g, -20f, 20f, -0.7f, 0f, 10.5f, 1.4f, raw, A(-2.2f, 2.2f, 0f, 6.5f), A(-12.4f, -10f, 0f, 4.4f), A(10f, 12.4f, 0f, 4.4f));
            d.BoxMM(new Vector3(-20.1f, 0f, -0.05f), new Vector3(20.1f, 0.9f, 0.1f), Stone, F.Front | F.Top);
            WallX(g, -7.7f, 7.7f, -0.7f, 10.5f, 18f, 1.4f, raw, A(-1.5f, 1.5f, 12.5f, 15.5f));
            g.Push(new Vector3(0, 18f, -0.7f), 90f); g.GableRoof(Vector3.zero, 1.6f, 15.8f, 4f, raw, raw); g.Pop();
            g.BoxMM(new Vector3(-20.3f, 10.3f, -1.5f), new Vector3(20.3f, 10.7f, 0.2f), Ochre, F.All);
            foreach (var x in new[] { -16f, -7.7f, 7.7f, 16f }) d.BoxMM(new Vector3(x - 0.7f, 0, 0), new Vector3(x + 0.7f, x == -16f || x == 16f ? 10.3f : 18f, 0.3f), Wash, F.Front | F.Left | F.Right | F.Top);
            foreach (var x in new[] { -11.2f, 11.2f }) d.BoxMM(new Vector3(x - 1.2f, 0f, -0.05f), new Vector3(x + 1.2f, 3.2f, 0.05f), WoodDark, F.Front);
            // scaffolding on the right half: the works were still going on
            for (float x = 8.5f; x <= 19f; x += 3.5f) for (int k = 0; k < 2; k++) d.CylBetween(new Vector3(x, 0, 0.6f + k * 1.4f), new Vector3(x, 16f, 0.6f + k * 1.4f), 0.09f, 0.07f, 4, Wood, false);
            foreach (var y in new[] { 4.2f, 8.4f, 12.6f }) { d.BoxMM(new Vector3(8.3f, y, 0.4f), new Vector3(19.2f, y + 0.08f, 2.2f), WoodLightC, F.All); d.BoxMM(new Vector3(8.3f, y + 1f, 2.1f), new Vector3(19.2f, y + 1.08f, 2.18f), Wood, F.All); }
            // bell tower on the left
            g.BoxMM(new Vector3(-20f, 0, -6f), new Vector3(-14f, 20f, 0.2f), raw, F.All & ~F.Bottom);
            Belfry(g, new Vector3(-17f, 0, -2.9f), 3f, 20f, Wash, 3f);

            // nave arcades and clerestory
            var bays = new O[6]; var clere = new O[6];
            for (int k = 0; k < 6; k++) { float zc = -5f - 6.8f * k; bays[k] = A(zc - 2.6f, zc + 2.6f, 0f, 8.8f); clere[k] = A(zc - 0.9f, zc + 0.9f, 10.5f, 12.8f); }
            WallZ(g, -43f, -1.4f, -7f, 0f, NAVE, 1.6f, Wash, Concat(bays, clere));
            WallZ(g, -43f, -1.4f, 7f, 0f, NAVE, 1.6f, Wash, Concat(bays, clere));
            // aisle ceilings and roofs
            foreach (var sx in new[] { -1f, 1f })
            {
                float a = sx * 7.8f, b = sx * 17.9f;
                g.BoxMM(new Vector3(Mathf.Min(a, b), AISLE - 0.3f, -56f), new Vector3(Mathf.Max(a, b), AISLE, -1.4f), Palette.Hex(0xece6da), F.Bottom);
            }
            g.Quad(new Vector3(7.8f, 13.6f, -56f), new Vector3(7.8f, 13.6f, 0f), new Vector3(19.3f, 10f, 0f), new Vector3(19.3f, 10f, -56f), Tile);
            g.Quad(new Vector3(-19.3f, 10f, -56f), new Vector3(-19.3f, 10f, 0f), new Vector3(-7.8f, 13.6f, 0f), new Vector3(-7.8f, 13.6f, -56f), Tile);
            // nave vault (inside) and gable roof (outside)
            const int VS = 8;
            for (int i = 0; i < VS; i++)
            {
                float a0 = Mathf.PI * i / VS, a1 = Mathf.PI * (i + 1) / VS;
                Vector3 P(float a, float z) => new Vector3(Mathf.Cos(a) * 6.2f, NAVE + Mathf.Sin(a) * 6.2f, z);
                d.Quad(P(a0, -1.4f), P(a1, -1.4f), P(a1, -43f), P(a0, -43f), i % 2 == 0 ? Palette.Hex(0xf2ede2) : Palette.Hex(0xe9e2d4));
            }
            g.Push(new Vector3(0, NAVE, -22.2f), 90f); g.GableRoof(Vector3.zero, 41.8f, 16.6f, 8.5f, Tile, raw); g.Pop();
            // crossing: drum, dome, lantern
            g.BoxMM(new Vector3(-18.6f, NAVE - 0.3f, -56f), new Vector3(-7.6f, NAVE, -43f), Tile, F.Top);
            g.BoxMM(new Vector3(7.6f, NAVE - 0.3f, -56f), new Vector3(18.6f, NAVE, -43f), Tile, F.Top);
            WallZ(g, -56f, -43f, -7f, AISLE, NAVE, 1.4f, Wash);
            WallZ(g, -56f, -43f, 7f, AISLE, NAVE, 1.4f, Wash);
            WallX(g, -7f, 7f, -43f, NAVE - 0.1f, NAVE + 0.4f, 1f, Wash);
            g.Cyl(new Vector3(0, NAVE, -49.5f), 7.4f, 7.4f, 5f, 8, Wash, false, false, Mathf.PI / 8);
            g.Lathe(new Vector3(0, NAVE + 5f, -49.5f), new[] { new Vector2(7.6f, 0f), new Vector2(7.6f, 0.4f), new Vector2(7f, 2.8f), new Vector2(5.3f, 5.1f), new Vector2(2.8f, 6.5f), new Vector2(1f, 6.9f), new Vector2(0f, 6.9f) }, 8, Palette.Hex(0x6d8fa6), Mathf.PI / 8);
            g.Cyl(new Vector3(0, NAVE + 11.9f, -49.5f), 1f, 1f, 2.2f, 8, Wash);
            g.Cyl(new Vector3(0, NAVE + 14.1f, -49.5f), 1.2f, 0.1f, 1.4f, 8, Palette.Hex(0x6d8fa6));
            Props.Cross(g, new Vector3(0, NAVE + 15.4f, -49.5f), 1.6f, Iron);
            d.Lathe(new Vector3(0, NAVE + 5f, -49.5f), new[] { new Vector2(0f, 6.4f), new Vector2(2.6f, 6f), new Vector2(5f, 4.6f), new Vector2(6.6f, 2.4f), new Vector2(7.05f, 0f), new Vector2(7.05f, -5f) }, 8, Palette.Hex(0xf0ead9), Mathf.PI / 8);
            for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4; d.Box(new Vector3(Mathf.Cos(a) * 6.9f, NAVE + 2.6f, -49.5f + Mathf.Sin(a) * 6.9f), new Vector3(1.2f, 1.8f, 1.2f), Palette.Hex(0xfff2c8), F.All); }
            // apse
            WallZ(g, -62f, -56f, -7f, 0f, NAVE, 1.4f, Wash);
            WallZ(g, -62f, -56f, 7f, 0f, NAVE, 1.4f, Wash);
            WallX(g, -7.7f, 7.7f, -62f, 0f, NAVE, 1.4f, Wash);
            g.HipRoof(new Vector3(0, NAVE, -59f), 16.6f, 7.4f, 3f, Tile);
            d.BoxMM(new Vector3(-6.3f, NAVE - 0.3f, -61.3f), new Vector3(6.3f, NAVE, -56f), Palette.Hex(0xece6da), F.Bottom);

            // floor, altar, retablo
            Props.Checker(g, -17.9f, 17.9f, -56f, -1.4f, 0.04f, 1.8f, Palette.Hex(0xe8e0cf), Palette.Hex(0x4a4440));
            Props.Checker(g, -6.3f, 6.3f, -61.3f, -56f, 0.04f, 1.8f, Palette.Hex(0xe8e0cf), Palette.Hex(0x4a4440));
            for (int k = 0; k < 3; k++) g.BoxMM(new Vector3(-6.3f, 0, -61.3f), new Vector3(6.3f, 0.3f * (k + 1), -53f - k * 0.8f), k == 2 ? Palette.Hex(0xd9cfbd) : Stone, F.Top | F.Front | F.Left | F.Right);
            float AY = 0.9f;
            d.BoxMM(new Vector3(-2f, AY, -57.6f), new Vector3(2f, AY + 1.0f, -56.6f), Props.Linen, F.All & ~F.Bottom);
            d.BoxMM(new Vector3(-2.05f, AY + 0.7f, -57.65f), new Vector3(2.05f, AY + 1.02f, -56.55f), Props.Gold, F.Front | F.Left | F.Right);
            for (int i = 0; i < 6; i++) Props.Candle(d, new Vector3(-1.75f + i * 0.7f, AY + 1.0f, -57.3f), 0.45f);
            Props.Cross(d, new Vector3(0, AY + 1.0f, -57.4f), 0.9f, Props.Gold);
            // retablo
            d.BoxMM(new Vector3(-6f, AY, -61.3f), new Vector3(6f, AY + 2.2f, -60.4f), Props.Gold, F.All & ~F.Bottom);
            d.BoxMM(new Vector3(-6f, AY + 2.2f, -61.3f), new Vector3(6f, AY + 10.5f, -61.05f), Palette.Hex(0x9a7a2a), F.Front);
            foreach (var x in new[] { -5.2f, -2.4f, 2.4f, 5.2f }) d.Cyl(new Vector3(x, AY + 2.2f, -60.7f), 0.28f, 0.24f, 7.4f, 8, Props.Gold, false);
            d.BoxMM(new Vector3(-6.3f, AY + 9.6f, -61.3f), new Vector3(6.3f, AY + 10.4f, -60.3f), Props.Gold, F.All);
            d.Lathe(new Vector3(0, AY + 10.4f, -60.8f), new[] { new Vector2(3.2f, 0f), new Vector2(2.8f, 1.6f), new Vector2(1.4f, 2.6f), new Vector2(0f, 2.9f) }, 8, Props.Gold);
            foreach (var (x, w, h) in new[] { (-3.8f, 1.6f, 3.2f), (3.8f, 1.6f, 3.2f), (0f, 2.6f, 5.2f), (-3.8f, 1.4f, 2.4f), (3.8f, 1.4f, 2.4f) })
            {
                float y0 = h > 3f ? AY + 2.8f : AY + 6.6f;
                d.BoxMM(new Vector3(x - w / 2, y0, -61.02f), new Vector3(x + w / 2, y0 + h, -61f), Palette.Hex(0x3a2a6a), F.Front);
                Props.Statue(d, new Vector3(x, y0, -60.85f), x == 0 ? Palette.Hex(0x3a5a9a) : r.Pick(new[] { Palette.Hex(0x8a2d2d), Palette.Hex(0x5a4330), Palette.Hex(0x2f4a35) }), h > 4f ? 1.9f : h / 3.2f * 1.2f);
            }
            // pulpit, side altars, benches, chandeliers
            d.Cyl(new Vector3(-7.9f, 0.04f, -18.6f), 0.2f, 0.25f, 2.4f, 6, Wood);
            d.Cyl(new Vector3(-7.9f, 2.4f, -18.6f), 0.5f, 0.9f, 0.4f, 6, Props.Gold);
            d.Cyl(new Vector3(-7.9f, 2.8f, -18.6f), 0.9f, 0.9f, 1.1f, 6, Wood);
            d.Cyl(new Vector3(-7.9f, 5.6f, -18.6f), 1.1f, 0.2f, 0.9f, 6, Props.Gold, true, true);
            foreach (var sx in new[] { -1f, 1f }) foreach (var z in new[] { -14f, -30f })
                {
                    d.Push(new Vector3(sx * 17.9f, 0.04f, z), sx > 0 ? -90f : 90f);
                    d.BoxMM(new Vector3(-1.6f, 0, -0.6f), new Vector3(1.6f, 1.1f, 0.2f), Props.Gold, F.All & ~F.Bottom);
                    d.BoxMM(new Vector3(-1.4f, 1.1f, -0.6f), new Vector3(1.4f, 4.6f, -0.4f), Palette.Hex(0x9a7a2a), F.Front);
                    Props.Statue(d, new Vector3(0, 1.1f, -0.3f), r.Pick(new[] { Palette.Hex(0x8a2d2d), Palette.Hex(0x3a5a9a), Palette.Hex(0x5a4330) }), 1.3f);
                    Props.Candle(d, new Vector3(-0.9f, 1.1f, 0f)); Props.Candle(d, new Vector3(0.9f, 1.1f, 0f));
                    d.Pop();
                }
            for (int i = 0; i < 6; i++) { float z = -26f - i * 2.4f; Props.Bench(d, new Vector3(-3.4f, 0.04f, z), 180, 5f); Props.Bench(d, new Vector3(3.4f, 0.04f, z), 180, 5f); }
            Props.Rugs(d, new Vector3(0, 0.04f, -14f), 5f, 8f, Props.Rug, Props.Rug2);
            foreach (var z in new[] { -12f, -26f, -40f })
            {
                d.CylBetween(new Vector3(0, 13.5f, z), new Vector3(0, 8.6f, z), 0.02f, 0.02f, 3, Iron, false);
                d.Cyl(new Vector3(0, 8.2f, z), 1.0f, 1.2f, 0.4f, 8, Props.Gold, false, true);
                for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4; Props.Candle(d, new Vector3(Mathf.Cos(a) * 1.05f, 8.6f, z + Mathf.Sin(a) * 1.05f), 0.22f, false); }
            }
            s.Light(new Vector3(0, 4.5f, -55f), new Color(1f, 0.82f, 0.55f), 16f, 2.2f, true);
            s.Light(new Vector3(0, 9f, -49f), new Color(1f, 0.92f, 0.78f), 20f, 1.1f);
            s.Light(new Vector3(0, 7f, -26f), new Color(1f, 0.85f, 0.65f), 18f, 1.2f, true);
            s.Light(new Vector3(0, 7f, -10f), new Color(1f, 0.85f, 0.65f), 16f, 0.9f, true);
            foreach (var sx in new[] { -1f, 1f }) s.Light(new Vector3(sx * 15f, 3f, -22f), new Color(1f, 0.8f, 0.55f), 12f, 0.9f, true);

            var e = s.AddEntrance("Iglesia Catedral", "Iglesia Mayor de Buenos Aires", new Vector3(0, 0, 3.4f), new Vector3(0, 0, -6.5f));
            s.AddLeaf(e, new Vector3(-2.2f, 0, -0.7f), 2.2f, 4.3f, 0.2f, false, WoodDark, 95f);
            s.AddLeaf(e, new Vector3(2.2f, 0, -0.7f), 2.2f, 4.3f, 0.2f, true, WoodDark, 95f);

            string[] whisper = {
                "Dicen que el obispo Lué habló en el Cabildo contra cualquier cambio.",
                "Y el doctor Castelli le contestó que, caída España, el poder vuelve al pueblo.",
                "Baje la voz, doña, que estamos en la casa de Dios.",
                "Recemos para que todo se resuelva sin sangre." };
            s.Actor(new Vector3(0, AY, -55.6f), 0, Outfit.Fraile, "Canónigo", 1, new[] { "Recemos por la paz de esta ciudad en días tan inciertos.", "Dios ilumine a los señores del Cabildo.", "Mañana repicarán las campanas, venga quien venga." }, false, "book", "none");
            s.Actor(new Vector3(-2.2f, 0.04f, -26f), 180, Outfit.Dama, "Doña Remedios", 2, whisper, true, "none", "mantilla");
            s.Actor(new Vector3(-3.4f, 0.04f, -26f), 180, Outfit.Dama, "Doña Petrona", 2, null, true, "none", "mantilla");
            s.Actor(new Vector3(-4.2f, 0.04f, -30.8f), 180, Outfit.Criolla, "Feligresa", -1, null, true);
            s.Actor(new Vector3(3.4f, 0.04f, -35.6f), 180, Outfit.Caballero, "Vecino devoto", -1, null, true, "none", "none");
            s.Actor(new Vector3(-12.5f, 0.04f, -30f), 90, Outfit.Peon, "Sacristán", 3, new[] { "Ya encendí las velas del altar mayor.", "Las campanas van a sonar fuerte esta semana, me dijeron.", "Cuidado con la cera, que resbala." }, false, "candles", "none");
            s.Actor(new Vector3(-7.2f, 0.04f, -6f), 90, Outfit.Fraile, "Fray Cayetano", 4, new[] { "Los franciscanos rezamos por todos, criollos y peninsulares.", "Esta fachada lleva años sin terminar… ¡paciencia!" });
            s.Actor(new Vector3(-5.9f, 0.04f, -5.4f), -90, Outfit.Criolla, "Doña Tomasa", 4, null, false, "none", "mantilla");

            s.Interior(new Vector3(-18f, -1f, -62f), new Vector3(18f, 30f, -1.4f));
            s.Finish(mat, "Catedral");
            return s;
        }

        static readonly Color32 WoodLightC = Palette.Hex(0x9a7048);
        static O[] Concat(O[] a, O[] b) { var r = new O[a.Length + b.Length]; a.CopyTo(r, 0); b.CopyTo(r, a.Length); return r; }

        // ======================================================================== RECOVA + PLAZA
        public static Site Recova(Material mat, Transform parent)
        {
            var s = new Site("recova", "Recova", City.W2U(City.LineC(6) + 6f, 398), Vector3.right, parent);
            var g = s.solid; var d = s.deco;
            // the frame's z axis points east; the building runs along local x (north-south), 12 m deep
            g.Push(new Vector3(0, 0, -6f));
            var arches = new System.Collections.Generic.List<O>();
            for (float x = -47f; x < 45f; x += 4f) if (x > 4f || x + 3f < -4f) arches.Add(A(x, x + 3f, 0f, 4.2f));
            foreach (var z in new[] { -5.6f, 5.6f })
            {
                WallX(g, -48f, -4.6f, z, 0f, 5.2f, 0.7f, Wash, arches.FindAll(o => o.x1 < -4.6f).ToArray());
                WallX(g, 4.6f, 48f, z, 0f, 5.2f, 0.7f, Wash, arches.FindAll(o => o.x0 > 4.6f).ToArray());
                WallX(g, -4.6f, 4.6f, z, 0f, 9.6f, 0.9f, Wash, A(-3.4f, 3.4f, 0f, 7.2f));   // Arco de los Virreyes
            }
            foreach (var x in new[] { -4.6f, 4.6f }) WallZ(g, -5.25f, 5.25f, x, 0f, 9.6f, 0.5f, Wash, A(-5.2f, -2.8f, 0f, 4.2f), A(2.8f, 5.2f, 0f, 4.2f));
            // shops back to back in the middle with a door every 4 m, and the central passage
            foreach (var z in new[] { -2.6f, 2.6f })
            {
                var doors = new System.Collections.Generic.List<O>();
                for (float x = -46f; x < 45f; x += 4f) if (x > 4.6f || x + 1.5f < -4.6f) doors.Add(R(x, x + 1.5f, 0f, 2.7f));
                WallX(g, -48f, -4.6f, z, 0f, 5.2f, 0.4f, Ochre, doors.FindAll(o => o.x1 < -4.6f).ToArray());
                WallX(g, 4.6f, 48f, z, 0f, 5.2f, 0.4f, Ochre, doors.FindAll(o => o.x0 > 4.6f).ToArray());
                foreach (var o in doors) d.BoxMM(new Vector3(o.x0, 0, z - (z > 0 ? 0.3f : -0.25f)), new Vector3(o.x1, 2.7f, z - (z > 0 ? 0.25f : -0.3f)), z > 0 ? CityBuilder.Wood : CityBuilder.WoodDark, F.Front | F.Back);
            }
            g.BoxMM(new Vector3(-48.3f, 0, -6f), new Vector3(-47.6f, 5.2f, 6f), Wash, F.All & ~F.Bottom);
            g.BoxMM(new Vector3(47.6f, 0, -6f), new Vector3(48.3f, 5.2f, 6f), Wash, F.All & ~F.Bottom);
            Props.Floor(g, -48f, 48f, -6f, 6f, 0.04f, Props.BrickFloor);
            g.BoxMM(new Vector3(-48.3f, 4.9f, -6.1f), new Vector3(-4.6f, 5.2f, 6.1f), Palette.Hex(0xe7dfcf), F.Bottom);
            g.BoxMM(new Vector3(4.6f, 4.9f, -6.1f), new Vector3(48.3f, 5.2f, 6.1f), Palette.Hex(0xe7dfcf), F.Bottom);
            g.BoxMM(new Vector3(-4.6f, 7.3f, -5.6f), new Vector3(4.6f, 7.5f, 5.6f), Palette.Hex(0xe7dfcf), F.Bottom);
            g.GableRoof(new Vector3(-26.2f, 5.2f, 0), 43.6f, 12.8f, 2.6f, Tile, Wash);
            g.GableRoof(new Vector3(26.2f, 5.2f, 0), 43.6f, 12.8f, 2.6f, Tile, Wash);
            g.HipRoof(new Vector3(0, 9.6f, 0), 10f, 12.8f, 2.2f, Tile);
            g.BoxMM(new Vector3(-5f, 9.3f, -6.4f), new Vector3(5f, 9.7f, 6.4f), Ochre, F.All);
            g.Pop();
            s.Finish(mat, "Recova");
            return s;
        }
    }
}
