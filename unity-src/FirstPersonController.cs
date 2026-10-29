using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// First-person walker. Computer: WASD/arrows to move, mouse to look, Esc frees the cursor
// (click to capture it again). Phone/tablet: on-screen joystick + drag to look (MobileTouchInput).
// The active scheme is detected automatically by MobileTouchInput.
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    public Transform cameraPivot;
    public float walkSpeed = 1.6f;
    public float mouseSensitivity = 0.15f;
    public float gamepadLookSpeed = 170f;
    public float gravity = -9.81f;
    public float headBobAmount = 0.025f;
    public float headBobFrequency = 1.8f;
    public float runMultiplier = 1f;            // > 1 enables Shift / the on-screen run button

    public static bool InputLocked;            // cutscenes: no look/move input
    public static bool RunToggle;              // on-screen run button
    public static bool MenuOpen;               // a menu needs the mouse pointer

    CharacterController controller;
    MobileTouchInput touch;
    float pitch;
    float verticalVelocity;
    float bobPhase;
    float eyeBaseY;
    bool wasTouchMode;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        touch = GetComponent<MobileTouchInput>();
        if (touch == null) touch = gameObject.AddComponent<MobileTouchInput>();
        if (cameraPivot == null && Camera.main != null) cameraPivot = Camera.main.transform;
        eyeBaseY = cameraPivot.localPosition.y;
    }

    void Start()
    {
        wasTouchMode = MobileTouchInput.IsTouchMode;
#if UNITY_WEBGL && !UNITY_EDITOR
        // browsers only allow pointer lock after a click, which Update handles
        LockCursor(false);
#else
        LockCursor(!wasTouchMode);
#endif
    }

    static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    public float Pitch { get => pitch; set => pitch = value; }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        var pad = Gamepad.current;
        bool touchMode = MobileTouchInput.IsTouchMode;

        // ---- cursor (computer only) ----
        if (touchMode != wasTouchMode) { LockCursor(!touchMode); wasTouchMode = touchMode; }
        if (!touchMode)
        {
            if (kb != null && kb.escapeKey.wasPressedThisFrame) LockCursor(false);
            if (MenuOpen && Cursor.lockState == CursorLockMode.Locked) LockCursor(false);
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (!MenuOpen && !overUI && mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) LockCursor(true);
        }

        if (!controller.enabled)
        {
            // a cutscene is moving the player: only keep the camera pitch in sync
            cameraPivot.localPosition = new Vector3(0, eyeBaseY, 0);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0, 0);
            return;
        }

        // ---- look ----
        Vector2 look = InputLocked ? Vector2.zero : touch.LookDelta;
        if (!InputLocked && !touchMode && mouse != null && Cursor.lockState == CursorLockMode.Locked) look += mouse.delta.ReadValue() * mouseSensitivity;
        if (!InputLocked && pad != null) look += pad.rightStick.ReadValue() * gamepadLookSpeed * Time.deltaTime;
        transform.Rotate(0f, look.x, 0f);
        pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);

        // ---- move ----
        Vector2 input = touch.Move;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1;
        }
        if (pad != null) input += pad.leftStick.ReadValue();
        input = InputLocked ? Vector2.zero : Vector2.ClampMagnitude(input, 1f);

        bool running = runMultiplier > 1f && (RunToggle || (kb != null && kb.shiftKey.isPressed) || (pad != null && pad.leftStickButton.isPressed));
        Vector3 move = (transform.right * input.x + transform.forward * input.y) * walkSpeed * (running ? runMultiplier : 1f);
        verticalVelocity = controller.isGrounded ? -1f : verticalVelocity + gravity * Time.deltaTime;
        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);

        // ---- head bob ----
        float planar = new Vector2(controller.velocity.x, controller.velocity.z).magnitude;
        if (controller.isGrounded && planar > 0.1f) bobPhase += Time.deltaTime * headBobFrequency * Mathf.Sqrt(planar / walkSpeed) * Mathf.PI * 2f;
        else bobPhase = Mathf.Lerp(bobPhase, Mathf.Round(bobPhase / Mathf.PI) * Mathf.PI, Time.deltaTime * 4f);
        float bob = Mathf.Sin(bobPhase) * headBobAmount * Mathf.Clamp01(planar / walkSpeed);
        cameraPivot.localPosition = new Vector3(0, eyeBaseY + bob, 0);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0, 0);
    }
}
