using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

// Touch controls for phones/tablets and automatic computer/phone detection.
// - Left half of the screen: a virtual joystick appears where the finger lands (walk).
// - Right half: drag to look around.
// Detection: starts in touch mode on mobile platforms (including mobile browsers) or on
// touch-only devices, then follows what the player actually uses — a touch switches to
// touch mode, keyboard/mouse activity switches back to computer mode.
public class MobileTouchInput : MonoBehaviour
{
    public static bool IsTouchMode { get; private set; }
    // optional: screen positions (pixels, origin bottom-left) owned by on-screen buttons
    public static System.Func<Vector2, bool> IsOverUI;

    // Sizes are relative to the screen because browsers don't report a reliable DPI.
    [Tooltip("Joystick radius as a fraction of the screen's shorter side.")]
    public float joystickRadius = 0.11f;
    [Tooltip("Degrees turned when dragging across the full screen height.")]
    public float lookDegreesPerScreen = 120f;

    public Vector2 Move { get; private set; }
    public Vector2 LookDelta { get; private set; }

    int moveFinger = -1, lookFinger = -1;
    Vector2 stickOrigin, stickPos;
    float lastTouchTime = -10f;
    Texture2D circleTex;

    float StickRadiusPx => joystickRadius * Mathf.Min(Screen.width, Screen.height);

    void Awake()
    {
        IsTouchMode = DeviceInfo.IsMobile
                      || (Touchscreen.current != null && Mouse.current == null && Keyboard.current == null);
        if (Application.isMobilePlatform && Application.platform != RuntimePlatform.WebGLPlayer)
        {
            // native apps: landscape only (in browsers the page shows a "rotate the phone" hint instead)
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
        circleTex = MakeCircle(128);
    }

    void OnEnable() => EnhancedTouchSupport.Enable();
    void OnDisable() => EnhancedTouchSupport.Disable();

    void Update()
    {
        DetectMode();

        Vector2 look = Vector2.zero;
        foreach (var t in ETouch.activeTouches)
        {
            int id = t.touchId;
            Vector2 p = t.screenPosition;
            if (t.phase == TouchPhase.Began && IsOverUI != null && IsOverUI(p)) continue;
            if (t.phase == TouchPhase.Began)
            {
                if (p.x < Screen.width * 0.5f && moveFinger < 0) { moveFinger = id; stickOrigin = stickPos = p; }
                else if (p.x >= Screen.width * 0.5f && lookFinger < 0) lookFinger = id;
            }
            if (id == moveFinger)
            {
                stickPos = p;
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) moveFinger = -1;
            }
            else if (id == lookFinger)
            {
                if (t.phase == TouchPhase.Moved) look += t.delta / Screen.height * lookDegreesPerScreen;
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) lookFinger = -1;
            }
        }
        LookDelta = look;

        if (moveFinger >= 0)
        {
            Vector2 offset = Vector2.ClampMagnitude(stickPos - stickOrigin, StickRadiusPx);
            Move = offset / StickRadiusPx;
            if (Move.magnitude < 0.12f) Move = Vector2.zero; // small dead zone
        }
        else Move = Vector2.zero;
    }

    void DetectMode()
    {
        if (ETouch.activeTouches.Count > 0)
        {
            lastTouchTime = Time.unscaledTime;
            IsTouchMode = true;
            return;
        }
        // Browsers can emulate mouse events from touches, so only trust mouse/keyboard a moment after the last touch.
        if (Time.unscaledTime - lastTouchTime < 1f) return;
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        bool keyboardUsed = kb != null && kb.anyKey.wasPressedThisFrame;
        // on phones/tablets only a real keyboard switches to computer controls (browsers emit fake mouse events)
        bool mouseUsed = !DeviceInfo.IsMobile && mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 4f || mouse.leftButton.wasPressedThisFrame);
        if (keyboardUsed || mouseUsed) IsTouchMode = false;
    }

    // ---------------------------------------------------------------- on-screen joystick
    void OnGUI()
    {
        if (!IsTouchMode) return;
        float r = StickRadiusPx;
        float knob = r * 0.45f;
        Vector2 origin, pos;
        float alpha;
        if (moveFinger >= 0)
        {
            origin = stickOrigin;
            pos = stickOrigin + Vector2.ClampMagnitude(stickPos - stickOrigin, r);
            alpha = 0.55f;
        }
        else
        {
            // resting hint at the bottom-left corner
            origin = pos = new Vector2(r * 1.6f, r * 1.6f);
            alpha = 0.25f;
        }
        // GUI y axis points down, touch y axis points up
        var o = new Vector2(origin.x, Screen.height - origin.y);
        var k = new Vector2(pos.x, Screen.height - pos.y);
        var prev = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha * 0.6f);
        GUI.DrawTexture(new Rect(o.x - r, o.y - r, r * 2, r * 2), circleTex);
        GUI.color = new Color(1f, 1f, 1f, alpha + 0.2f);
        GUI.DrawTexture(new Rect(k.x - knob, k.y - knob, knob * 2, knob * 2), circleTex);
        GUI.color = prev;
    }

    static Texture2D MakeCircle(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float c = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                float a = Mathf.Clamp01((1f - d) * size * 0.5f);  // anti-aliased edge
                float ring = Mathf.Lerp(0.55f, 1f, Mathf.SmoothStep(0.75f, 0.95f, d));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * ring));
            }
        tex.Apply();
        return tex;
    }
}
