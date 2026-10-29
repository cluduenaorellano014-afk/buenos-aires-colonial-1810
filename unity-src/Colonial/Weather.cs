using UnityEngine;

namespace Colonial
{
    // Rain showers over the city: a particle curtain that follows the camera and dies on roofs and
    // floors (so it never falls indoors), a darker sky and fog while it rains, wet ground and the
    // odd lightning flash. Showers come and go on their own; the HUD button forces them on or off.
    public class Weather : MonoBehaviour
    {
        public float Rain { get; private set; }          // 0 dry .. 1 downpour (smoothed)
        public bool Raining => target > 0f;
        public System.Action onThunder;                   // the sound side plays the rumble

        Transform cam;
        ParticleSystem ps;
        ParticleSystem.EmissionModule emission;
        Light sun;
        Material sky, dirt, grass;
        bool mobile;
        float target, timer, flash, nextFlash, coverTimer, shelter;
        bool covered;
        int maxParticles;

        // the dry look, captured at start
        float sunI, fogStart, fogEnd, skyExposure;
        Color fogC, ambSky, ambEq, ambGround, skyTint, dirtC, grassC;

        static readonly Color RainFog = new Color(0.55f, 0.58f, 0.6f);

        public void Init(Camera camera, Light sunLight, Material rainMat, Material dirtMat, Material grassMat, bool isMobile)
        {
            cam = camera.transform; sun = sunLight; mobile = isMobile; dirt = dirtMat; grass = grassMat;
            sunI = sun != null ? sun.intensity : 1f;
            fogC = RenderSettings.fogColor; fogStart = RenderSettings.fogStartDistance; fogEnd = RenderSettings.fogEndDistance;
            ambSky = RenderSettings.ambientSkyColor; ambEq = RenderSettings.ambientEquatorColor; ambGround = RenderSettings.ambientGroundColor;
            if (RenderSettings.skybox != null)
            {
                sky = new Material(RenderSettings.skybox);    // runtime copy: never touch the asset
                RenderSettings.skybox = sky;
                skyExposure = sky.HasProperty("_Exposure") ? sky.GetFloat("_Exposure") : 1f;
                skyTint = sky.HasProperty("_SkyTint") ? sky.GetColor("_SkyTint") : Color.grey;
            }
            if (dirt != null) dirtC = dirt.GetColor("_BaseColor");
            if (grass != null) grassC = grass.GetColor("_BaseColor");
            BuildParticles(rainMat);
            timer = Random.Range(35f, 60f);   // first shower soon after arriving
            nextFlash = Random.Range(12f, 30f);
        }

        // HUD button: start or stop the rain now (the automatic cycle resumes later)
        public bool Toggle()
        {
            target = target > 0f ? 0f : Random.Range(0.7f, 1f);
            timer = target > 0f ? Random.Range(150f, 240f) : Random.Range(240f, 360f);
            return Raining;
        }

        void BuildParticles(Material mat)
        {
            var go = new GameObject("Lluvia");
            go.transform.SetParent(transform, false);
            ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            maxParticles = mobile ? 1000 : 2600;

            var main = ps.main;
            main.loop = true; main.playOnAwake = false;
            main.duration = 5f;
            main.startLifetime = 1.5f;
            main.startSpeed = 0f;
            main.startSize = mobile ? 0.035f : 0.026f;
            main.startColor = new Color(0.78f, 0.82f, 0.88f, 0.42f);
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;

            emission = ps.emission;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(mobile ? 26f : 38f, 0.5f, mobile ? 26f : 38f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(1.1f, 1.8f);   // a little wind from the river
            vel.y = new ParticleSystem.MinMaxCurve(-15f, -12.5f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);

            // drops die where they hit: roofs keep the interiors dry
            var col = ps.collision;
            col.enabled = true;
            col.type = ParticleSystemCollisionType.World;
            col.mode = ParticleSystemCollisionMode.Collision3D;
            col.quality = ParticleSystemCollisionQuality.High;   // exact raycasts: the voxel cache lets drops through thin roofs
            col.lifetimeLoss = 1f;
            col.radiusScale = 0.1f;
            col.collidesWith = ~(1 << 2);   // not the invisible guards or the people
            col.enableDynamicColliders = false;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.045f; r.lengthScale = 1.2f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            if (mat != null)
            {
                r.sharedMaterial = mat;
                mat.SetTexture("_BaseMap", StreakTexture());
            }
            ps.Play();
        }

        static Texture2D StreakTexture()
        {
            // a thin vertical streak, soft at the ends and edges
            const int W = 8, H = 64;
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = Mathf.Abs((x + 0.5f) / W - 0.5f) * 2f, v = (y + 0.5f) / H;
                    float a = Mathf.Clamp01(1f - u * u) * Mathf.Sin(v * Mathf.PI);
                    px[y * W + x] = new Color(1, 1, 1, a);
                }
            t.SetPixels(px); t.Apply(false);
            return t;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            // automatic cycle of showers and dry spells
            timer -= dt;
            if (timer <= 0f)
            {
                if (target > 0f) { target = 0f; timer = Random.Range(150f, 280f); }
                else { target = Random.Range(0.45f, 1f); timer = Random.Range(110f, 200f); }
            }
            Rain = Mathf.MoveTowards(Rain, target, dt / 12f);   // showers build up and fade over ~12 s

            // under a roof the curtain thins out: what's left falls on the patios and streets nearby
            coverTimer -= dt;
            if (coverTimer <= 0f) { coverTimer = 0.25f; covered = Physics.Raycast(cam.position, Vector3.up, 40f, ~(1 << 2), QueryTriggerInteraction.Ignore); }
            shelter = Mathf.MoveTowards(shelter, covered ? 1f : 0f, dt * 3f);
            emission.rateOverTime = Rain * maxParticles / 1.5f * (1f - shelter * 0.85f);

            // lightning in the heavy showers
            if (Rain > 0.75f)
            {
                nextFlash -= dt;
                if (nextFlash <= 0f) { flash = 1f; nextFlash = Random.Range(18f, 45f); onThunder?.Invoke(); }
            }
            flash = Mathf.MoveTowards(flash, 0f, dt * 3.5f);
            float f = flash > 0f ? flash * (0.6f + 0.4f * Mathf.Sin(Time.time * 60f)) : 0f;

            float k = Mathf.SmoothStep(0f, 1f, Rain);
            if (sun != null) sun.intensity = Mathf.Lerp(sunI, sunI * 0.3f, k) + f * 1.6f;
            RenderSettings.fogColor = Color.Lerp(fogC, RainFog, k) + new Color(0.25f, 0.25f, 0.28f) * f;
            RenderSettings.fogStartDistance = Mathf.Lerp(fogStart, fogStart * 0.35f, k);
            RenderSettings.fogEndDistance = Mathf.Lerp(fogEnd, fogEnd * 0.45f, k);
            RenderSettings.ambientSkyColor = Color.Lerp(ambSky, ambSky * 0.62f, k) + Color.white * 0.35f * f;
            RenderSettings.ambientEquatorColor = Color.Lerp(ambEq, ambEq * 0.66f, k);
            RenderSettings.ambientGroundColor = Color.Lerp(ambGround, ambGround * 0.7f, k);
            if (sky != null)
            {
                if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", Mathf.Lerp(skyExposure, skyExposure * 0.5f, k) + f * 0.8f);
                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", Color.Lerp(skyTint, new Color(0.42f, 0.44f, 0.47f), k));
            }
            // wet earth is darker
            if (dirt != null) dirt.SetColor("_BaseColor", Color.Lerp(dirtC, dirtC * 0.72f, k));
            if (grass != null) grass.SetColor("_BaseColor", Color.Lerp(grassC, grassC * 0.82f, k));
        }

        void LateUpdate()
        {
            // the emitter rides above the camera, a bit ahead so the view is always covered
            var fwd = cam.forward; fwd.y = 0;
            ps.transform.position = cam.position + Vector3.up * 16f + fwd.normalized * 5f - Vector3.right * 2.5f;
        }

        void OnDestroy()
        {
            // the ground materials are assets shared with the editor: put their colors back
            if (dirt != null) dirt.SetColor("_BaseColor", dirtC);
            if (grass != null) grass.SetColor("_BaseColor", grassC);
        }
    }
}
