using UnityEngine;
using O = Colonial.LowPoly.Opening;
using F = Colonial.LowPoly.F;

namespace Colonial
{
    public static partial class Landmarks
    {
        // ceiling under a gable roof: same prism mirrored so its faces point inward
        static void InnerGable(LowPoly g, Vector3 c, float w, float d, float rh, Color32 col)
        {
            g.PushM(Matrix4x4.TRS(c, Quaternion.identity, new Vector3(1, 1, -1)));
            g.GableRoof(Vector3.zero, w, d, rh, col, col, true);
            g.Pop();
        }

        // ======================================================================== REAL FORTALEZA
        public static Site Fuerte(Material mat, Material waterMat, Transform parent)
        {
            var s = new Site("fuerte", "Real Fortaleza", City.W2U(808, 398), Vector3.left, parent);
            s.mapRect = new Rect(796, 348, 100, 100);
            var g = s.solid; var d = s.deco; var r = new Rng(1594);
            var stone = Palette.Hex(0xcfc6b2); var stoneDark = Palette.Hex(0xa89f8a);
            const float WH = 8f;

            // ---- moat with drawbridge ----
            d.BoxMM(new Vector3(-50f, 0.02f, 0.4f), new Vector3(50f, 0.05f, 11f), Palette.Hex(0x5f7a72), F.Top);
            g.BoxMM(new Vector3(-50f, 0f, 11f), new Vector3(-2.8f, 0.5f, 11.6f), stoneDark, F.All & ~F.Bottom);
            g.BoxMM(new Vector3(2.8f, 0f, 11f), new Vector3(50f, 0.5f, 11.6f), stoneDark, F.All & ~F.Bottom);
            s.blockers.Add((new Vector3(-26.4f, 1f, 6f), new Vector3(47.2f, 2f, 10f)));
            s.blockers.Add((new Vector3(26.4f, 1f, 6f), new Vector3(47.2f, 2f, 10f)));
            g.BoxMM(new Vector3(-2.7f, 0f, 0f), new Vector3(2.7f, 0.32f, 11.4f), CityBuilder.Wood, F.All & ~F.Bottom);
            for (float z = 0.5f; z < 11f; z += 0.7f) d.BoxMM(new Vector3(-2.7f, 0.32f, z), new Vector3(2.7f, 0.34f, z + 0.06f), CityBuilder.WoodDark, F.Top);
            foreach (var sx in new[] { -2.5f, 2.5f }) d.CylBetween(new Vector3(sx, 0.4f, 10.8f), new Vector3(sx, 7.2f, 0.3f), 0.05f, 0.05f, 4, Iron, false);

            // ---- curtain walls (4 m thick, 8 m high) with merlons ----
            WallX(g, -44f, 44f, -2f, 0f, WH, 4f, stone, A(-2.6f, 2.6f, 0f, 6.2f));
            WallX(g, -44f, 44f, -86f, 0f, WH, 4f, stone);
            WallZ(g, -84f, -4f, -42f, 0f, WH, 4f, stone);
            WallZ(g, -84f, -4f, 42f, 0f, WH, 4f, stone);
            void Merlons(Vector3 a, Vector3 b, Vector3 outward)
            {
                var dir = (b - a); float len = dir.magnitude; dir /= len;
                for (float t = 0.6f; t < len - 0.6f; t += 2.2f)
                {
                    var p = a + dir * t + outward * 0.4f;
                    g.Push(p, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg); g.Box(new Vector3(0, WH + 0.6f, 0), new Vector3(0.8f, 1.2f, 1.3f), stone, F.All & ~F.Bottom); g.Pop();
                }
                var mid = (a + b) / 2 + outward * 0.4f;
                var sz = new Vector3(Mathf.Abs(dir.x) * len + Mathf.Abs(outward.x) * 0.8f, 2.4f, Mathf.Abs(dir.z) * len + Mathf.Abs(outward.z) * 0.8f);
                s.blockers.Add((mid + Vector3.up * (WH + 1.2f), sz));
            }
            Merlons(new Vector3(-44, 0, 0), new Vector3(44, 0, 0), Vector3.zero);
            Merlons(new Vector3(-44, 0, -88), new Vector3(44, 0, -88), Vector3.forward * 0f);
            Merlons(new Vector3(-44, 0, 0), new Vector3(-44, 0, -88), Vector3.zero);
            Merlons(new Vector3(44, 0, 0), new Vector3(44, 0, -88), Vector3.zero);
            // bastions with sentry boxes at the corners
            foreach (var (cx, cz) in new[] { (-44f, 0f), (44f, 0f), (-44f, -88f), (44f, -88f) })
            {
                g.Push(new Vector3(cx, 0, cz), 45f);
                g.BoxMM(new Vector3(-6.5f, 0, -6.5f), new Vector3(6.5f, WH, 6.5f), stone, F.All & ~F.Bottom);
                g.BoxMM(new Vector3(-6.8f, WH - 0.4f, -6.8f), new Vector3(6.8f, WH, 6.8f), stoneDark, F.All);
                g.Pop();
                var tip = new Vector3(cx + Mathf.Sign(cx) * 8.6f, WH, cz + (cz < -1f ? -8.6f : 8.6f));
                g.Cyl(tip, 0.9f, 0.9f, 2.4f, 8, stone, false);
                g.Lathe(tip + Vector3.up * 2.4f, new[] { new Vector2(1.1f, 0f), new Vector2(0.9f, 0.6f), new Vector2(0f, 1.2f) }, 8, stone);
                s.blockers.Add((tip + Vector3.up * 1.5f - new Vector3(Mathf.Sign(cx), 0, cz < -1f ? -1 : 1) * 1.6f, new Vector3(5f, 3f, 5f)));
                Props.Cannon(d, new Vector3(cx + Mathf.Sign(cx) * 3.5f, WH, cz + (cz < -1f ? -3.5f : 3.5f)), Mathf.Atan2(Mathf.Sign(cx), cz < -1f ? -1 : 1) * Mathf.Rad2Deg);
            }

            // ---- gatehouse ----
            g.BoxMM(new Vector3(-6f, 0, -12f), new Vector3(-2.6f, WH, -4f), stone, F.All & ~F.Bottom);
            g.BoxMM(new Vector3(2.6f, 0, -12f), new Vector3(6f, WH, -4f), stone, F.All & ~F.Bottom);
            g.BoxMM(new Vector3(-2.6f, 6.2f, -12f), new Vector3(2.6f, WH, 0f), stone, F.Bottom | F.Top | F.Back);
            d.BoxMM(new Vector3(-3.4f, 6.4f, 0f), new Vector3(3.4f, 7.4f, 0.35f), stoneDark, F.Front | F.Top | F.Bottom);
            // royal coat of arms over the gate
            d.BoxMM(new Vector3(-1f, 6.6f, 0.36f), new Vector3(1f, 7.9f, 0.42f), Palette.Hex(0xb02020), F.Front);
            d.BoxMM(new Vector3(-0.6f, 6.9f, 0.42f), new Vector3(0.6f, 7.6f, 0.46f), Palette.Hex(0xe8c030), F.Front);
            var gate = s.AddEntrance("Real Fortaleza de Buenos Aires", "Plaza de armas · residencia del virrey", new Vector3(0, 0.35f, 6.5f), new Vector3(0, 0, -16f));
            s.AddLeaf(gate, new Vector3(-2.6f, 0, -0.6f), 2.6f, 3.6f, 0.25f, false, CityBuilder.WoodDark, 95f);
            s.AddLeaf(gate, new Vector3(2.6f, 0, -0.6f), 2.6f, 3.6f, 0.25f, true, CityBuilder.WoodDark, 95f);

            // ---- ramp up to the walls, cannons on the walls ----
            g.Quad(new Vector3(-40f, WH, -54f), new Vector3(-40f, 0.02f, -30f), new Vector3(-36f, 0.02f, -30f), new Vector3(-36f, WH, -54f), stoneDark);
            g.Tri(new Vector3(-36f, 0f, -54f), new Vector3(-36f, WH, -54f), new Vector3(-36f, 0f, -30f), stone);
            g.BoxMM(new Vector3(-40f, 0, -58f), new Vector3(-36f, WH, -54f), stone, F.All & ~F.Bottom);
            foreach (var x in new[] { -30f, -16f, 16f, 30f }) { Props.Cannon(d, new Vector3(x, WH, -1.6f), 0); Props.Cannon(d, new Vector3(x, WH, -86.4f), 180); }
            foreach (var z in new[] { -24f, -44f, -64f }) { Props.Cannon(d, new Vector3(42.4f, WH, z), 90); Props.Cannon(d, new Vector3(-42.4f, WH, z - 6f), -90); }

            // ---- plaza de armas ----
            Props.Flag(d, new Vector3(0, 0, -34f), 15f);
            d.Cyl(new Vector3(0, 0, -34f), 1.2f, 1.0f, 0.5f, 8, stoneDark);
            foreach (var x in new[] { -12f, -6f, 6f, 12f }) Props.Cannon(d, new Vector3(x, 0.02f, -24f), 0);
            Props.CannonBalls(d, new Vector3(-9f, 0.02f, -26f)); Props.CannonBalls(d, new Vector3(9f, 0.02f, -26f));
            Props.Floor(d, -3f, 3f, -62f, -12f, 0.03f, Palette.Hex(0xb6ab95));
            for (int i = 0; i < 6; i++) Props.Barrel(d, new Vector3(-34f + (i % 3) * 0.8f, 0.02f, -66f - (i / 3) * 0.8f));
            // barracks along the right wall
            WallX(g, 26f, 40f, -12f, 0f, 4.5f, 0.6f, Wash);
            WallX(g, 26f, 40f, -50f, 0f, 4.5f, 0.6f, Wash);
            var bdoors = new O[4]; for (int i = 0; i < 4; i++) { float zc = -18f - 9f * i; bdoors[i] = R(zc - 0.8f, zc + 0.8f, 0f, 2.6f); }
            WallZ(g, -50f, -12f, 26f, 0f, 4.5f, 0.6f, Wash, bdoors);
            for (int i = 0; i < 4; i++) d.BoxMM(new Vector3(26.32f, 0f, -18.8f - 9f * i), new Vector3(26.36f, 2.6f, -17.2f - 9f * i), WoodDark, F.Left | F.Right);
            g.Push(new Vector3(33f, 4.5f, -31f), 90f); g.GableRoof(Vector3.zero, 38.8f, 14.8f, 2.6f, Tile, Wash); g.Pop();
            for (int i = 0; i < 3; i++) { d.Push(new Vector3(25.4f, 0.02f, -22f - 9f * i), -90f); for (int k = 0; k < 5; k++) d.CylBetween(new Vector3(-0.8f + k * 0.4f, 0, 0.1f), new Vector3(-0.8f + k * 0.4f, 1.8f, -0.1f), 0.03f, 0.02f, 4, CityBuilder.Iron, false); d.Box(new Vector3(0, 1.2f, 0), new Vector3(2.2f, 0.08f, 0.15f), Wood); d.Pop(); }
            // powder magazine
            g.BoxMM(new Vector3(-40f, 0, -84f), new Vector3(-30f, 4f, -72f), stone, F.All & ~F.Bottom);
            g.Push(new Vector3(-35f, 4f, -78f), 90f); g.GableRoof(Vector3.zero, 12.4f, 10.4f, 2.2f, stoneDark, stone); g.Pop();
            d.BoxMM(new Vector3(-36f, 0f, -71.96f), new Vector3(-34f, 2.4f, -71.9f), Iron, F.Front);

            // ---- Casa de los Virreyes: arcade, Salón del Virrey ----
            const float G = 5f, U = 10f;
            var pal = new O[9]; for (int k = 0; k < 9; k++) { float x0 = -17.5f + 4f * k; pal[k] = A(x0, x0 + 3f, 0f, 4.1f); }
            WallX(g, -24f, 24f, -62.3f, 0f, G, 0.6f, Palette.Hex(0xf3ead7), pal);
            WallX(g, -24f, 24f, -62.3f, G, U, 0.6f, Palette.Hex(0xf3ead7));
            d.Push(new Vector3(0, 0, -62f)); foreach (var x in new[] { -16f, -8f, 0f, 8f, 16f }) CityBuilder.Balcony(d, x, G + 0.6f); d.Pop();
            g.BoxMM(new Vector3(-24.3f, G - 0.2f, -62f), new Vector3(24.3f, G + 0.15f, -61.7f), Ochre, F.All);
            g.BoxMM(new Vector3(-24.4f, U - 0.3f, -62f), new Vector3(24.4f, U + 0.1f, -61.5f), Ochre, F.All);
            WallX(g, -24f, 24f, -66.2f, 0f, G, 0.6f, Palette.Hex(0xf3ead7), R(-1.5f, 1.5f, 0f, 3.8f));
            g.BoxMM(new Vector3(-24f, G - 0.3f, -66.5f), new Vector3(24f, G, -62f), Palette.Hex(0xe7dfcf), F.Bottom);
            Props.Floor(g, -24f, 24f, -66f, -62.6f, 0.04f, Props.BrickFloor);
            g.BoxMM(new Vector3(-24f, 0, -84f), new Vector3(-12.3f, U, -66.5f), Palette.Hex(0xf3ead7), F.All & ~F.Bottom);
            g.BoxMM(new Vector3(12.3f, 0, -84f), new Vector3(24f, U, -66.5f), Palette.Hex(0xf3ead7), F.All & ~F.Bottom);
            g.BoxMM(new Vector3(-24.3f, U - 0.35f, -84f), new Vector3(24.3f, U, -62f), Palette.Hex(0xe7dfcf), F.Bottom);
            g.HipRoof(new Vector3(0, U, -73f), 49.2f, 23f, 3.4f, Tile);
            WallX(g, -24f, 24f, -84.15f, 0f, U, 0.3f, Palette.Hex(0xf3ead7));   // the palace's own back wall (taller than the curtain wall)
            var salon = s.AddEntrance("Salón del Virrey", "Casa de los Virreyes · donde Cisneros espera noticias", new Vector3(0, 0.04f, -63.6f), new Vector3(0, 0.04f, -71f), "Salir a la plaza de armas");
            s.AddLeaf(salon, new Vector3(-1.5f, 0, -66.2f), 1.5f, 3.8f, 0.14f, false, Palette.Hex(0x5a2a1a));
            s.AddLeaf(salon, new Vector3(1.5f, 0, -66.2f), 1.5f, 3.8f, 0.14f, true, Palette.Hex(0x5a2a1a));
            // Salón
            Props.Checker(d, -12.3f, 12.3f, -84f, -66.5f, 0.04f, 1.2f, Palette.Hex(0xe8e0cf), Palette.Hex(0x7a3a2a));
            Props.Ceiling(d, -12.3f, 12.3f, -84f, -66.5f, U - 0.36f, Palette.Hex(0xefe6d2), Props.Gold, false);
            d.BoxMM(new Vector3(-1.2f, 0.04f, -82f), new Vector3(1.2f, 0.06f, -66.6f), Props.Velvet, F.Top);
            d.BoxMM(new Vector3(-3.5f, 0.04f, -84f), new Vector3(3.5f, 0.4f, -80f), Props.Velvet, F.Top | F.Front | F.Left | F.Right);
            d.BoxMM(new Vector3(-2.6f, 0.4f, -84f), new Vector3(2.6f, 0.7f, -81.5f), Props.Velvet, F.Top | F.Front | F.Left | F.Right);
            // throne under a canopy (dosel)
            d.Push(new Vector3(0, 0.7f, -83f));
            d.BoxMM(new Vector3(-0.6f, 0, -0.5f), new Vector3(0.6f, 0.55f, 0.4f), Props.Gold, F.All & ~F.Bottom);
            d.BoxMM(new Vector3(-0.55f, 0.55f, -0.45f), new Vector3(0.55f, 0.65f, 0.35f), Props.Velvet, F.All);
            d.BoxMM(new Vector3(-0.6f, 0.55f, -0.55f), new Vector3(0.6f, 2.0f, -0.4f), Props.Velvet, F.All);
            d.BoxMM(new Vector3(-0.68f, 1.9f, -0.58f), new Vector3(0.68f, 2.15f, -0.38f), Props.Gold, F.All);
            d.Pop();
            d.BoxMM(new Vector3(-3f, 0.7f, -83.95f), new Vector3(3f, 6.8f, -83.85f), Props.Velvet, F.Front);
            d.BoxMM(new Vector3(-3.3f, 6.6f, -84f), new Vector3(3.3f, 7.2f, -81.2f), Props.Velvet, F.All);
            d.BoxMM(new Vector3(-3.35f, 6.5f, -81.25f), new Vector3(3.35f, 6.65f, -81.1f), Props.Gold, F.All);
            d.BoxMM(new Vector3(-0.9f, 4f, -83.84f), new Vector3(0.9f, 5.8f, -83.8f), Palette.Hex(0xe8c030), F.Front);
            d.BoxMM(new Vector3(-0.5f, 4.3f, -83.8f), new Vector3(0.5f, 5.5f, -83.76f), Palette.Hex(0xb02020), F.Front);
            Props.Painting(d, new Vector3(-12.2f, 4.2f, -73f), 90, 2f, 2.8f, Props.Canvas, Palette.Hex(0x7a1f22));
            Props.Painting(d, new Vector3(12.2f, 4.2f, -73f), -90, 2f, 2.8f, Props.Canvas, Palette.Hex(0x1f2a44));
            Props.Painting(d, new Vector3(-12.2f, 4.2f, -79f), 90, 1.6f, 2.2f, Props.Canvas, Palette.Hex(0x2f4a35));
            Props.Table(d, new Vector3(7.5f, 0.04f, -76f), 90, 3f, 1.4f, Palette.Hex(0x7a1f22), 0.82f, true);
            Props.Papers(d, new Vector3(7.5f, 0.88f, -76f), r); Props.Candelabra(d, new Vector3(7.6f, 0.88f, -75f));
            Props.Chair(d, new Vector3(8.6f, 0.04f, -76f), -90, Props.Velvet);
            foreach (var z in new[] { -69f, -72f, -75f, -78f }) { Props.Chair(d, new Vector3(-11.4f, 0.04f, z), 90, Props.Velvet); }
            foreach (var z in new[] { -71f, -78f })
            {
                d.CylBetween(new Vector3(0, U - 0.4f, z), new Vector3(0, 6.8f, z), 0.02f, 0.02f, 3, Iron, false);
                d.Cyl(new Vector3(0, 6.4f, z), 0.9f, 1.1f, 0.4f, 8, Props.Gold, false, true);
                for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4; Props.Candle(d, new Vector3(Mathf.Cos(a) * 0.95f, 6.8f, z + Mathf.Sin(a) * 0.95f), 0.2f, false); }
                s.Light(new Vector3(0, 6.2f, z), new Color(1f, 0.85f, 0.6f), 13f, 1.7f, true);
            }
            s.Light(new Vector3(0, 3f, -81f), new Color(1f, 0.8f, 0.55f), 9f, 1.2f, true);

            // ---- people ----
            s.Actor(new Vector3(-3.2f, 0.32f, 9f), 0, Outfit.Patricio, "Centinela del Fuerte", 1, new[] { "¡Alto! ¿Quién vive?", "El virrey no recibe a nadie sin audiencia.", "Dicen que en el Cabildo se decide todo hoy…" });
            s.Actor(new Vector3(3.2f, 0.32f, 9f), 0, Outfit.Patricio, "Centinela del Fuerte", 1);
            s.Actor(new Vector3(-8f, 0.02f, -40f), 60, Outfit.Patricio, "Soldado", 2, new[] { "Dicen que mañana se forma una Junta nueva.", "¿Y a quién vamos a obedecer nosotros?", "Al que pague la soldada, compadre.", "Callá, que te oye el sargento." });
            s.Actor(new Vector3(-6.2f, 0.02f, -41.4f), -50, Outfit.Patricio, "Soldado", 2);
            s.Actor(new Vector3(-6.4f, 0.02f, -38.4f), -130, Outfit.Peon, "Tambor", 2);
            s.Actor(new Vector3(30f, WH, -3.6f), 180, Outfit.Patricio, "Artillero", 3, new[] { "Este cañón no dispara desde las Invasiones Inglesas.", "En 1807 sí que hubo pólvora en esta ciudad.", "Desde aquí se ve todo el río, ¡mire los barcos!" });
            s.Actor(new Vector3(6.6f, 0.04f, -76.4f), 90, Outfit.Virrey, "Virrey Cisneros", 4, new[] {
                "Me piden que deje el mando… ¡a mí, virrey nombrado por la Junta Central!",
                "Excelencia, los comandantes de los cuerpos criollos no lo van a sostener.",
                "El 18 tuve que admitir en mi proclama que Sevilla había caído.",
                "Saavedra fue claro, excelencia: su tiempo ha terminado.",
                "Si el Cabildo lo resuelve, me someteré… pero esto no termina aquí.",
                "Excelencia, el Cabildo pide su renuncia por escrito." });
            s.Actor(new Vector3(4.6f, 0.04f, -75f), -100, Outfit.Caballero, "Oidor de la Audiencia", 4);
            s.Actor(new Vector3(4.8f, 0.04f, -77.6f), -70, Outfit.Levita, "Secretario del virrey", 4);
            s.Actor(new Vector3(-2.6f, 0.04f, -67.6f), 180, Outfit.Patricio, "Alabardero", 5, new[] { "Silencio en el salón de su excelencia.", "Quítese el sombrero ante el retrato del rey." });

            s.Interior(new Vector3(-44f, -1f, -88f), new Vector3(44f, 14f, -1f));
            s.Finish(mat, "Fuerte");
            return s;
        }

        // ======================================================================== JABONERÍA DE VIEYTES
        public static Site Jaboneria(Material mat, Transform parent)
        {
            var place = City.Data.places.jaboneria;
            var s = new Site("jaboneria", "Jabonería de Vieytes", City.W2U(place.foot.x + place.foot.w / 2, place.foot.z), Vector3.forward, parent);
            s.mapRect = new Rect(place.foot.x, place.foot.z, place.foot.w, 48);
            var g = s.solid; var d = s.deco; var r = new Rng(1806);
            var wall = Palette.Hex(0xeee0c2);
            const float H = 6f;
            // facade
            WallX(g, -18f, 18f, -0.3f, 0f, H, 0.6f, wall, R(-1.7f, 1.7f, 0f, 3.6f));
            foreach (var x in new[] { -13f, -8.5f, 8.5f, 13f }) CityBuilder.Reja(d, x, 1.0f, 1.3f, 1.9f);
            d.BoxMM(new Vector3(-2.3f, 0, 0), new Vector3(2.3f, 4.3f, 0.12f), CityBuilder.Ochre, F.Front | F.Left | F.Right | F.Top);
            d.BoxMM(new Vector3(-18f, 0, 0), new Vector3(18f, 0.85f, 0.1f), CityBuilder.Zocalo, F.Front | F.Top);
            g.BoxMM(new Vector3(-18.2f, H - 0.3f, -0.6f), new Vector3(18.2f, H, 0.3f), Wash, F.All);
            // hanging sign: a soap bar on an iron bracket
            d.CylBetween(new Vector3(3.2f, 4.6f, 0f), new Vector3(3.2f, 4.6f, 1.4f), 0.03f, 0.03f, 4, Iron, false);
            d.Box(new Vector3(3.2f, 4.1f, 1.1f), new Vector3(0.06f, 0.9f, 1.3f), CityBuilder.WoodLight, F.All);
            d.Box(new Vector3(3.2f, 4.1f, 1.1f), new Vector3(0.1f, 0.4f, 0.7f), Props.Soap, F.All);
            var e = s.AddEntrance("Jabonería de Vieytes", "Fábrica de jabón de Vieytes y Rodríguez Peña", new Vector3(0, 0, 2.6f), new Vector3(0, 0, -5f));
            s.AddLeaf(e, new Vector3(-1.7f, 0, -0.3f), 1.7f, 3.6f, 0.15f, false, CityBuilder.Wood);
            s.AddLeaf(e, new Vector3(1.7f, 0, -0.3f), 1.7f, 3.6f, 0.15f, true, CityBuilder.Wood);
            // shell: side walls, back wall, roof with exposed trusses
            WallZ(g, -26f, -0.6f, -17.7f, 0f, H, 0.6f, wall, R(-12f, -10.6f, 1.2f, 2.6f));
            WallZ(g, -26f, -0.6f, 17.7f, 0f, H, 0.6f, wall, R(-12f, -10.6f, 1.2f, 2.6f));
            WallX(g, -18f, 18f, -26f, 0f, H, 0.6f, wall, R(10f, 12.5f, 0f, 3f));
            g.GableRoof(new Vector3(0, H, -13f), 36.8f, 27.2f, 3.4f, Tile, wall);
            InnerGable(d, new Vector3(0, H - 0.05f, -13f), 35.2f, 25.8f, 3.3f, Palette.Hex(0x7a5a3a));
            for (float x = -15f; x <= 15f; x += 5f)
            {
                d.BoxMM(new Vector3(x - 0.12f, H - 0.3f, -25.6f), new Vector3(x + 0.12f, H - 0.06f, -0.6f), CityBuilder.WoodDark, F.All & ~F.Top);
                d.CylBetween(new Vector3(x, H - 0.2f, -13f), new Vector3(x, H + 3.2f, -13f), 0.1f, 0.1f, 4, CityBuilder.WoodDark, false);
            }
            Props.Floor(g, -17.4f, 17.4f, -25.7f, -0.6f, 0.04f, Props.BrickFloor);
            // shop corner: counter, shelves of soap, scale
            d.BoxMM(new Vector3(-16f, 0.04f, -4.6f), new Vector3(-8f, 1.05f, -3.8f), CityBuilder.Wood, F.All & ~F.Bottom);
            d.BoxMM(new Vector3(-16.1f, 1.05f, -4.7f), new Vector3(-7.9f, 1.12f, -3.7f), CityBuilder.WoodDark, F.All);
            foreach (var z in new[] { -2.2f, -5.6f, -9f }) Props.Shelf(d, new Vector3(-17.3f, 0.04f, z), 90, 3.2f, 2.8f, 5, (l, k) => (k + l) % 5 == 4 ? new Color32(0, 0, 0, 0) : (k % 2 == 0 ? Props.Soap : Props.Soap2));
            d.Cyl(new Vector3(-10f, 1.12f, -4.2f), 0.04f, 0.04f, 0.5f, 4, CityBuilder.Brass);
            d.BoxMM(new Vector3(-10.4f, 1.6f, -4.22f), new Vector3(-9.6f, 1.64f, -4.18f), CityBuilder.Brass, F.All);
            foreach (var sx in new[] { -0.35f, 0.35f }) d.Cyl(new Vector3(-10f + sx, 1.3f, -4.2f), 0.15f, 0.15f, 0.04f, 7, CityBuilder.Brass, true, true);
            for (int i = 0; i < 6; i++) d.Box(new Vector3(-14.5f + i * 0.5f, 1.17f, -4.2f), new Vector3(0.3f, 0.1f, 0.2f), Props.Soap, F.All & ~F.Bottom);
            // workshop: boiling pans, moulds, tallow barrels, ash sacks
            foreach (var z in new[] { -6.5f, -12.5f, -18.5f })
            {
                Props.Cauldron(d, new Vector3(14.6f, 0.04f, z));
                d.BoxMM(new Vector3(15f, 1.0f, z - 0.6f), new Vector3(17.4f, H + 3.2f, z + 0.6f), CityBuilder.Brick, F.All & ~F.Bottom);
                s.Light(new Vector3(13.2f, 1.2f, z), new Color(1f, 0.55f, 0.2f), 7f, 2.2f, true);
            }
            s.blockers.Add((new Vector3(14.6f, 1f, -12.5f), new Vector3(2.4f, 2f, 15f)));
            for (int i = 0; i < 3; i++)
            {
                float z = -6f - i * 4.5f;
                Props.Table(d, new Vector3(3.5f, 0.04f, z), 0, 4f, 1.4f, CityBuilder.Wood, 0.9f);
                for (int k = 0; k < 6; k++) d.Box(new Vector3(2f + k * 0.6f, 0.98f, z + ((k % 2) - 0.5f) * 0.5f), new Vector3(0.45f, 0.16f, 0.32f), k % 3 == 0 ? Props.Soap2 : Props.Soap, F.All & ~F.Bottom);
            }
            for (int i = 0; i < 7; i++) Props.Barrel(d, new Vector3(-2.5f + (i % 4) * 0.85f, 0.04f, -22f - (i / 4) * 0.85f));
            for (int i = 0; i < 5; i++) Props.Sack(d, new Vector3(8f + i * 0.7f, 0.04f, -24.6f + (i % 2) * 0.5f), Props.Ash);
            for (int i = 0; i < 3; i++) Props.Crate(d, new Vector3(-6f + i * 0.75f, 0.04f, -2.2f), r.Range(-10, 10));
            s.Light(new Vector3(2f, 4.5f, -10f), new Color(1f, 0.85f, 0.65f), 16f, 0.9f);
            // trastienda: the secret meeting room
            WallX(g, -17.4f, -4f, -16f, 0f, 4.2f, 0.4f, wall, R(-9f, -7.6f, 0f, 2.4f));
            WallZ(g, -25.7f, -16.2f, -4.2f, 0f, 4.2f, 0.4f, wall);
            g.BoxMM(new Vector3(-17.4f, 4.2f, -25.7f), new Vector3(-4f, 4.4f, -15.8f), Palette.Hex(0x7a5a3a), F.Top | F.Bottom);
            Props.Floor(d, -17.4f, -4.4f, -25.7f, -16.2f, 0.06f, Props.FloorTile);
            Props.Table(d, new Vector3(-10.8f, 0.06f, -21f), 90, 3.4f, 1.6f, CityBuilder.Wood, 0.8f);
            Props.Papers(d, new Vector3(-10.8f, 0.86f, -21f), r); Props.Papers(d, new Vector3(-10.5f, 0.87f, -21.8f), r);
            Props.Candelabra(d, new Vector3(-10.8f, 0.86f, -20.3f)); Props.Candle(d, new Vector3(-10.6f, 0.86f, -22.2f));
            d.BoxMM(new Vector3(-15f, 1.6f, -25.66f), new Vector3(-12f, 3.4f, -25.6f), Palette.Hex(0xd9c79a), F.Front);   // map of the Río de la Plata
            d.BoxMM(new Vector3(-14.4f, 2.0f, -25.6f), new Vector3(-12.6f, 3.0f, -25.58f), Palette.Hex(0x7a9aa6), F.Front);
            for (int i = 0; i < 3; i++) { Props.Chair(d, new Vector3(-12.2f, 0.06f, -22f + i * 1.0f), 90, Props.Velvet); Props.Chair(d, new Vector3(-9.4f, 0.06f, -22f + i * 1.0f), -90, Props.Velvet); }
            Props.Shelf(d, new Vector3(-6f, 0.06f, -25.6f), 0, 2.4f, 2.4f, 4, (l, k) => k % 3 == 2 ? new Color32(0, 0, 0, 0) : Palette.Hex(k % 2 == 0 ? 0x5a2b1a : 0x3a2418));
            s.Light(new Vector3(-10.8f, 2.4f, -21f), new Color(1f, 0.78f, 0.5f), 8f, 1.8f, true);
            // back yard
            WallZ(g, -46f, -26.3f, -17.7f, 0f, 3f, 0.6f, wall); WallZ(g, -46f, -26.3f, 17.7f, 0f, 3f, 0.6f, wall);
            WallX(g, -18f, 18f, -46f, 0f, 3f, 0.6f, wall);
            Props.Floor(d, -17.4f, 17.4f, -45.7f, -26.3f, 0.03f, Palette.Hex(0x9a8566));
            for (int i = 0; i < 4; i++) Props.Barrel(d, new Vector3(-12f + i * 1.5f, 0.03f, -32f), 1.5f);
            for (int i = 0; i < 18; i++) d.CylBetween(new Vector3(6f, 0.25f + (i / 6) * 0.42f, -40f + (i % 6) * 0.42f), new Vector3(9f, 0.25f + (i / 6) * 0.42f, -40f + (i % 6) * 0.42f), 0.2f, 0.2f, 6, CityBuilder.Wood);
            Props.Cart(d, new Vector3(-10f, 0.03f, -40f), 80f, false);
            CityBuilder.Tree(d, new Vector3(12f, 0, -32f), 1.1f, new Rng(18));

            string[] secreto = {
                "Pasen, pasen. Aquí, entre el olor a sebo, nadie sospecha nada.",
                "Señores, la caída de la Junta Central nos deja sin gobierno legítimo.",
                "Entonces la soberanía vuelve al pueblo. Hay que decirlo en el Cabildo.",
                "Cisneros tendrá que aceptar un Cabildo Abierto.",
                "Mis muchachos están listos para llenar la plaza.",
                "Y repartiremos cintas para reconocernos entre patriotas.",
                "Hay que escribir, publicar, explicarle al pueblo lo que está en juego.",
                "Lo que buscamos es un gobierno propio, y comercio libre para estas tierras." };
            s.Actor(new Vector3(-8.8f, 0.06f, -21f), -90, Outfit.Levita, "Hipólito Vieytes", 1, secreto);
            s.Actor(new Vector3(-12.2f, 0.06f, -22f), 90, Outfit.Caballero, "Manuel Belgrano", 1, null, true, "none", "none");
            s.Actor(new Vector3(-12.2f, 0.06f, -21f), 90, Outfit.Levita, "Juan José Castelli", 1, null, true, "none", "none");
            s.Actor(new Vector3(-9.4f, 0.06f, -22f), -90, Outfit.Caballero, "Nicolás Rodríguez Peña", 1, null, true, "none", "none");
            s.Actor(new Vector3(-13.6f, 0.06f, -19.2f), 130, Outfit.Patricio, "Domingo French", 1, null, false, "none", "bi");
            s.Actor(new Vector3(-7.4f, 0.06f, -19f), -140, Outfit.Levita, "Antonio Beruti", 1);
            s.Actor(new Vector3(-12.2f, 0.06f, -20f), 90, Outfit.Levita, "Mariano Moreno", 1, null, true, "book", "none");
            s.Actor(new Vector3(12.6f, 0.04f, -9.5f), 90, Outfit.Peon, "Jabonero", 2, new[] { "Este jabón sale blanco como la nieve, ¡el mejor de Buenos Aires!", "Revolvé la paila, que el sebo no se pegue.", "De noche vienen señores muy serios… yo no vi nada, ¿eh?" });
            s.Actor(new Vector3(12.6f, 0.04f, -15.5f), 90, Outfit.Peon, "Aprendiz", 2);
            s.Actor(new Vector3(-12f, 0.04f, -5.4f), 0, Outfit.Criolla, "Despachante", 3, new[] { "¿Jabón blanco o jabón de sebo, señor?", "A medio real la pastilla, el mejor precio de la ciudad.", "Don Hipólito está ocupado atrás, con unos amigos." });

            s.Interior(new Vector3(-17.7f, -1f, -46f), new Vector3(17.7f, 9f, -0.6f));
            s.Finish(mat, "Jaboneria");
            return s;
        }

        // ======================================================================== CASA DE DON BLAS
        public static Site DonBlas(Material mat, Transform parent)
        {
            var place = City.Data.places.donblas;
            var s = new Site("donblas", "Casa de Don Blas", City.W2U(place.foot.x + place.foot.w / 2, place.foot.z + place.foot.d), Vector3.back, parent);
            s.mapRect = new Rect(place.foot.x, place.foot.z + place.foot.d - 74, place.foot.w, 74);
            var g = s.solid; var d = s.deco; var r = new Rng(1795);
            var wall = Palette.Hex(0xf3ecdc); var ceil = Palette.Hex(0xefe8d8);
            const float H = 5.4f;
            // facade with a carved doorway, grilles, parapet and urns
            WallX(g, -13f, 13f, -0.3f, 0f, H, 0.6f, wall, R(-1.6f, 1.6f, 0f, 3.4f));
            foreach (var x in new[] { -9.2f, -5.2f, 5.2f, 9.2f }) CityBuilder.Reja(d, x, 1.0f, 1.3f, 2.0f);
            d.BoxMM(new Vector3(-13f, 0, 0), new Vector3(13f, 0.85f, 0.1f), CityBuilder.Zocalo, F.Front | F.Top);
            foreach (var x in new[] { -2.5f, 2.5f }) d.BoxMM(new Vector3(x - 0.4f, 0, 0), new Vector3(x + 0.4f, 4.4f, 0.3f), CityBuilder.Ochre, F.Front | F.Left | F.Right | F.Top);
            d.BoxMM(new Vector3(-3.1f, 4.1f, 0), new Vector3(3.1f, 4.5f, 0.4f), CityBuilder.Ochre, F.All & ~F.Back);
            d.Push(new Vector3(0, 4.5f, 0.2f), 90f); d.GableRoof(Vector3.zero, 0.4f, 6.2f, 1.0f, CityBuilder.Ochre, CityBuilder.Ochre); d.Pop();
            g.BoxMM(new Vector3(-13.2f, H - 0.3f, -0.6f), new Vector3(13.2f, H, 0.3f), Wash, F.All);
            g.BoxMM(new Vector3(-13f, H, -0.6f), new Vector3(13f, H + 0.8f, 0f), wall, F.All & ~F.Bottom);
            foreach (var x in new[] { -12.4f, -6f, 6f, 12.4f }) { d.Box(new Vector3(x, H + 1.1f, -0.3f), new Vector3(0.45f, 0.6f, 0.45f), Wash); d.Lathe(new Vector3(x, H + 1.4f, -0.3f), new[] { new Vector2(0f, 0f), new Vector2(0.2f, 0f), new Vector2(0.28f, 0.3f), new Vector2(0.1f, 0.55f), new Vector2(0f, 0.6f) }, 7, Wash); }
            var e = s.AddEntrance("Casa de Don Blas", "Zaguán, patios, sala de tertulias", new Vector3(0, 0, 2.4f), new Vector3(0, 0, -4.4f));
            s.AddLeaf(e, new Vector3(-1.6f, 0, -0.3f), 1.6f, 3.4f, 0.15f, false, Palette.Hex(0x5a3a1e));
            s.AddLeaf(e, new Vector3(1.6f, 0, -0.3f), 1.6f, 3.4f, 0.15f, true, Palette.Hex(0x5a3a1e));
            // outer walls of the whole lot
            WallZ(g, -72f, -0.6f, -12.7f, 0f, H, 0.6f, wall);
            WallZ(g, -72f, -0.6f, 12.7f, 0f, H, 0.6f, wall);
            WallX(g, -13f, 13f, -72f, 0f, 3f, 0.6f, wall);
            // ---- zaguán and front rooms ----
            WallZ(g, -6f, -0.6f, -1.6f, 0f, H, 0.3f, wall, R(-3.6f, -2.4f, 0f, 2.4f));
            WallZ(g, -6f, -0.6f, 1.6f, 0f, H, 0.3f, wall, R(-3.6f, -2.4f, 0f, 2.4f));
            WallX(g, -12.4f, -1.45f, -6f, 0f, H, 0.4f, wall);
            WallX(g, 1.45f, 12.4f, -6f, 0f, H, 0.4f, wall);
            g.BoxMM(new Vector3(-12.4f, H - 0.3f, -6.2f), new Vector3(12.4f, H, -0.6f), ceil, F.Top | F.Bottom);
            Props.Ceiling(d, -12.4f, 12.4f, -6.2f, -0.6f, H - 0.31f, ceil, CityBuilder.WoodDark);
            Props.Floor(g, -12.4f, 12.4f, -6.2f, -0.6f, 0.04f, Props.FloorTile);
            // iron cancel (open) at the end of the zaguán
            foreach (var sx in new[] { -1f, 1f }) { d.Push(new Vector3(sx * 1.45f, 0, -5.7f), sx * 80f); for (int k = 0; k <= 5; k++) d.BoxMM(new Vector3(-sx * k * 0.27f - 0.02f, 0.04f, -0.02f), new Vector3(-sx * k * 0.27f + 0.02f, 2.6f, 0.02f), Iron, F.Front | F.Back | F.Left | F.Right); d.BoxMM(new Vector3(Mathf.Min(0, -sx * 1.4f), 2.5f, -0.03f), new Vector3(Mathf.Max(0, -sx * 1.4f), 2.6f, 0.03f), Iron, F.All); d.Pop(); }
            // tienda (left)
            d.BoxMM(new Vector3(-11f, 0.04f, -3.8f), new Vector3(-4f, 1.0f, -3.1f), CityBuilder.Wood, F.All & ~F.Bottom);
            Props.Shelf(d, new Vector3(-7.5f, 0.04f, -0.7f), 180, 6f, 2.8f, 4, (l, k) => Palette.Hex(new[] { 0x8a2d2d, 0x2f4f7a, 0xd9c08a, 0x3e6b4a, 0xe6dccb, 0x6b3a6b }[(k + l * 2) % 6]));
            for (int i = 0; i < 4; i++) d.Push(new Vector3(-10f + i * 1.6f, 1.0f, -3.45f), 0, 0, 90).Cyl(new Vector3(0, -0.5f, 0), 0.16f, 0.16f, 1.0f, 6, Palette.Hex(new[] { 0x8a2d2d, 0x2f4f7a, 0xd9c08a, 0x3e6b4a }[i]), true, true);
            for (int i = 0; i < 4; i++) d.Pop();
            Props.Barrel(d, new Vector3(-11.6f, 0.04f, -5.3f)); Props.Sack(d, new Vector3(-10.6f, 0.04f, -5.4f), Palette.Hex(0xc9b38a));
            // escritorio (right)
            Props.Table(d, new Vector3(7f, 0.04f, -3.5f), 0, 2f, 1f, CityBuilder.Wood);
            Props.Papers(d, new Vector3(7f, 0.82f, -3.5f), r); Props.Candle(d, new Vector3(7.7f, 0.82f, -3.3f));
            Props.Chair(d, new Vector3(7f, 0.04f, -4.5f), 0, Props.Velvet);
            Props.Shelf(d, new Vector3(11.9f, 0.04f, -3.4f), -90, 4.2f, 2.6f, 5, (l, k) => k % 5 == 4 ? new Color32(0, 0, 0, 0) : Palette.Hex(k % 2 == 0 ? 0x5a2b1a : 0x3a2418));
            s.Light(new Vector3(-7.5f, 3.4f, -3.4f), new Color(1f, 0.86f, 0.66f), 8f, 0.9f);
            s.Light(new Vector3(7f, 3.4f, -3.4f), new Color(1f, 0.86f, 0.66f), 8f, 0.8f);

            // ---- first patio with aljibe; sala (left) and bedroom (right) ----
            Props.Checker(g, -4.8f, 4.8f, -24f, -6.2f, 0.04f, 0.9f, Palette.Hex(0xe8e0cf), Palette.Hex(0x3a3634));
            Props.Aljibe(d, new Vector3(0, 0.04f, -15f));
            foreach (var z in new[] { -8f, -22f }) foreach (var sx in new[] { -1f, 1f }) Props.Pot(d, new Vector3(sx * 4f, 0.04f, z), 1.1f);
            WallZ(g, -24f, -6.2f, -4.8f, 0f, H, 0.4f, wall, R(-16.2f, -14.2f, 0f, 2.7f), R(-11f, -9.6f, 1f, 2.5f), R(-20.4f, -19f, 1f, 2.5f));
            WallZ(g, -24f, -6.2f, 4.8f, 0f, H, 0.4f, wall, R(-16.2f, -14.2f, 0f, 2.7f), R(-11f, -9.6f, 1f, 2.5f));
            foreach (var sx in new[] { -1f, 1f })
            {
                d.Push(new Vector3(sx * 4.6f, 0, 0), sx > 0 ? -90f : 90f);
                CityBuilder.Reja(d, 10.3f * -sx, 1f, 1.4f, 1.5f); if (sx < 0) CityBuilder.Reja(d, 19.7f * -sx, 1f, 1.4f, 1.5f);
                d.Pop();
                // eaves on wooden posts along the patio
                for (float z = -7f; z >= -23f; z -= 4f) d.Cyl(new Vector3(sx * 3.4f, 0.04f, z), 0.1f, 0.1f, 3.1f, 6, CityBuilder.Wood, false);
                d.Quad(new Vector3(sx * 4.6f, 3.6f, -24f), new Vector3(sx * 4.6f, 3.6f, -6.2f), new Vector3(sx * 3.1f, 3.1f, -6.2f), new Vector3(sx * 3.1f, 3.1f, -24f), sx > 0 ? Tile : CityBuilder.TileDark);
                d.Quad(new Vector3(sx * 3.1f, 3.1f, -24f), new Vector3(sx * 3.1f, 3.1f, -6.2f), new Vector3(sx * 4.6f, 3.6f, -6.2f), new Vector3(sx * 4.6f, 3.6f, -24f), Palette.Hex(0x7a5a3a));
            }
            // sala
            Props.Checker(g, -12.4f, -5f, -24f, -6.2f, 0.04f, 1.2f, Props.FloorTile, Props.FloorTile2);
            d.BoxMM(new Vector3(-12.4f, 0.04f, -14f), new Vector3(-9.4f, 0.34f, -6.4f), CityBuilder.Wood, F.All & ~F.Bottom);   // estrado
            Props.Rugs(d, new Vector3(-10.9f, 0.34f, -10.2f), 2.8f, 7.4f, Props.Rug, Props.Rug2);
            for (int i = 0; i < 5; i++) d.Box(new Vector3(-11.8f, 0.48f, -7.2f - i * 1.5f), new Vector3(0.7f, 0.25f, 0.7f), i % 2 == 0 ? Props.Velvet : Palette.Hex(0x2f4f7a), F.All & ~F.Bottom);
            Props.Rugs(d, new Vector3(-7.6f, 0.04f, -17f), 4f, 6f, Palette.Hex(0x2f4f7a), Props.Rug2);
            Props.Bench(d, new Vector3(-11.8f, 0.04f, -18f), 90, 3f);
            for (int i = 0; i < 4; i++) Props.Chair(d, new Vector3(-6.2f, 0.04f, -15.2f - i * 1.2f), -90, Props.Velvet);
            Props.Table(d, new Vector3(-8.9f, 0.04f, -17f), 0, 1.2f, 1.2f, CityBuilder.Wood, 0.6f);
            Props.Candelabra(d, new Vector3(-8.9f, 0.64f, -17f));
            d.Sphere(new Vector3(-8.6f, 0.7f, -16.7f), new Vector3(0.07f, 0.08f, 0.07f), 6, 3, Palette.Hex(0x7a5a2a));     // mate
            d.Lathe(new Vector3(-9.2f, 0.64f, -16.8f), new[] { new Vector2(0f, 0f), new Vector2(0.1f, 0f), new Vector2(0.12f, 0.2f), new Vector2(0.05f, 0.28f), new Vector2(0f, 0.28f) }, 6, Palette.Hex(0xb8b8b8)); // kettle
            // clavichord
            d.BoxMM(new Vector3(-12.2f, 0.75f, -22.6f), new Vector3(-10.4f, 0.95f, -21.8f), Palette.Hex(0x3a2418), F.All);
            for (int k = 0; k < 4; k++) d.Box(new Vector3(-12f + k * 0.5f, 0.38f, -22.2f), new Vector3(0.06f, 0.74f, 0.06f), CityBuilder.WoodDark);
            d.BoxMM(new Vector3(-12.1f, 0.95f, -21.85f), new Vector3(-10.5f, 0.97f, -21.75f), Props.Linen, F.Top);
            Props.Painting(d, new Vector3(-8.7f, 3f, -23.75f), 0, 1.3f, 1.6f, Props.Canvas, Palette.Hex(0x2f3d5c));
            Props.Painting(d, new Vector3(-12.35f, 3f, -17f), 90, 1.0f, 1.4f, Palette.Hex(0xb8c4c8), Palette.Hex(0xb8c4c8));   // mirror
            s.Light(new Vector3(-8.6f, 3.2f, -15f), new Color(1f, 0.82f, 0.6f), 10f, 1.4f, true);
            // dormitorio
            Props.Checker(g, 5f, 12.4f, -24f, -6.2f, 0.04f, 1.2f, Props.FloorTile, Props.FloorTile2);
            Props.Bed(d, new Vector3(10.6f, 0.04f, -19f), 90);
            d.BoxMM(new Vector3(6f, 0.04f, -23.6f), new Vector3(8f, 0.8f, -22.8f), CityBuilder.WoodDark, F.All & ~F.Bottom);   // baúl
            Props.Cross(d, new Vector3(12.3f, 2.4f, -19f), 0.8f, CityBuilder.WoodDark);
            Props.Table(d, new Vector3(11.4f, 0.04f, -9f), 90, 1f, 0.6f, CityBuilder.Wood);
            d.Lathe(new Vector3(11.4f, 0.82f, -9f), new[] { new Vector2(0f, 0f), new Vector2(0.2f, 0f), new Vector2(0.3f, 0.12f), new Vector2(0f, 0.12f) }, 8, Props.Linen);
            s.Light(new Vector3(9f, 3.2f, -15f), new Color(1f, 0.85f, 0.65f), 9f, 0.8f);
            // roof slabs and ceilings of the patio rooms
            foreach (var (x0, x1) in new[] { (-12.4f, -4.6f), (4.6f, 12.4f) })
            {
                g.BoxMM(new Vector3(x0, H - 0.3f, -24.2f), new Vector3(x1, H, -6f), ceil, F.Top | F.Bottom);
                Props.Ceiling(d, x0, x1, -24.2f, -6f, H - 0.31f, ceil, CityBuilder.WoodDark, false);
            }

            // ---- comedor ----
            WallX(g, -12.4f, 12.4f, -24.2f, 0f, H, 0.4f, wall, A(-1.4f, 1.4f, 0f, 3.1f));
            WallX(g, -12.4f, 12.4f, -32f, 0f, H, 0.4f, wall, R(-1.2f, 1.2f, 0f, 2.7f));
            Props.Checker(g, -12.4f, 12.4f, -31.8f, -24.4f, 0.04f, 1.2f, Props.FloorTile, Props.FloorTile2);
            g.BoxMM(new Vector3(-12.4f, H - 0.3f, -32.2f), new Vector3(12.4f, H, -24f), ceil, F.Top | F.Bottom);
            Props.Ceiling(d, -12.4f, 12.4f, -32.2f, -24f, H - 0.31f, ceil, CityBuilder.WoodDark);
            Props.Table(d, new Vector3(-6f, 0.04f, -28.2f), 0, 5f, 1.4f, Props.Linen, 0.78f, true);
            for (int i = 0; i < 4; i++) { Props.Chair(d, new Vector3(-7.9f + i * 1.25f, 0.04f, -29.4f), 0, CityBuilder.Wood); Props.Chair(d, new Vector3(-7.9f + i * 1.25f, 0.04f, -27f), 180, CityBuilder.Wood); }
            for (int i = 0; i < 5; i++) d.Cyl(new Vector3(-7.8f + i * 0.9f, 0.82f, -28.2f), 0.14f, 0.14f, 0.02f, 8, Props.Linen, true);
            Props.Shelf(d, new Vector3(8f, 0.04f, -31.7f), 0, 3.6f, 2.4f, 4, (l, k) => (k % 2 == 0) ? Palette.Hex(0xe8e8f0) : Palette.Hex(0x3a5a9a));
            s.Light(new Vector3(-4f, 3.4f, -28f), new Color(1f, 0.85f, 0.65f), 11f, 1.1f, true);

            // ---- second patio: grape arbour, kitchen, servants' room ----
            Props.Floor(g, -12.4f, 12.4f, -50f, -32.2f, 0.03f, Props.BrickFloor);
            for (float x = -4f; x <= 4f; x += 4f) for (float z = -34f; z >= -46f; z -= 4f) d.Cyl(new Vector3(x, 0, z), 0.08f, 0.08f, 2.8f, 5, CityBuilder.Wood, false);
            for (float z = -34f; z >= -46f; z -= 1f) d.BoxMM(new Vector3(-4.2f, 2.8f, z - 0.04f), new Vector3(4.2f, 2.86f, z + 0.04f), CityBuilder.Wood, F.All);
            for (int i = 0; i < 26; i++) d.Sphere(new Vector3(r.Range(-4f, 4f), 2.95f + r.Range(0f, 0.2f), r.Range(-46f, -34f)), new Vector3(0.7f, 0.25f, 0.7f), 5, 2, i % 3 == 0 ? CityBuilder.LeafDark : CityBuilder.Leaf);
            for (int i = 0; i < 8; i++) d.Sphere(new Vector3(r.Range(-3.5f, 3.5f), 2.65f, r.Range(-45f, -35f)), new Vector3(0.08f, 0.14f, 0.08f), 5, 2, Palette.Hex(0x4a2a5a));
            CityBuilder.Tree(d, new Vector3(8.5f, 0, -46f), 0.9f, new Rng(77));
            Props.Bench(d, new Vector3(0, 0.03f, -40f), 90, 2.4f);
            // kitchen
            WallZ(g, -42f, -32.2f, -6f, 0f, 3.6f, 0.4f, wall, R(-38f, -36.6f, 0f, 2.4f));
            WallX(g, -12.4f, -5.8f, -42f, 0f, 3.6f, 0.4f, wall);
            g.Push(new Vector3(-9.2f, 3.6f, -37f), 90f); g.GableRoof(Vector3.zero, 10.6f, 7.6f, 1.6f, Tile, wall); g.Pop();
            d.Push(new Vector3(-9.2f, 3.55f, -37f), 90f); InnerGable(d, Vector3.zero, 10.4f, 7.2f, 1.55f, Palette.Hex(0x5a4330)); d.Pop();
            d.BoxMM(new Vector3(-12.4f, 0.03f, -41.6f), new Vector3(-9.4f, 0.9f, -39.8f), CityBuilder.Brick, F.All & ~F.Bottom);   // fogón
            d.BoxMM(new Vector3(-11.4f, 0.1f, -39.79f), new Vector3(-10.4f, 0.6f, -39.77f), Palette.Hex(0xff7a2a), F.Front);
            d.BoxMM(new Vector3(-12.4f, 2.2f, -41.6f), new Vector3(-9.4f, 5.6f, -40.4f), CityBuilder.Brick, F.All);
            foreach (var x in new[] { -11.6f, -10.3f }) d.Lathe(new Vector3(x, 0.9f, -40.7f), new[] { new Vector2(0f, 0f), new Vector2(0.25f, 0f), new Vector2(0.32f, 0.28f), new Vector2(0.24f, 0.42f), new Vector2(0f, 0.42f) }, 8, Palette.Hex(0x3a3634));
            Props.Table(d, new Vector3(-8.6f, 0.03f, -35f), 90, 2f, 1f, CityBuilder.Wood);
            for (int i = 0; i < 6; i++) d.Sphere(new Vector3(-8.9f + (i % 3) * 0.3f, 0.86f, -35.4f + (i / 3) * 0.5f), Vector3.one * 0.1f, 5, 3, i % 2 == 0 ? Palette.Hex(0xd8a04a) : Palette.Hex(0xe58a2a));
            s.Light(new Vector3(-10f, 1.6f, -39f), new Color(1f, 0.6f, 0.3f), 7f, 1.6f, true);
            // servants' room (closed)
            WallZ(g, -42f, -32.2f, 6f, 0f, 3.6f, 0.4f, wall, R(-38f, -36.6f, 0f, 2.4f));
            d.BoxMM(new Vector3(5.95f, 0.03f, -38f), new Vector3(6.05f, 2.4f, -36.6f), CityBuilder.WoodDark, F.Left | F.Right);
            WallX(g, 5.8f, 12.4f, -42f, 0f, 3.6f, 0.4f, wall);
            g.Push(new Vector3(9.2f, 3.6f, -37f), 90f); g.GableRoof(Vector3.zero, 10.6f, 7.6f, 1.6f, Tile, wall); g.Pop();
            s.blockers.Add((new Vector3(9.2f, 1.8f, -37f), new Vector3(6f, 3.6f, 9.6f)));
            // ---- huerta ----
            WallX(g, -12.4f, 12.4f, -50f, 0f, 1.5f, 0.4f, wall, R(-1f, 1f, 0f, 1.5f));
            Props.Floor(d, -12.4f, 12.4f, -71.7f, -50.2f, 0.03f, Palette.Hex(0x6f7a3a));
            for (int i = 0; i < 6; i++) CityBuilder.Tree(d, new Vector3(-9f + (i % 3) * 9f, 0, -55f - (i / 3) * 9f), 0.75f, new Rng((uint)(400 + i)));
            for (int row = 0; row < 4; row++) for (int k = 0; k < 10; k++) d.Sphere(new Vector3(-10f + k * 0.9f, 0.15f, -66f - row * 1.2f), new Vector3(0.3f, 0.2f, 0.3f), 5, 2, CityBuilder.Leaf);
            d.Cyl(new Vector3(8f, 0.03f, -66f), 0.8f, 0.8f, 0.8f, 8, CityBuilder.Brick); d.Cyl(new Vector3(8f, 0.83f, -66f), 0.6f, 0.6f, 0.01f, 8, Palette.Hex(0x1a1d1e));
            d.BoxMM(new Vector3(9.5f, 0.03f, -71.6f), new Vector3(12.3f, 1.6f, -69f), CityBuilder.Wood, F.All & ~F.Bottom);

            // ---- people ----
            string[] tertulia = {
                "Bienvenidos a esta humilde casa. ¿Un mate, señores?",
                "Hoy en la plaza no se hablaba de otra cosa que del Cabildo.",
                "Dicen que Cisneros ya firmó su renuncia.",
                "Los Patricios estamos con el pueblo, señora. Saavedra dio su palabra.",
                "¿Y qué será de nosotros si España se recupera?",
                "Seguiremos comerciando, doña. Pero con gobierno propio, que es lo justo." };
            s.Actor(new Vector3(-7.6f, 0.04f, -15.4f), -150, Outfit.Levita, "Don Blas", 1, tertulia, false, "none", "none");
            s.Actor(new Vector3(-6.2f, 0.04f, -15.2f), -90, Outfit.Dama, "Doña Mercedes", 1, null, true, "none", "peineta");
            s.Actor(new Vector3(-6.2f, 0.04f, -16.4f), -90, Outfit.Caballero, "Don Ignacio", 1, null, true, "none", "none");
            s.Actor(new Vector3(-9.6f, 0.04f, -18.6f), 40, Outfit.Patricio, "Teniente de Patricios", 1, null, false, "none", "bi");
            s.Actor(new Vector3(-6.2f, 0.04f, -17.6f), -90, Outfit.Dama, "Doña Pepa", 1, null, true, "none", "peineta");
            s.Actor(new Vector3(-10.9f, 0.34f, -9f), 90, Outfit.Dama, "Niña Encarnación", 2, new[] { "Mamá dice que las señoritas no hablan de política…", "¡Pero yo quiero saber de qué se trata!" }, true, "none", "peineta");
            s.Actor(new Vector3(-8.2f, 0.04f, -19.8f), 20, Outfit.Criolla, "Criada", 3, new[] { "¿Otro mate, su merced?", "Recién cebado, bien calentito.", "En la cocina hay pastelitos para la tertulia." }, false, "mate", "panuelo");
            s.Actor(new Vector3(-8.6f, 0.03f, -37.2f), -90, Outfit.Vendedora, "Cocinera", 4, new[] { "¡Que nadie toque el puchero, que todavía no está!", "Para la tertulia preparé pastelitos de membrillo.", "Dicen que en la plaza reparten cintas, ¿será cierto?" });
            s.Actor(new Vector3(-7.5f, 0.04f, -2.5f), 180, Outfit.Peon, "Dependiente", 5, new[] { "Tenemos paños de Castilla y bayeta de la tierra.", "Don Blas dice que pronto podremos comerciar sin tanto trámite.", "Pase, pase, que hoy hay buen precio." });
            s.Actor(new Vector3(7f, 0.04f, -4.5f), 180, Outfit.Levita, "Tenedor de libros", 5, null, true, "none", "none");

            s.Interior(new Vector3(-12.7f, -1f, -72f), new Vector3(12.7f, 9f, -0.6f));
            s.Finish(mat, "DonBlas");
            return s;
        }
    }
}
