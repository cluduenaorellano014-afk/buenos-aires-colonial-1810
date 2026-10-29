using System.Collections.Generic;
using UnityEngine;

namespace Colonial
{
    // Everybody in the city: walkers on the street grid, vendors crying their goods, groups talking about
    // the revolution, and the people inside the landmarks. One manager updates them all, and people far
    // from the player are neither animated nor drawn.
    public class Crowd : MonoBehaviour
    {
        public class Agent
        {
            public GameObject go; public Transform t; public PersonRig rig; public SkinnedMeshRenderer smr;
            public string name; public string[] lines; public bool shout;
            public Talk group; public Site site;
            public int kind;      // 0 walker, 1 static vendor, 2 group member, 3 site actor, 4 plaza wanderer
            public Vector2Int a, b; public float tPath, speed, offA, offB;
            public Vector3 target; public float wait;
            public float nextSay, dist; public string bubble; public float bubbleUntil;
            public bool visible = true; public int lastLine = -1;
        }
        public class Talk { public List<Agent> members = new List<Agent>(); public string[] lines; public int idx; public float next; public Site site; public bool loop = true; }

        public readonly List<Agent> agents = new List<Agent>();
        readonly List<Talk> talks = new List<Talk>();
        Material mat;
        Transform player;
        Rng rng = new Rng(525);
        float clock;
        int frame;
        bool mobile;
        static readonly string[] Women = { "Doña Mercedes", "Doña Remedios", "Doña Juana", "Doña Petrona", "Misia Pepa", "Doña Encarnación", "Doña Tomasa", "Doña Casilda", "Doña Pascuala" };
        static readonly string[] Men = { "Don Ramón", "Don Ignacio", "Don Matías", "Don Tomás", "Don Juan José", "Don Esteban", "Fray Cayetano", "Don Pedro", "Don Bernardo", "Don Anselmo" };
        static readonly string[] Chants = { "¡El pueblo quiere saber de qué se trata!", "¡Cabildo abierto!", "¡Que renuncie Cisneros!", "¡Viva la Junta!", "¡Queremos saber!", "¡Que salgan al balcón!" };

        public void Init(Material palette, Transform playerT, bool isMobile)
        {
            mat = palette; player = playerT; mobile = isMobile;
        }

        GameObject MakePerson(PersonSpec spec, string name)
        {
            var go = PersonFactory.Build(spec, mat, name);
            go.transform.SetParent(transform, false);
            go.layer = 2;   // Ignore Raycast: speech bubbles test walls only
            var cc = go.AddComponent<CapsuleCollider>(); cc.center = new Vector3(0, 0.9f, 0); cc.height = 1.75f; cc.radius = 0.28f;
            return go;
        }

        Agent Add(Outfit o, string name, Vector3 pos, float yaw, int kind, string item = null, string hat = null)
        {
            var spec = PersonSpec.Random(rng, o);
            if (item != null) spec.item = item;
            if (hat != null) spec.hat = hat;
            var go = MakePerson(spec, name);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var a = new Agent { go = go, t = go.transform, rig = go.GetComponent<PersonRig>(), smr = go.GetComponentInChildren<SkinnedMeshRenderer>(), name = name, kind = kind, nextSay = rng.Range(2f, 14f) };
            agents.Add(a);
            return a;
        }

        static Outfit RandomOutfit(Rng r, bool woman)
        {
            if (woman) { float x = r.Next(); return x < 0.45f ? Outfit.Dama : x < 0.85f ? Outfit.Criolla : Outfit.Vendedora; }
            float y = r.Next();
            return y < 0.35f ? Outfit.Caballero : y < 0.55f ? Outfit.Levita : y < 0.7f ? Outfit.Gaucho : y < 0.82f ? Outfit.Peon : y < 0.92f ? Outfit.Patricio : Outfit.Fraile;
        }

        // ------------------------------------------------------------------ population
        public void Populate(List<Site> sites)
        {
            int walkers = mobile ? 28 : 60;
            for (int i = 0; i < walkers; i++)
            {
                bool woman = rng.Chance(0.45f);
                var o = RandomOutfit(rng, woman);
                var a = Add(o, woman ? rng.Pick(Women) : (o == Outfit.Fraile ? "Fray " + rng.Pick(new[] { "Cayetano", "Julián", "Pedro" }) : o == Outfit.Patricio ? "Soldado de Patricios" : rng.Pick(Men)), Vector3.zero, 0, 0,
                    o == Outfit.Patricio ? "musket" : rng.Chance(0.2f) ? "umbrella" : null);
                StartWalk(a);
                a.lines = City.WalkerLines;
            }
            // walking vendors
            foreach (var v in City.Vendors)
            {
                var a = Add(v.woman ? Outfit.Vendedora : Outfit.Peon, v.name, Vector3.zero, 0, 0, MapItem(v.item));
                a.lines = v.lines; a.shout = true;
                StartWalk(a); a.speed = 0.8f;
            }
            // vendors standing at corners and in the plaza (map coordinates from the HTML map)
            var spots = new (int v, float x, float z)[] { (0, 640, 404), (2, 700, 388), (3, 760, 430), (5, 600, 430), (1, 566, 372), (6, 902, 380), (4, 298, 566) };
            foreach (var (vi, x, z) in spots)
            {
                var v = City.Vendors[vi];
                var a = Add(v.woman ? Outfit.Vendedora : Outfit.Peon, v.name, City.W2U(x, z), rng.Range(0, 360), 1, MapItem(v.item));
                a.lines = v.lines; a.shout = true; a.nextSay = rng.Range(1f, 6f);
            }
            // conversation groups in the streets (same spots as the HTML map)
            var groups = new[] { (592, 384), (620, 420), (650, 366), (722, 402), (760, 414), (600, 356), (284, 566), (184, 231), (460, 342), (350, 454), (574, 560), (796, 230), (640, 440), (740, 375) };
            int gi = 0;
            foreach (var (gx, gz) in groups)
            {
                var c = City.W2U(gx, gz);
                var talk = new Talk { lines = City.Dialogs[gi++ % City.Dialogs.Count], next = rng.Range(1f, 5f) };
                int n = rng.Int(2, 3);
                for (int k = 0; k < n; k++)
                {
                    float ang = k * Mathf.PI * 2 / n + rng.Range(-0.3f, 0.3f);
                    var p = c + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * 0.95f;
                    bool woman = rng.Chance(0.45f);
                    var a = Add(RandomOutfit(rng, woman), woman ? rng.Pick(Women) : rng.Pick(Men), p, 0, 2, rng.Chance(0.25f) ? "umbrella" : null);
                    a.t.rotation = Quaternion.LookRotation(c - p);
                    a.group = talk; talk.members.Add(a);
                }
                talks.Add(talk);
            }
            // the crowd waiting in front of the Cabildo, 25 May 1810
            for (int i = 0; i < (mobile ? 12 : 22); i++)
            {
                var p = City.W2U(rng.Range(576f, 622f), rng.Range(374f, 424f));
                bool woman = rng.Chance(0.35f);
                var a = Add(RandomOutfit(rng, woman), woman ? rng.Pick(Women) : rng.Pick(Men), p, 0, 1, rng.Chance(0.45f) ? "umbrella" : null);
                var look = City.W2U(560, 398) - p; look.y = 0;
                a.t.rotation = Quaternion.LookRotation(look) * Quaternion.Euler(0, rng.Range(-25f, 25f), 0);
                a.lines = Chants; a.shout = true; a.nextSay = rng.Range(3f, 25f);
            }
            // people wandering around the two plazas
            for (int i = 0; i < (mobile ? 6 : 14); i++)
            {
                bool woman = rng.Chance(0.45f);
                var a = Add(RandomOutfit(rng, woman), woman ? rng.Pick(Women) : rng.Pick(Men), PlazaPoint(), 0, 4, rng.Chance(0.2f) ? "basket" : null);
                a.target = PlazaPoint(); a.speed = rng.Range(0.9f, 1.3f); a.lines = City.WalkerLines;
            }
            // the people inside the landmarks
            foreach (var s in sites)
            {
                var bySite = new Dictionary<int, Talk>();
                foreach (var sp in s.actors)
                {
                    var a = Add(sp.outfit, sp.name, sp.pos, sp.yaw, 3, sp.item, sp.hat);
                    a.site = s;
                    if (sp.seated) a.rig.seated = true;
                    if (sp.group >= 0)
                    {
                        if (!bySite.TryGetValue(sp.group, out var talk)) { talk = new Talk { site = s, next = rng.Range(1f, 4f) }; bySite[sp.group] = talk; talks.Add(talk); }
                        if (sp.lines != null) talk.lines = sp.lines;
                        a.group = talk; talk.members.Add(a);
                    }
                    else if (sp.lines != null) a.lines = sp.lines;
                }
            }
        }

        static string MapItem(string it) => it switch { "olla" => "olla", "candles" => "candles", "tray" => "tray", "barrelcart" => "none", "brooms" => "brooms", "basket" => "basket", "fish" => "fish", _ => "none" };

        Vector3 PlazaPoint()
        {
            // both halves of the plaza, avoiding the Recova in the middle and the market stalls
            bool west = rng.Chance(0.6f);
            float x = west ? rng.Range(578f, 666f) : rng.Range(690f, 780f);
            float z = rng.Range(352f, 444f);
            return City.W2U(x, z);
        }

        // ------------------------------------------------------------------ walking on the street grid
        static bool EdgeOK(Vector2Int a, Vector2Int b) => !(a.x == 6 && b.x == 6 && ((a.y == 3 && b.y == 4) || (a.y == 4 && b.y == 3)));
        List<Vector2Int> Neigh(Vector2Int n)
        {
            var l = new List<Vector2Int>(4);
            foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
            {
                var m = n + d;
                if (m.x >= 0 && m.x <= City.COLS && m.y >= 0 && m.y <= City.ROWS && EdgeOK(n, m)) l.Add(m);
            }
            return l;
        }
        void StartWalk(Agent a)
        {
            a.a = new Vector2Int(rng.Int(0, City.COLS), rng.Int(0, City.ROWS));
            a.b = rng.Pick(Neigh(a.a));
            a.tPath = rng.Next();
            a.offA = (rng.Chance(0.5f) ? -1 : 1) * rng.Range(3.3f, 4.6f);
            a.offB = (rng.Chance(0.5f) ? -1 : 1) * rng.Range(3.3f, 4.6f);
            a.speed = rng.Range(1.1f, 1.6f);
            a.t.position = WalkPos(a);
        }
        Vector3 Node(Vector2Int n) => City.W2U(City.LineC(n.x), City.LineC(n.y));
        Vector3 WalkPos(Agent a)
        {
            Vector3 pa = Node(a.a), pb = Node(a.b);
            var dir = (pb - pa).normalized; var side = new Vector3(dir.z, 0, -dir.x);
            return Vector3.Lerp(pa, pb, a.tPath) + side * a.offA;
        }

        // ------------------------------------------------------------------ update
        public bool InsideAny(List<Site> sites, Vector3 p, out Site where)
        {
            foreach (var s in sites) if (s.IsInside(p)) { where = s; return true; }
            where = null; return false;
        }

        public Site playerSite;   // set by the game each frame

        void Update()
        {
            if (player == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            clock += dt; frame++;
            Vector3 pp = player.position;
            float drawDist = mobile ? 95f : 150f, animDist = mobile ? 45f : 70f;

            foreach (var a in agents)
            {
                // movement
                if (a.kind == 0)
                {
                    var before = a.t.position;
                    bool blocked = (before - pp).sqrMagnitude < 1.4f;
                    if (!blocked)
                    {
                        float len = Vector3.Distance(Node(a.a), Node(a.b));
                        a.tPath += a.speed * dt / len;
                        if (a.tPath >= 1f)
                        {
                            a.tPath -= 1f;
                            var prev = a.a; a.a = a.b;
                            var nb = Neigh(a.a); if (nb.Count > 1) nb.RemoveAll(m => m == prev);
                            a.b = rng.Pick(nb);
                        }
                    }
                    var np = WalkPos(a);
                    var mv = np - before;
                    a.t.position = np;
                    if (mv.sqrMagnitude > 1e-6f) a.t.rotation = Quaternion.Slerp(a.t.rotation, Quaternion.LookRotation(mv), dt * 6f);
                    a.rig.speed = blocked ? 0f : a.speed;
                }
                else if (a.kind == 4)
                {
                    if (a.wait > 0f) { a.wait -= dt; a.rig.speed = 0f; }
                    else
                    {
                        var d = a.target - a.t.position; d.y = 0;
                        if (d.magnitude < 0.5f) { a.target = PlazaPoint(); a.wait = rng.Range(2f, 9f); }
                        else
                        {
                            bool blocked = (a.t.position - pp).sqrMagnitude < 1.4f;
                            if (!blocked) a.t.position += d.normalized * a.speed * dt;
                            a.t.rotation = Quaternion.Slerp(a.t.rotation, Quaternion.LookRotation(d), dt * 5f);
                            a.rig.speed = blocked ? 0f : a.speed;
                        }
                    }
                }

                // culling: inside a building you only see that building's people, outside only the street's
                a.dist = Vector3.Distance(a.t.position, pp);
                bool sameSpace = a.site == null ? (playerSite == null || a.dist < 30f) : (a.site == playerSite || (playerSite == null && a.dist < 35f));
                bool vis = a.dist < drawDist && sameSpace;
                if (vis != a.visible) { a.visible = vis; a.smr.enabled = vis; }
                if (!vis) continue;
                if (a.dist < animDist || (frame + a.GetHashCode()) % 4 == 0)
                {
                    a.rig.talking = a.bubbleUntil > clock;
                    a.rig.Tick(a.dist < animDist ? dt : dt * 4f);
                }

                // solo remarks
                if (a.group == null && a.lines != null && a.lines.Length > 0 && a.dist < (a.shout ? 26f : 14f))
                {
                    a.nextSay -= dt;
                    if (a.nextSay <= 0f)
                    {
                        Say(a, a.lines[rng.Int(0, a.lines.Length - 1)]);
                        a.nextSay = a.shout ? rng.Range(6f, 13f) : rng.Range(10f, 26f);
                    }
                }
            }

            // conversations: one speaker at a time, cycling through the script
            foreach (var g in talks)
            {
                if (g.lines == null || g.members.Count == 0) continue;
                var first = g.members[0];
                if (first.dist > 30f || !first.visible) continue;
                if (clock < g.next) continue;
                var who = g.members[g.idx % g.members.Count];
                string line = g.lines[g.idx % g.lines.Length];
                Say(who, line);
                foreach (var m in g.members) if (m != who) { var look = who.t.position - m.t.position; look.y = 0; if (look.sqrMagnitude > 0.01f && !m.rig.seated) m.t.rotation = Quaternion.Slerp(m.t.rotation, Quaternion.LookRotation(look), 0.5f); }
                g.idx++;
                g.next = clock + LineTime(line) + (g.idx % g.lines.Length == 0 ? rng.Range(5f, 10f) : 0.35f);
            }
        }

        static float LineTime(string s) => 2.2f + s.Length * 0.055f;
        void Say(Agent a, string s) { a.bubble = s; a.bubbleUntil = clock + LineTime(s); a.rig.gestureBoost = 1.5f; }

        // the HUD asks for the people who are talking right now
        public void Speaking(List<Agent> into)
        {
            into.Clear();
            foreach (var a in agents) if (a.visible && a.bubbleUntil > clock && a.dist < 30f) into.Add(a);
            into.Sort((x, y) => x.dist.CompareTo(y.dist));
        }
        public bool IsShout(Agent a) => a.shout;
    }
}
