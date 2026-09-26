using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-contained, persistent (DontDestroyOnLoad) full-screen dark overlay that
/// represents "brightness" - no need to set this up again in every scene.
///
/// SETUP (only ONCE, in your first scene e.g. MainMenu):
/// 1. Create an empty GameObject named "PersistentOverlay", add a Canvas component
///    (Render Mode: Screen Space - Overlay), set "Sort Order" to something high
///    like 999 so it draws above every other Canvas in any scene.
/// 2. Add a Canvas Scaler component (UI Scale Mode: Scale With Screen Size, same
///    Reference Resolution as your other canvases, so it always covers the full
///    screen consistently).
/// 3. Add a Graphic Raycaster component (Unity usually adds it automatically with Canvas).
/// 4. As a child of "PersistentOverlay", create a full-screen Image ("OverlayImage"):
///    color black, Anchors stretched (0,0 to 1,1), Left/Top/Right/Bottom = 0,
///    Raycast Target OFF (so it never blocks clicks).
/// 5. Attach this script to "PersistentOverlay" (the root), drag "OverlayImage" into
///    the "Overlay Image" field below.
/// That's it - because THIS SCRIPT calls DontDestroyOnLoad on a ROOT GameObject
/// (the Canvas itself), the whole thing persists automatically into every scene
/// loaded afterwards, same as GameSettingsManager.
/// </summary>
public class PersistentBrightnessOverlay : MonoBehaviour
{
    private static PersistentBrightnessOverlay instance;

    [SerializeField] private Image overlayImage;
    [SerializeField] private float maxDarknessAlpha = 0.85f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (GameSettingsManager.Instance != null)
        {
            ApplyBrightness(GameSettingsManager.Instance.GetBrightness());
            GameSettingsManager.Instance.OnBrightnessChanged += ApplyBrightness;
        }
    }

    private void OnDestroy()
    {
        if (GameSettingsManager.Instance != null)
        {
            GameSettingsManager.Instance.OnBrightnessChanged -= ApplyBrightness;
        }
    }

    private void ApplyBrightness(float brightness01)
    {
        if (overlayImage == null) return;
        float alpha = (1f - brightness01) * maxDarknessAlpha;
        Color c = overlayImage.color;
        c.a = alpha;
        overlayImage.color = c;
    }
}
