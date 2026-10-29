using System.Collections.Generic;
using UnityEngine;

namespace Colonial
{
    // Ambient sound, synthesized at startup (no audio files to download): rain outdoors and drumming
    // on the roof indoors, wind, the murmur of the crowd, the river, the Jabonería fires, the
    // Catedral bell, birds, thunder, footsteps and creaking doors.
    public class Ambience : MonoBehaviour
    {
        const int SR = 22050;

        Transform player, cam;
        CharacterController cc;
        Weather weather;
        Crowd crowd;
        List<Site> sites;
        System.Random rnd = new System.Random(1810);

        AudioSource rainOut, rainIn, wind, murmur, water, oneShot;
        AudioClip bell, thunder, door;
        AudioClip[] chirps, steps, wetSteps;
        Vector3 bellPos;
        AudioSource bellSrc, birdSrc;
        float covered, cov, coverTimer, bellTimer, birdTimer, crowdLevel, crowdTimer, stepDist;
        public bool Enabled { get; private set; } = true;

        public void Init(Transform playerT, Camera camera, Weather w, Crowd c, List<Site> siteList)
        {
            player = playerT; cam = camera.transform; cc = player.GetComponent<CharacterController>();
            weather = w; crowd = c; sites = siteList;

            rainOut = Loop("Lluvia", RainOutdoor());
            rainIn = Loop("Lluvia en el techo", RainOnRoof());
            wind = Loop("Viento", Wind());
            murmur = Loop("Gente", Murmur());
            water = Loop("Río", Water());
            oneShot = gameObject.AddComponent<AudioSource>(); oneShot.playOnAwake = false;

            // fires under the soap pans of the Jabonería
            var jab = sites.Find(s => s.key == "jaboneria");
            if (jab != null) Spot("Fuego", Fire(), jab.World(new Vector3(14f, 1f, -12.5f)), 2f, 16f, 0.5f, AudioRolloffMode.Linear);
            var cat = sites.Find(s => s.key == "catedral");
            bellPos = cat != null ? cat.World(new Vector3(-17f, 22f, -3f)) : City.W2U(600, 320, 22f);

            bell = Bell(); thunder = Thunder(); door = Door();
            bellSrc = Spot("Campana", null, bellPos, 40f, 900f, 1f, AudioRolloffMode.Logarithmic);
            birdSrc = Spot("Pájaros", null, Vector3.zero, 6f, 60f, 1f, AudioRolloffMode.Logarithmic);
            chirps = new AudioClip[4]; for (int i = 0; i < chirps.Length; i++) chirps[i] = Chirp(i);
            steps = new AudioClip[4]; wetSteps = new AudioClip[4];
            for (int i = 0; i < 4; i++) { steps[i] = Step(i, false); wetSteps[i] = Step(i, true); }

            bellTimer = 20f; birdTimer = 3f;
            weather.onThunder += () => StartCoroutine(Delayed(thunder, Rand(0.6f, 2.4f), Rand(0.55f, 0.9f)));
            SetEnabled(PlayerPrefs.GetInt("colonial_sonido", 1) == 1);
        }

        public bool Toggle() { SetEnabled(!Enabled); return Enabled; }
        void SetEnabled(bool on)
        {
            Enabled = on;
            AudioListener.volume = on ? 1f : 0f;
            PlayerPrefs.SetInt("colonial_sonido", on ? 1 : 0);
        }

        public void PlayDoor() => oneShot.PlayOneShot(door, 0.55f);

        System.Collections.IEnumerator Delayed(AudioClip c, float delay, float vol)
        {
            yield return new WaitForSeconds(delay);
            oneShot.PlayOneShot(c, vol);
        }

        AudioSource Loop(string name, AudioClip clip)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var a = go.AddComponent<AudioSource>();
            a.clip = clip; a.loop = true; a.volume = 0f; a.spatialBlend = 0f; a.playOnAwake = false;
            a.time = Rand(0f, clip.length * 0.9f);
            a.Play();
            return a;
        }

        AudioSource Spot(string name, AudioClip clip, Vector3 pos, float min, float max, float vol, AudioRolloffMode rolloff)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false); go.transform.position = pos;
            var a = go.AddComponent<AudioSource>();
            a.clip = clip; a.loop = clip != null; a.volume = vol; a.spatialBlend = 1f; a.rolloffMode = rolloff;
            a.minDistance = min; a.maxDistance = max; a.dopplerLevel = 0f; a.playOnAwake = false;
            if (clip != null) a.Play();
            return a;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector3 p = cam.position;
            Site inside = null;
            foreach (var s in sites) if (s.IsInside(player.position)) { inside = s; break; }

            // under a roof? (interiors, galleries, the recova)
            coverTimer -= dt;
            if (coverTimer <= 0f)
            {
                coverTimer = 0.25f;
                bool roof = Physics.Raycast(p, Vector3.up, 40f, ~(1 << 2), QueryTriggerInteraction.Ignore);
                covered = roof ? 1f : 0f;
            }
            cov = Mathf.MoveTowards(cov, covered, dt * 2f);
            float rain = weather.Rain;
            Fade(rainOut, rain * Mathf.Lerp(0.75f, 0.12f, cov), dt);
            Fade(rainIn, rain * cov * 0.65f, dt);
            Fade(wind, Mathf.Lerp(0.1f + rain * 0.14f, 0.03f, cov), dt);

            // crowd murmur grows with the number of people around
            crowdTimer -= dt;
            if (crowdTimer <= 0f)
            {
                crowdTimer = 0.5f;
                int n = 0;
                foreach (var a in crowd.agents) if (a.go.activeSelf && a.site == inside && (a.t.position - p).sqrMagnitude < 22f * 22f) n++;
                crowdLevel = Mathf.Clamp01(n / 12f);
            }
            Fade(murmur, crowdLevel * 0.42f * (1f - rain * 0.45f), dt * 0.5f);

            // the river: loud on the shore, gone a few blocks inland
            float shore = City.W + 50f - p.x;
            Fade(water, Mathf.Clamp01(1f - shore / 160f) * (inside != null ? 0.1f : 0.5f), dt);

            // the Catedral bell, now and then: three strokes
            bellTimer -= dt;
            if (bellTimer <= 0f)
            {
                bellTimer = Rand(80f, 150f);
                for (int i = 0; i < 3; i++) StartCoroutine(BellAt(i * 2.4f));
            }

            // birds in dry weather, outdoors
            birdTimer -= dt;
            if (birdTimer <= 0f)
            {
                birdTimer = Rand(2.5f, 8f);
                if (rain < 0.15f && inside == null && cov < 0.5f)
                {
                    float ang = Rand(0f, Mathf.PI * 2f), r = Rand(8f, 26f);
                    birdSrc.transform.position = p + new Vector3(Mathf.Cos(ang) * r, Rand(3f, 9f), Mathf.Sin(ang) * r);
                    birdSrc.pitch = Rand(0.9f, 1.15f);
                    birdSrc.PlayOneShot(chirps[rnd.Next(chirps.Length)], Rand(0.25f, 0.5f));
                }
            }

            // footsteps
            if (cc != null && cc.enabled && cc.isGrounded)
            {
                var v = cc.velocity; v.y = 0;
                float speed = v.magnitude;
                stepDist += speed * dt;
                float stride = speed > 4.5f ? 1.05f : 0.75f;
                if (speed > 0.3f && stepDist > stride)
                {
                    stepDist = 0f;
                    bool wet = rain > 0.3f && cov < 0.5f;
                    var set = wet ? wetSteps : steps;
                    oneShot.pitch = Rand(0.9f, 1.1f);
                    oneShot.PlayOneShot(set[rnd.Next(set.Length)], speed > 4.5f ? 0.32f : 0.22f);
                    oneShot.pitch = 1f;
                }
            }
        }

        System.Collections.IEnumerator BellAt(float delay)
        {
            yield return new WaitForSeconds(delay);
            bellSrc.PlayOneShot(bell, 0.9f);
        }

        static void Fade(AudioSource a, float target, float dt) => a.volume = Mathf.MoveTowards(a.volume, target, dt * 0.8f);
        float Rand(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        float N() => (float)rnd.NextDouble() * 2f - 1f;

        // ================================================================== synthesis
        static float A(float fc) => 1f - Mathf.Exp(-2f * Mathf.PI * fc / SR);   // one-pole coefficient

        AudioClip Make(string name, float[] d)
        {
            float peak = 0f; foreach (var x in d) peak = Mathf.Max(peak, Mathf.Abs(x));
            if (peak > 0f) for (int i = 0; i < d.Length; i++) d[i] *= 0.9f / peak;
            var c = AudioClip.Create(name, d.Length, 1, SR, false);
            c.SetData(d, 0);
            return c;
        }

        // generate len + xf seconds, then crossfade the tail into the head so the loop is seamless
        AudioClip MakeLoop(string name, float seconds, System.Func<int, float[]> gen)
        {
            int L = (int)(seconds * SR), F = SR / 2;
            var x = gen(L + F);
            var y = new float[L];
            for (int i = 0; i < L; i++) y[i] = x[i];
            for (int i = 0; i < F; i++) { float k = i / (float)F; y[i] = x[i] * k + x[L + i] * (1f - k); }
            return Make(name, y);
        }

        AudioClip RainOutdoor() => MakeLoop("lluvia", 4f, n =>
        {
            var d = new float[n]; float lp = 0, lp2 = 0, drop = 0, dlp = 0;
            for (int i = 0; i < n; i++)
            {
                float w = N();
                lp += A(6500f) * (w - lp); lp2 += A(500f) * (lp - lp2);
                float hiss = (lp - lp2) * 0.6f;
                if (rnd.NextDouble() < 90.0 / SR) drop = Rand(0.4f, 1f);     // individual drops on the street
                drop *= 0.996f; dlp += A(3000f) * (N() * drop - dlp);
                d[i] = hiss + dlp * 0.8f;
            }
            return d;
        });

        AudioClip RainOnRoof() => MakeLoop("lluvia techo", 4f, n =>
        {
            var d = new float[n]; float lp = 0, drum = 0, dlp = 0;
            for (int i = 0; i < n; i++)
            {
                lp += A(380f) * (N() - lp);
                if (rnd.NextDouble() < 140.0 / SR) drum = Rand(0.5f, 1f);   // drops on the tiles
                drum *= 0.993f; dlp += A(260f) * (N() * drum - dlp);
                d[i] = lp * 1.4f + dlp * 2.2f;
            }
            return d;
        });

        AudioClip Wind() => MakeLoop("viento", 8f, n =>
        {
            var d = new float[n]; float lp = 0, lp2 = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float gust = 0.55f + 0.3f * Mathf.Sin(t * 0.7f) + 0.15f * Mathf.Sin(t * 1.9f + 1f);
                lp += A(250f + 350f * gust) * (N() - lp); lp2 += A(90f) * (lp - lp2);
                d[i] = (lp - lp2 * 0.5f) * gust;
            }
            return d;
        });

        // babble: a few synthetic voices (pulse train through two vowel formants), syllable by syllable
        AudioClip Murmur() => MakeLoop("gente", 8f, n =>
        {
            var d = new float[n];
            float[][] vowels = { new[] { 730f, 1090f }, new[] { 530f, 1840f }, new[] { 270f, 2290f }, new[] { 570f, 840f }, new[] { 300f, 870f } };
            for (int v = 0; v < 8; v++)
            {
                float f0 = Rand(95f, 230f), ph = 0, env = 0;
                var b1 = new Biquad(); var b2 = new Biquad();
                int sylEnd = 0, sylLen = 1; bool pause = false;
                for (int i = 0; i < n; i++)
                {
                    if (i >= sylEnd)
                    {
                        sylLen = (int)(Rand(0.09f, 0.26f) * SR); sylEnd = i + sylLen;
                        pause = rnd.NextDouble() < 0.22;
                        var vw = vowels[rnd.Next(vowels.Length)];
                        b1.BandPass(vw[0] * Rand(0.9f, 1.1f), 5f); b2.BandPass(vw[1] * Rand(0.9f, 1.1f), 7f);
                        f0 *= Rand(0.94f, 1.06f); f0 = Mathf.Clamp(f0, 90f, 250f);
                    }
                    float k = 1f - (sylEnd - i) / (float)sylLen;
                    float target = pause ? 0f : Mathf.Sin(k * Mathf.PI);
                    env += (target - env) * 0.004f;
                    ph += f0 / SR; if (ph >= 1f) ph -= 1f;
                    float src = (ph < 0.08f ? 1f : 0f) + N() * 0.05f;
                    d[i] += (b1.Run(src) + b2.Run(src) * 0.6f) * env;
                }
            }
            float lp = 0;
            for (int i = 0; i < n; i++) { lp += A(2200f) * (d[i] - lp); d[i] = lp; }
            return d;
        });

        AudioClip Water() => MakeLoop("rio", 6f, n =>
        {
            var d = new float[n]; float lp = 0, lp2 = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float swell = 0.5f + 0.5f * Mathf.Sin(t * 1.05f) * Mathf.Sin(t * 0.37f + 0.5f);
                lp += A(300f + 900f * swell) * (N() - lp); lp2 += A(60f) * (lp - lp2);
                d[i] = (lp - lp2) * (0.35f + swell);
            }
            return d;
        });

        AudioClip Fire() => MakeLoop("fuego", 4f, n =>
        {
            var d = new float[n]; float lp = 0, crack = 0, hp = 0, prev = 0;
            for (int i = 0; i < n; i++)
            {
                lp += A(500f) * (N() - lp);
                if (rnd.NextDouble() < 18.0 / SR) crack = Rand(0.5f, 1f);
                crack *= 0.985f;
                float c = N() * crack; hp = 0.85f * (hp + c - prev); prev = c;
                d[i] = lp * 0.9f + hp * 0.7f;
            }
            return d;
        });

        AudioClip Bell()
        {
            int n = (int)(7f * SR);
            var d = new float[n];
            float f = 196f;   // a big bronze bell, about G3
            float[] ratio = { 0.5f, 1f, 1.183f, 1.506f, 2f, 2.514f, 2.662f, 3.011f, 4.166f };
            float[] amp = { 0.5f, 1f, 0.6f, 0.45f, 0.5f, 0.25f, 0.2f, 0.18f, 0.1f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR, s = 0;
                for (int k = 0; k < ratio.Length; k++) s += amp[k] * Mathf.Sin(2f * Mathf.PI * f * ratio[k] * t) * Mathf.Exp(-t * (0.5f + ratio[k] * 0.45f));
                if (t < 0.03f) s += N() * (1f - t / 0.03f) * 0.6f;   // the strike
                d[i] = s;
            }
            return Make("campana", d);
        }

        AudioClip Thunder()
        {
            int n = (int)(6f * SR);
            var d = new float[n]; float lp = 0, lp2 = 0, bump = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                if (rnd.NextDouble() < 6.0 / SR) bump = Rand(0.4f, 1f);
                bump *= 0.9997f;
                float env = Mathf.Min(1f, t * 6f) * Mathf.Exp(-t * 0.55f) * (0.5f + bump);
                lp += A(Mathf.Lerp(420f, 110f, t / 6f)) * (N() - lp); lp2 += A(40f) * (lp - lp2);
                d[i] = (lp - lp2 * 0.6f) * env;
            }
            return Make("trueno", d);
        }

        AudioClip Chirp(int seed)
        {
            int n = (int)(0.7f * SR);
            var d = new float[n];
            int notes = 2 + seed % 3; float pos = 0.02f;
            for (int k = 0; k < notes; k++)
            {
                float len = Rand(0.05f, 0.11f), f0 = Rand(2600f, 4200f), f1 = f0 * Rand(0.7f, 1.4f), ph = 0;
                int a = (int)(pos * SR), b = Mathf.Min(n, a + (int)(len * SR));
                for (int i = a; i < b; i++)
                {
                    float u = (i - a) / (float)(b - a);
                    float f = Mathf.Lerp(f0, f1, u) * (1f + 0.04f * Mathf.Sin(u * 60f));
                    ph += f / SR;
                    d[i] += Mathf.Sin(2f * Mathf.PI * ph) * Mathf.Sin(u * Mathf.PI);
                }
                pos += len + Rand(0.03f, 0.09f);
            }
            return Make("pajaro", d);
        }

        AudioClip Step(int seed, bool wet)
        {
            int n = (int)(0.2f * SR);
            var d = new float[n]; float lp = 0, hp = 0, prev = 0;
            float fc = 700f + seed * 120f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float env = Mathf.Exp(-t * 38f);
                lp += A(fc) * (N() - lp);
                float thump = Mathf.Sin(2f * Mathf.PI * (70f + seed * 6f) * t) * Mathf.Exp(-t * 45f);
                float s = lp * env * 1.4f + thump * 0.6f;
                if (wet) { float w = N() * Mathf.Exp(-t * 18f); hp = 0.9f * (hp + w - prev); prev = w; s += hp * 0.5f; }
                d[i] = s;
            }
            return Make(wet ? "paso mojado" : "paso", d);
        }

        AudioClip Door()
        {
            // a slow wooden creak: a rough sawtooth whose pitch wanders
            int n = (int)(1.1f * SR);
            var d = new float[n]; float ph = 0, lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float f = 140f + 60f * Mathf.Sin(t * 5f) + 25f * Mathf.Sin(t * 23f) + N() * 15f;
                ph += f / SR; if (ph >= 1f) ph -= 1f;
                float env = Mathf.Sin(Mathf.Clamp01(t / 1.1f) * Mathf.PI) * (0.6f + 0.4f * Mathf.Sin(t * 31f));
                lp += A(1800f) * ((ph * 2f - 1f) - lp);
                d[i] = lp * env;
            }
            return Make("puerta", d);
        }

        // RBJ band-pass (constant 0 dB peak gain)
        class Biquad
        {
            float b0, b1, b2, a1, a2, x1, x2, y1, y2;
            public void BandPass(float f, float q)
            {
                float w = 2f * Mathf.PI * f / SR, alpha = Mathf.Sin(w) / (2f * q), a0 = 1f + alpha;
                b0 = alpha / a0; b1 = 0f; b2 = -alpha / a0; a1 = -2f * Mathf.Cos(w) / a0; a2 = (1f - alpha) / a0;
            }
            public float Run(float x)
            {
                float y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x; y2 = y1; y1 = y;
                return y;
            }
        }
    }
}
