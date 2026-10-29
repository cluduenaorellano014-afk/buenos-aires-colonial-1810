using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Colonial
{
    // All the on-screen UI, built in code (legacy uGUI Text, works on every platform without extra assets).
    public class ColonialHUD : MonoBehaviour
    {
        public static readonly Color Parchment = new Color(0.96f, 0.92f, 0.82f, 0.96f), Ink = new Color(0.23f, 0.14f, 0.07f), Red = new Color(0.55f, 0.13f, 0.1f);
        Font font;
        Sprite rounded;
        RectTransform canvasRT;
        Text placeTitle, placeSub, promptText, titleText, subText, helpText, lookHint;
        GameObject promptBox, menu, touchAction, titleGroup;
        Image fade, barTop, barBottom;
        CanvasGroup titleCG;
        Button runBtn;
        readonly List<RectTransform> touchButtons = new List<RectTransform>();
        class Bubble { public RectTransform rt; public Text name, line; public Image bg; }
        readonly List<Bubble> bubbles = new List<Bubble>();
        readonly List<Crowd.Agent> speaking = new List<Crowd.Agent>();
        Crowd crowd; Camera cam;
        public Action onAction, onMap;
        public Action<string> onTravel;
        float titleT, fadeAlpha, fadeTarget, barT, barTarget, helpTimer = 10f;
        bool touchMode;

        public void Init(Crowd c, Camera camera, bool embedded)
        {
            crowd = c; cam = camera;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rounded = MakeRounded();
            if (FindFirstObjectByType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var cgo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var sc = cgo.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1280, 720); sc.matchWidthOrHeight = 0.6f;
            canvasRT = cgo.GetComponent<RectTransform>();

            // speech bubbles (behind everything else)
            for (int i = 0; i < 8; i++)
            {
                var b = new Bubble();
                b.rt = Panel(canvasRT, "Globo", new Color(1f, 0.99f, 0.95f, 0.95f), Vector2.zero, new Vector2(300, 64), new Vector2(0.5f, 0f)).rectTransform;
                b.bg = b.rt.GetComponent<Image>();
                b.rt.anchorMin = b.rt.anchorMax = Vector2.zero;
                var vl = b.rt.gameObject.AddComponent<VerticalLayoutGroup>(); vl.padding = new RectOffset(12, 12, 7, 9); vl.spacing = 1; vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false;
                var fit = b.rt.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                b.name = Label(b.rt, "", 13, Red, TextAnchor.MiddleLeft, FontStyle.BoldAndItalic);
                b.line = Label(b.rt, "", 16, Ink, TextAnchor.MiddleLeft);
                b.rt.gameObject.SetActive(false);
                bubbles.Add(b);
            }

            // place label (top-left)
            var place = Panel(canvasRT, "Lugar", Parchment, new Vector2(16, -16), new Vector2(360, 66), new Vector2(0, 1));
            placeTitle = Label(place.rectTransform, "", 22, Ink, TextAnchor.UpperLeft, FontStyle.Bold); Stretch(placeTitle.rectTransform, 14, 8, 14, 30);
            placeSub = Label(place.rectTransform, "", 15, new Color(0.35f, 0.22f, 0.1f), TextAnchor.LowerLeft, FontStyle.Italic); Stretch(placeSub.rectTransform, 14, 36, 14, 8);

            // buttons (top-right, under the map's own "back" button when embedded)
            float top = embedded ? -76f : -16f;
            if (!embedded) Button(canvasRT, "‹ Mapa", new Vector2(-16, top), new Vector2(150, 48), new Vector2(1, 1), () => onMap?.Invoke(), Red, Color.white);
            var lugares = Button(canvasRT, "Lugares", new Vector2(-16, embedded ? top : top - 58), new Vector2(150, 46), new Vector2(1, 1), ToggleMenu, Parchment, Ink);
            touchButtons.Add(lugares);
            menu = new GameObject("Menu", typeof(RectTransform)); menu.transform.SetParent(canvasRT, false);
            var mrt = (RectTransform)menu.transform; mrt.anchorMin = mrt.anchorMax = mrt.pivot = new Vector2(1, 1); mrt.anchoredPosition = new Vector2(-16, (embedded ? top : top - 58) - 52); mrt.sizeDelta = new Vector2(250, 6 * 46);
            int k = 0;
            foreach (var key in City.PlaceKeys)
            {
                var pd = City.Place(key);
                var b = Button(mrt, pd.name, new Vector2(0, -k * 46), new Vector2(250, 42), new Vector2(1, 1), () => { ToggleMenu(); onTravel?.Invoke(key); }, Parchment, Ink);
                k++;
            }
            menu.SetActive(false);

            // cinematic bars, title card, fade
            barTop = Panel(canvasRT, "Franja", Color.black, Vector2.zero, new Vector2(0, 90), new Vector2(0.5f, 1)); Full(barTop.rectTransform, true);
            barBottom = Panel(canvasRT, "Franja", Color.black, Vector2.zero, new Vector2(0, 90), new Vector2(0.5f, 0)); Full(barBottom.rectTransform, false);
            barTop.sprite = barBottom.sprite = null;
            titleGroup = new GameObject("Titulo", typeof(RectTransform), typeof(CanvasGroup)); titleGroup.transform.SetParent(canvasRT, false);
            var trt = (RectTransform)titleGroup.transform; trt.anchorMin = new Vector2(0, 0.5f); trt.anchorMax = new Vector2(1, 0.5f); trt.sizeDelta = new Vector2(0, 160); trt.anchoredPosition = new Vector2(0, 60);
            titleCG = titleGroup.GetComponent<CanvasGroup>(); titleCG.alpha = 0; titleCG.blocksRaycasts = false;
            titleText = Label(trt, "", 50, new Color(0.98f, 0.94f, 0.84f), TextAnchor.MiddleCenter, FontStyle.Bold); Stretch(titleText.rectTransform, 0, 0, 0, 60);
            subText = Label(trt, "", 22, new Color(0.95f, 0.85f, 0.65f), TextAnchor.MiddleCenter, FontStyle.Italic); Stretch(subText.rectTransform, 0, 105, 0, 10);
            foreach (var t in new[] { titleText, subText }) { var sh = t.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.8f); sh.effectDistance = new Vector2(2, -2); }

            // interaction prompt
            var pr = Panel(canvasRT, "Accion", Parchment, new Vector2(0, 120), new Vector2(420, 52), new Vector2(0.5f, 0));
            pr.rectTransform.anchorMin = pr.rectTransform.anchorMax = new Vector2(0.5f, 0);
            promptBox = pr.gameObject;
            promptText = Label(pr.rectTransform, "", 20, Ink, TextAnchor.MiddleCenter, FontStyle.Bold); Stretch(promptText.rectTransform, 10, 4, 10, 4);
            promptBox.SetActive(false);

            // touch buttons (bottom-right)
            runBtn = Button(canvasRT, "Correr", new Vector2(-24, 24), new Vector2(120, 64), new Vector2(1, 0), () =>
            {
                FirstPersonController.RunToggle = !FirstPersonController.RunToggle;
                runBtn.GetComponent<Image>().color = FirstPersonController.RunToggle ? new Color(0.85f, 0.65f, 0.3f, 0.95f) : new Color(0.1f, 0.07f, 0.04f, 0.55f);
            }, new Color(0.1f, 0.07f, 0.04f, 0.55f), Color.white).GetComponent<Button>();
            touchButtons.Add((RectTransform)runBtn.transform);
            var act = Button(canvasRT, "Entrar", new Vector2(-24, 104), new Vector2(160, 76), new Vector2(1, 0), () => onAction?.Invoke(), Red, Color.white);
            touchAction = act.gameObject; touchButtons.Add(act);

            // help line and "click to look" hint
            var help = Panel(canvasRT, "Ayuda", new Color(0.12f, 0.08f, 0.05f, 0.75f), new Vector2(0, 14), new Vector2(880, 34), new Vector2(0.5f, 0));
            help.rectTransform.anchorMin = help.rectTransform.anchorMax = new Vector2(0.5f, 0);
            helpText = Label(help.rectTransform, "", 15, new Color(0.96f, 0.92f, 0.82f), TextAnchor.MiddleCenter); Stretch(helpText.rectTransform, 8, 2, 8, 2);
            lookHint = Label(canvasRT, "Hacé clic para mirar con el ratón", 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Italic);
            lookHint.rectTransform.anchorMin = lookHint.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); lookHint.rectTransform.sizeDelta = new Vector2(500, 40); lookHint.rectTransform.anchoredPosition = new Vector2(0, -90);
            lookHint.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);

            fade = Panel(canvasRT, "Fundido", Color.black, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            fade.sprite = null; fade.rectTransform.anchorMin = Vector2.zero; fade.rectTransform.anchorMax = Vector2.one; fade.rectTransform.sizeDelta = Vector2.zero; fade.raycastTarget = false;
            fadeAlpha = 1f; fadeTarget = 0f;

            MobileTouchInput.IsOverUI = p =>
            {
                foreach (var rt in touchButtons)
                    if (rt != null && rt.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rt, p, null)) return true;
                return menu.activeSelf;
            };
        }

        // ------------------------------------------------------------------ public API
        public void SetPlace(string title, string sub) { if (placeTitle.text != title) placeTitle.text = title; if (placeSub.text != sub) placeSub.text = sub; }
        public void SetPrompt(string text, string touchLabel)
        {
            bool on = !string.IsNullOrEmpty(text);
            promptBox.SetActive(on && !touchMode);
            touchAction.SetActive(on && touchMode);
            if (on) { promptText.text = "<color=#8c2219>[E]</color>  " + text; touchAction.GetComponentInChildren<Text>().text = touchLabel; }
        }
        public void ShowTitle(string title, string sub) { titleText.text = title; subText.text = sub; titleT = 4.2f; }
        public void Cinematic(bool on) { barTarget = on ? 1f : 0f; }
        public void Fade(bool toBlack) { fadeTarget = toBlack ? 1f : 0f; }
        public bool FadeDone => Mathf.Abs(fadeAlpha - fadeTarget) < 0.01f;
        public bool MenuOpen => menu != null && menu.activeSelf;
        void ToggleMenu() { menu.SetActive(!menu.activeSelf); FirstPersonController.MenuOpen = menu.activeSelf; }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            touchMode = MobileTouchInput.IsTouchMode;
            runBtn.gameObject.SetActive(touchMode);
            fadeAlpha = Mathf.MoveTowards(fadeAlpha, fadeTarget, dt * 2.2f);
            fade.color = new Color(0, 0, 0, fadeAlpha); fade.gameObject.SetActive(fadeAlpha > 0.001f);
            barT = Mathf.MoveTowards(barT, barTarget, dt * 2f);
            float bh = Mathf.SmoothStep(0, 1, barT) * 80f;
            barTop.rectTransform.sizeDelta = new Vector2(0, bh); barBottom.rectTransform.sizeDelta = new Vector2(0, bh);
            if (titleT > 0f) { titleT -= dt; titleCG.alpha = Mathf.Clamp01(Mathf.Min((4.2f - titleT) * 1.6f, titleT * 1.2f)); } else titleCG.alpha = 0f;

            helpTimer -= dt;
            helpText.text = touchMode ? "Izquierda: caminar · Derecha: mirar · «Correr» · «Entrar» junto a las puertas" : "W A S D: caminar · Ratón: mirar · Shift: correr · E: entrar / salir · Esc: soltar el ratón";
            helpText.transform.parent.gameObject.SetActive(helpTimer > 0f || (!touchMode && Cursor.lockState != CursorLockMode.Locked));
            lookHint.gameObject.SetActive(!touchMode && Cursor.lockState != CursorLockMode.Locked && !MenuOpen && barTarget == 0f);

            UpdateBubbles();
        }

        void UpdateBubbles()
        {
            crowd.Speaking(speaking);
            int used = 0;
            float scale = canvasRT.localScale.x;
            foreach (var a in speaking)
            {
                if (used >= bubbles.Count) break;
                var head = a.t.position + Vector3.up * (a.rig.seated ? 1.75f : 2.25f) * a.t.localScale.y;
                var sp = cam.WorldToScreenPoint(head);
                if (sp.z < 0.3f) continue;
                if (sp.x < -100 || sp.x > Screen.width + 100 || sp.y < -50 || sp.y > Screen.height + 50) continue;
                // hidden behind a wall?
                if (Physics.Linecast(cam.transform.position, head - (head - cam.transform.position).normalized * 0.5f, ~(1 << 2), QueryTriggerInteraction.Ignore)) continue;
                var b = bubbles[used++];
                b.rt.gameObject.SetActive(true);
                b.name.text = a.name;
                b.line.text = a.bubble;
                b.line.fontStyle = a.shout ? FontStyle.Italic : FontStyle.Normal;
                b.bg.color = a.shout ? new Color(1f, 0.93f, 0.78f, 0.95f) : new Color(1f, 0.99f, 0.95f, 0.95f);
                float s = Mathf.Clamp(9f / Mathf.Max(a.dist, 1f) + 0.45f, 0.6f, 1.05f);
                b.rt.localScale = Vector3.one * s;
                b.rt.anchoredPosition = new Vector2(sp.x, sp.y) / scale;
                b.rt.SetSiblingIndex(bubbles.Count - used);
            }
            for (int i = used; i < bubbles.Count; i++) bubbles[i].rt.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ helpers
        Image Panel(RectTransform parent, string name, Color c, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.GetComponent<Image>(); img.sprite = rounded; img.type = Image.Type.Sliced; img.color = c; img.raycastTarget = false;
            return img;
        }
        Text Label(RectTransform parent, string s, int size, Color c, TextAnchor anchor, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("Texto", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>(); t.font = font; t.fontSize = size; t.color = c; t.alignment = anchor; t.fontStyle = style; t.text = s; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.supportRichText = true;
            return t;
        }
        RectTransform Button(RectTransform parent, string label, Vector2 pos, Vector2 size, Vector2 pivot, Action click, Color bg, Color fg)
        {
            var img = Panel(parent, label, bg, pos, size, pivot);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>(); b.onClick.AddListener(() => click());
            var cb = b.colors; cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f); cb.pressedColor = new Color(0.8f, 0.8f, 0.8f); b.colors = cb;
            var t = Label(img.rectTransform, label, 19, fg, TextAnchor.MiddleCenter, FontStyle.Bold); Stretch(t.rectTransform, 4, 2, 4, 2);
            return img.rectTransform;
        }
        static void Stretch(RectTransform rt, float l, float t, float r, float b) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t); }
        static void Full(RectTransform rt, bool top) { rt.anchorMin = new Vector2(0, top ? 1 : 0); rt.anchorMax = new Vector2(1, top ? 1 : 0); rt.pivot = new Vector2(0.5f, top ? 1 : 0); rt.anchoredPosition = Vector2.zero; }

        static Sprite MakeRounded()
        {
            const int S = 32, R = 10;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(R - x - 0.5f, x + 0.5f - (S - R))), dy = Mathf.Max(0, Mathf.Max(R - y - 0.5f, y + 0.5f - (S - R)));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(R - d + 0.5f);
                    float edge = Mathf.Clamp01(R - d - 1.5f) < 1f && d > R - 2.5f ? 0.75f : 1f;
                    tex.SetPixel(x, y, new Color(edge, edge, edge, a));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(R + 1, R + 1, R + 1, R + 1));
        }
    }
}
