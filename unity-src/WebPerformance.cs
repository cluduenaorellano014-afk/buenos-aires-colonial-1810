using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Runtime performance for web and mobile builds:
// - picks the "Mobile" quality level on phones/tablets and "PC" on computers,
// - then adapts URP's render scale to the measured frame rate so the room stays smooth
//   on weak GPUs (phones, integrated laptop GPUs, Safari) and sharp on strong ones.
// Does nothing inside the editor or in desktop standalone builds.
public class WebPerformance : MonoBehaviour
{
    // scenes can ask for a different shadow distance (the colonial room needs 14 m, the city much more)
    public static float ShadowDistance = 14f;

    const float SampleSeconds = 1f;
    const float WarmupSeconds = 3f;   // ignore the loading hitch right after start
    const float LowFps = 45f;
    const float HighFps = 57f;

    float minScale, maxScale, scale;
    float elapsed, warmup;
    int frames, goodSamples;
    UniversalRenderPipelineAsset urp;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (Application.isEditor) return;
        if (UnityEngine.XR.XRSettings.isDeviceActive) return;   // VR builds tune themselves (VRPlayerRig)
        bool web = Application.platform == RuntimePlatform.WebGLPlayer;
        if (!web && !Application.isMobilePlatform) return;
        var go = new GameObject("WebPerformance");
        DontDestroyOnLoad(go);
        go.AddComponent<WebPerformance>();
    }

    void Start()
    {
        bool mobile = DeviceInfo.IsMobile;
        SetQuality(mobile ? "Mobile" : "PC");
        urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            // only the sunlit window patch needs shadows, so a short distance is enough
            urp.shadowDistance = ShadowDistance;
        }

        if (mobile)
        {
            minScale = 0.5f; maxScale = 0.85f; scale = 0.75f;
            // candle shadows are the most expensive thing per frame; phones skip them
            foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
                if (l.type == LightType.Point) l.shadows = LightShadows.None;
        }
        else
        {
            minScale = 0.6f; maxScale = 1f; scale = 1f;
        }
        Apply();
    }

    static void SetQuality(string name)
    {
        var names = QualitySettings.names;
        for (int i = 0; i < names.Length; i++)
            if (names[i] == name) { QualitySettings.SetQualityLevel(i, true); return; }
    }

    void Update()
    {
        warmup += Time.unscaledDeltaTime;
        if (warmup < WarmupSeconds) return;

        elapsed += Time.unscaledDeltaTime;
        frames++;
        if (elapsed < SampleSeconds) return;
        float fps = frames / elapsed;
        elapsed = 0f; frames = 0;

        if (fps < LowFps && scale > minScale)
        {
            scale = Mathf.Max(minScale, scale - 0.1f);
            goodSamples = 0;
            Apply();
        }
        else if (fps > HighFps)
        {
            // raise slowly so it doesn't oscillate
            if (++goodSamples >= 3 && scale < maxScale)
            {
                scale = Mathf.Min(maxScale, scale + 0.05f);
                goodSamples = 0;
                Apply();
            }
        }
        else goodSamples = 0;
    }

    void Apply()
    {
        if (urp == null) urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null) urp.renderScale = scale;
    }
}
