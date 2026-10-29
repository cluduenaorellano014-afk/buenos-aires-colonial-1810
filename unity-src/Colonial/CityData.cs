using System;
using System.Collections.Generic;
using UnityEngine;

namespace Colonial
{
    // City layout exported from the HTML map (Assets/Resources/ciudad.json) so the 3D city matches
    // the map house by house. Map coordinates: x east, z SOUTH (SVG). Unity: x east, z NORTH.
    [Serializable] public class HouseData
    {
        public float x, z, w, d, nx, nz, len, h, door;
        public string edge, roof, color, family, occ, desc;
        public int floors;
        public float[] wins, upWins;
    }
    [Serializable] public class Rect2 { public float x, z, w, d; }
    [Serializable] public class TP { public float x, z, lx, lz; }
    [Serializable] public class PlaceData { public string name, kicker, desc; public Rect2 foot; public TP tp; }
    [Serializable] public class PlacesData { public PlaceData plaza, cabildo, jaboneria, donblas, catedral, fuerte; }
    [Serializable] public class CityJson
    {
        public float B, S, PITCH, D, W, H;
        public int COLS, ROWS;
        public string[] V_STREETS, H_STREETS;
        public PlacesData places;
        public HouseData[] houses;
    }
    [Serializable] public class VendorData { public string name, item; public bool woman; public string[] lines; }
    [Serializable] public class DialogJson { public List<StringList> dialogs; public string[] walker; public VendorData[] vendors; }
    [Serializable] public class StringList { public string[] l; }

    public static class City
    {
        public static CityJson Data;
        public static List<string[]> Dialogs = new List<string[]>();
        public static string[] WalkerLines;
        public static VendorData[] Vendors;

        public static float B => Data.B;
        public static float S => Data.S;
        public static float PITCH => Data.PITCH;
        public static float D => Data.D;
        public static float W => Data.W;
        public static float H => Data.H;
        public static int COLS => Data.COLS;
        public static int ROWS => Data.ROWS;

        public static float Bx(int i) => S + i * PITCH;
        public static float Bz(int j) => S + j * PITCH;
        public static float LineC(int i) => i * PITCH + S / 2;
        public static bool IsPlazaBlock(int i, int j) => j == 3 && (i == 5 || i == 6);
        public static bool IsFuerteBlock(int i, int j) => i == 7 && j == 3;

        // map (x, z-south) -> Unity world
        public static Vector3 W2U(float mx, float mz, float y = 0f) => new Vector3(mx, y, H - mz);
        public static Vector2 U2Map(Vector3 p) => new Vector2(p.x, H - p.z);

        public static void Load()
        {
            if (Data != null) return;
            Data = JsonUtility.FromJson<CityJson>(Resources.Load<TextAsset>("ciudad").text);
            var dj = JsonUtility.FromJson<DialogJson>(Resources.Load<TextAsset>("dialogos").text);
            WalkerLines = dj.walker;
            Vendors = dj.vendors;
            Dialogs.Clear();
            foreach (var d in dj.dialogs) Dialogs.Add(d.l);
        }

        public static string StreetAt(Vector3 p, out string near)
        {
            near = null;
            Vector2 m = U2Map(p);
            float best = 1e9f; string name = "Buenos Aires";
            for (int i = 0; i <= COLS && i < Data.V_STREETS.Length; i++)
            {
                float d = Mathf.Abs(m.x - LineC(i));
                if (d < best && d < 9f) { best = d; name = Data.V_STREETS[i]; }
            }
            for (int j = 0; j <= ROWS && j < Data.H_STREETS.Length; j++)
            {
                float d = Mathf.Abs(m.y - LineC(j));
                if (d < best && d < 9f) { best = d; name = Data.H_STREETS[j]; }
            }
            return name;
        }

        public static PlaceData Place(string key)
        {
            var p = Data.places;
            switch (key)
            {
                case "plaza": return p.plaza;
                case "cabildo": return p.cabildo;
                case "jaboneria": return p.jaboneria;
                case "donblas": return p.donblas;
                case "catedral": return p.catedral;
                case "fuerte": return p.fuerte;
            }
            return null;
        }
        public static readonly string[] PlaceKeys = { "plaza", "cabildo", "catedral", "jaboneria", "donblas", "fuerte" };
    }

    // deterministic RNG so the city is the same on every device
    public class Rng
    {
        uint s;
        public Rng(uint seed) { s = seed; }
        public float Next() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return (s & 0xFFFFFF) / 16777216f; }
        public float Range(float a, float b) => a + (b - a) * Next();
        public int Int(int a, int bInclusive) => Mathf.Min(bInclusive, a + (int)(Next() * (bInclusive - a + 1)));
        public T Pick<T>(IList<T> l) => l[Mathf.Min(l.Count - 1, (int)(Next() * l.Count))];
        public bool Chance(float p) => Next() < p;
    }
}
