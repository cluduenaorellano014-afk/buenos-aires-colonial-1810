using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Colonial
{
    // Entry point of "Buenos Aires 1810": builds the city, the landmarks and the crowd at startup,
    // places the player (optionally at a landmark requested by the map: ?lugar=cabildo), and runs the
    // door cutscenes, the place labels and fast travel.
    public class ColonialGame : MonoBehaviour
    {
        public Material palette, dirt, grass, water, rain;
        public Light sun;

        readonly List<Site> sites = new List<Site>();
        Crowd crowd;
        ColonialHUD hud;
        Weather weather;
        Ambience ambience;
        Transform player;
        CharacterController cc;
        FirstPersonController fpc;
        Camera cam;
        bool busy, mobile, embedded;
        Entrance promptEntrance; bool promptIn;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int ColonialIsEmbedded();
        [DllImport("__Internal")] static extern void ColonialBackToMap();
#else
        static int ColonialIsEmbedded() => 0;
        static void ColonialBackToMap() { Debug.Log("[Colonial] volver al mapa"); }
#endif

        void Awake()
        {
            mobile = DeviceInfo.IsMobile;
            embedded = ColonialIsEmbedded() == 1;
            WebPerformance.ShadowDistance = mobile ? 32f : 70f;
            QualitySettings.lodBias = mobile ? 0.8f : 1.2f;
            BuildWorld();
            BuildPlayer();
            crowd = new GameObject("Gente").AddComponent<Crowd>();
            crowd.Init(palette, player, mobile);
            crowd.Populate(sites);
            ApplyPalette();   // people add their own colors to the palette

            hud = new GameObject("Interfaz").AddComponent<ColonialHUD>();
            hud.Init(crowd, cam, embedded);
            hud.onAction = () => { if (promptEntrance != null) StartCoroutine(Pass(promptEntrance, promptIn)); };
            hud.onTravel = k => { if (!busy) StartCoroutine(Travel(k, false)); };
            hud.onMap = ColonialBackToMap;

            weather = new GameObject("Clima").AddComponent<Weather>();
            weather.Init(cam, sun, rain, dirt, grass, mobile);
            ambience = new GameObject("Sonido").AddComponent<Ambience>();
            ambience.Init(player, cam, weather, crowd, sites);
            hud.AddToggles(() => weather.Toggle(), weather.Raining, () => ambience.Toggle(), ambience.Enabled);
        }

        // Also called from the editor (ColonialBuilder) to render verification shots without playing.
        public Transform BuildWorld()
        {
            City.Load();
            Palette.Reset();
            sites.Clear();
            var world = new GameObject("Ciudad").transform;
            // landmarks first: their yards must stay free of the blocks' trees
            sites.Add(Landmarks.Cabildo(palette, world));
            sites.Add(Landmarks.Catedral(palette, world));
            sites.Add(Landmarks.Fuerte(palette, water, world));
            sites.Add(Landmarks.Jaboneria(palette, world));
            sites.Add(Landmarks.DonBlas(palette, world));
            Landmarks.Recova(palette, world);
            var cb = new CityBuilder(palette, world);
            foreach (var s in sites) cb.blockedRects.Add(s.mapRect);
            cb.BuildBlocks(mobile);
            cb.BuildGround(dirt, grass, water);
            cb.BuildOutskirts();
            cb.BuildShips();
            BuildMarket(world);
            ApplyPalette();
            GroundTextures();
            Atmosphere();
            return world;
        }

        public List<Site> Sites => sites;

        public void ApplyPalette()
        {
            palette.SetTexture("_BaseMap", Palette.BuildTexture());
        }

        void BuildPlayer()
        {
            var pgo = new GameObject("Jugador");
            pgo.transform.position = City.W2U(650, 414);
            cc = pgo.AddComponent<CharacterController>();
            cc.height = 1.75f; cc.radius = 0.3f; cc.center = new Vector3(0, 0.9f, 0); cc.stepOffset = 0.45f; cc.slopeLimit = 50f; cc.skinWidth = 0.04f;
            cam = Camera.main;
            cam.transform.SetParent(pgo.transform, false);
            cam.transform.localPosition = new Vector3(0, 1.62f, 0);
            cam.transform.localRotation = Quaternion.identity;
            cam.nearClipPlane = 0.1f; cam.farClipPlane = mobile ? 420f : 800f; cam.fieldOfView = 68f;
            fpc = pgo.AddComponent<FirstPersonController>();
            fpc.walkSpeed = 3.0f; fpc.runMultiplier = 2.0f; fpc.headBobAmount = 0.03f; fpc.headBobFrequency = 1.9f;
            player = pgo.transform;
        }

        IEnumerator Start()
        {
            string lugar = null;
            var url = Application.absoluteURL;
            int q = string.IsNullOrEmpty(url) ? -1 : url.IndexOf("lugar=", System.StringComparison.Ordinal);
            if (q >= 0) { lugar = url.Substring(q + 6); int amp = lugar.IndexOfAny(new[] { '&', '#' }); if (amp >= 0) lugar = lugar.Substring(0, amp); }
            if (string.IsNullOrEmpty(lugar)) lugar = "plaza";
            PlaceAt(lugar);
            hud.Fade(false);
            yield return new WaitForSeconds(0.8f);
            var site = sites.Find(s => s.key == lugar);
            if (site != null) StartCoroutine(Pass(site.entrances[0], true));
            else { var p = City.Place(lugar) ?? City.Place("plaza"); hud.ShowTitle(p.name, "Buenos Aires · mayo de 1810"); }
        }

        void PlaceAt(string key)
        {
            var site = sites.Find(s => s.key == key);
            cc.enabled = false;
            if (site != null)
            {
                var e = site.entrances[0];
                player.position = e.outside - Quaternion.Euler(0, e.outsideYaw, 0) * Vector3.forward * 1.5f;
                player.rotation = Quaternion.Euler(0, e.outsideYaw, 0);
            }
            else
            {
                var tp = (City.Place(key) ?? City.Place("plaza")).tp;
                player.position = City.W2U(tp.x, tp.z);
                var look = City.W2U(tp.lx, tp.lz) - player.position;
                player.rotation = Quaternion.LookRotation(new Vector3(look.x, 0, look.z));
            }
            fpc.Pitch = 0f;
            cc.enabled = true;
        }

        IEnumerator Travel(string key, bool enter)
        {
            busy = true;
            FirstPersonController.InputLocked = true;
            hud.Fade(true);
            yield return new WaitUntil(() => hud.FadeDone);
            PlaceAt(key);
            yield return null;
            hud.Fade(false);
            var p = City.Place(key);
            hud.ShowTitle(p.name, p.kicker);
            FirstPersonController.InputLocked = false;
            busy = false;
        }

        // ------------------------------------------------------------------ door cutscene
        IEnumerator Pass(Entrance e, bool goingIn)
        {
            if (busy) yield break;
            busy = true;
            FirstPersonController.InputLocked = true;
            hud.SetPrompt(null, null);
            hud.Cinematic(true);
            cc.enabled = false;
            Vector3 start = goingIn ? e.outside : e.inside, end = goingIn ? e.inside : e.outside;
            float yawTo = goingIn ? e.outsideYaw : e.insideYaw;
            Vector3 from = player.position; float yawFrom = player.eulerAngles.y, pitchFrom = fpc.Pitch;

            // 1) step in front of the door and look at it
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.8f)
            {
                float s = Mathf.SmoothStep(0, 1, t);
                player.position = Vector3.Lerp(from, start, s);
                player.rotation = Quaternion.Euler(0, Mathf.LerpAngle(yawFrom, yawTo, s), 0);
                fpc.Pitch = Mathf.Lerp(pitchFrom, -4f, s);
                yield return null;
            }
            // 2) the doors swing open
            foreach (var l in e.leaves) l.Open(true);
            if (e.leaves.Count > 0) ambience.PlayDoor();
            yield return new WaitForSeconds(e.leaves.Count > 0 ? 0.95f : 0.1f);
            // 3) walk through the doorway, with a gentle step rhythm
            bool titled = false;
            float dur = Mathf.Clamp(Vector3.Distance(start, end) / 2.6f, 1.6f, 4f);
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
            {
                float s = t * t * (3f - 2f * t);
                player.position = Vector3.Lerp(start, end, s) + Vector3.up * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 4f)) * 0.04f;
                fpc.Pitch = Mathf.Lerp(-4f, 2f, s);
                if (!titled && t > 0.35f)
                {
                    titled = true;
                    if (goingIn) hud.ShowTitle(e.title, e.subtitle);
                    else hud.ShowTitle(City.StreetAt(end, out _), "Buenos Aires · mayo de 1810");
                }
                yield return null;
            }
            player.position = end;
            // 4) the doors close behind
            foreach (var l in e.leaves) l.Open(false);
            hud.Cinematic(false);
            yield return new WaitForSeconds(0.4f);
            cc.enabled = true;
            FirstPersonController.InputLocked = false;
            busy = false;
        }

        // ------------------------------------------------------------------ per frame
        void Update()
        {
            Site inside = null;
            foreach (var s in sites) if (s.IsInside(player.position)) { inside = s; break; }
            crowd.playerSite = inside;

            if (inside != null) hud.SetPlace(inside.title, "Interior · " + City.Place(inside.key).kicker);
            else
            {
                string street = City.StreetAt(player.position, out _);
                hud.SetPlace(street, "Cerca de: " + NearestPlace());
            }

            // the nearest door within reach offers "Entrar" / "Salir"
            promptEntrance = null;
            if (!busy)
            {
                float best = 3.3f;
                foreach (var s in sites)
                    foreach (var e in s.entrances)
                    {
                        float dOut = Flat(player.position - e.outside), dIn = Flat(player.position - e.inside);
                        if (dOut < best && Mathf.Abs(player.position.y - e.outside.y) < 1.5f) { best = dOut; promptEntrance = e; promptIn = true; }
                        if (dIn < best && Mathf.Abs(player.position.y - e.inside.y) < 1.5f) { best = dIn; promptEntrance = e; promptIn = false; }
                    }
            }
            if (promptEntrance != null) hud.SetPrompt(promptIn ? "Entrar: " + promptEntrance.title : promptEntrance.exitTitle, promptIn ? "Entrar" : "Salir");
            else hud.SetPrompt(null, null);

            var kb = Keyboard.current;
            if (kb != null && !busy)
            {
                if (kb.eKey.wasPressedThisFrame && promptEntrance != null) StartCoroutine(Pass(promptEntrance, promptIn));
                if (kb.mKey.wasPressedThisFrame) ColonialBackToMap();
            }
            // never fall out of the world
            if (player.position.y < -20f) PlaceAt("plaza");
        }

        static float Flat(Vector3 v) { v.y = 0; return v.magnitude; }

        string NearestPlace()
        {
            string best = ""; float bd = 1e9f;
            foreach (var k in City.PlaceKeys)
            {
                var p = City.Place(k);
                float d = (City.W2U(p.tp.x, p.tp.z) - player.position).sqrMagnitude;
                if (d < bd) { bd = d; best = p.name; }
            }
            return best;
        }

        // ------------------------------------------------------------------ plaza market and look
        void BuildMarket(Transform parent)
        {
            var g = new LowPoly(); var r = new Rng(1803);
            var awnings = new[] { Palette.Hex(0xe8dcc0), Palette.Hex(0xc9a24a), Palette.Hex(0x8a2d2d), Palette.Hex(0x2f4f7a), Palette.Hex(0xb8673a) };
            for (float x = 696; x <= 778; x += 12)
            {
                Props.Stall(g, City.W2U(x, 357), 180f, r.Pick(awnings), r);
                Props.Stall(g, City.W2U(x, 440), 0f, r.Pick(awnings), r);
            }
            Props.Cart(g, City.W2U(776, 396), 10f, true);
            Props.Cart(g, City.W2U(704, 424), -100f, true);
            Props.Cart(g, City.W2U(584, 442), 70f, true);
            for (int i = 0; i < 10; i++) Props.Barrel(g, City.W2U(r.Range(690, 780), r.Range(366, 432)));
            for (int i = 0; i < 8; i++) Props.Sack(g, City.W2U(r.Range(690, 780), r.Range(366, 432)), Palette.Hex(0xc9b38a));
            // hitching posts along the Cabildo street
            for (float z = 362; z <= 434; z += 6) g.Cyl(City.W2U(570.5f, z), 0.1f, 0.08f, 1.1f, 5, CityBuilder.WoodDark);
            var go = CityBuilder.MeshObject("Mercado", g.ToMesh("mercado"), palette, parent, true);
        }

        void GroundTextures()
        {
            dirt.SetTexture("_BaseMap", Noise(128, new Color(0.62f, 0.52f, 0.38f), new Color(0.5f, 0.41f, 0.29f), 9f, 11));
            grass.SetTexture("_BaseMap", Noise(128, new Color(0.47f, 0.55f, 0.29f), new Color(0.36f, 0.45f, 0.22f), 12f, 23));
            water.SetTexture("_BaseMap", Noise(128, new Color(0.45f, 0.56f, 0.56f), new Color(0.36f, 0.47f, 0.49f), 6f, 37));
        }

        static Texture2D Noise(int n, Color a, Color b, float freq, int seed)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 4 };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    // tileable value noise: sum of periodic sines + hashed speckles
                    float u = x / (float)n * Mathf.PI * 2, v = y / (float)n * Mathf.PI * 2;
                    float f = 0.5f + 0.25f * Mathf.Sin(u * 2 + Mathf.Sin(v * 3 + seed) * 1.3f) * Mathf.Cos(v * 2 + seed * 0.7f)
                              + 0.15f * Mathf.Sin(u * 5 + v * 3 + seed) + 0.1f * Mathf.Cos(u * 9 - v * 7 + seed * 1.3f);
                    uint h = (uint)(x * 73856093 ^ y * 19349663 ^ seed * 83492791);
                    h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
                    f += ((h & 255) / 255f - 0.5f) * 0.35f;
                    px[y * n + x] = Color.Lerp(a, b, Mathf.Clamp01(f));
                }
            t.SetPixels(px); t.Apply(true);
            return t;
        }

        void Atmosphere()
        {
            // a grey-gold afternoon of late May (autumn), with a hint of the famous drizzle
            var fogC = new Color(0.78f, 0.8f, 0.79f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogC;
            RenderSettings.fogStartDistance = mobile ? 60f : 110f;
            RenderSettings.fogEndDistance = mobile ? 330f : 620f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.72f, 0.74f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.6f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.42f, 0.37f, 0.3f);
            if (sun != null)
            {
                sun.color = new Color(1f, 0.93f, 0.8f);
                sun.intensity = 1.25f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.75f;
                RenderSettings.sun = sun;
            }
        }
    }
}
