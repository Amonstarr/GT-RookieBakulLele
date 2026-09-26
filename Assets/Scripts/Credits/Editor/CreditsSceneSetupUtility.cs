#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace GT.Credits.Editor
{
    /// <summary>
    /// Utility editor untuk membangun scene Credit otomatis (gaya credit film):
    /// background gelap, viewport mask penuh layar, konten scroll, serta prefab baris/header.
    /// User cukup mengisi nama & role pada aset CreditsContentSO.
    /// </summary>
    public static class CreditsSceneSetupUtility
    {
        private const string CREDITS_SCENE_PATH = "Assets/Scenes/Credits.unity";
        private const string LEADERBOARD_SCENE_PATH = "Assets/Scenes/Leaderboard.unity";
        private const string CREDIT_LIST_PATH = "Assets/ScriptableObjects/CreditsContent.asset";
        private const string SCRIPTABLE_OBJECTS_FOLDER = "Assets/ScriptableObjects";
        private const string PREFABS_FOLDER = "Assets/Prefabs";

        private static readonly Color BackgroundColor = new Color(0.03f, 0.04f, 0.07f, 1f);
        private static readonly Color TitleGold = new Color(1f, 0.84f, 0.24f, 1f);
        private static readonly Color NameColor = Color.white;
        private static readonly Color RoleColor = new Color(0.75f, 0.75f, 0.8f, 1f);

        private static TMP_FontAsset defaultFont;
        private static TMP_FontAsset titleFont;

        [MenuItem("Tools/Credits/Build Scene UI")]
        public static void BuildSceneUI()
        {
            EnsureCreditsSceneOpen();

            if (Object.FindFirstObjectByType<CreditsScrollController>() != null)
            {
                EditorUtility.DisplayDialog("Credits Setup",
                    "CreditsScrollController sudah ada di scene. Hapus object/komponen lama jika ingin membangun ulang.", "OK");
                return;
            }

            ResolveFonts();

            Canvas canvas = EnsureCanvas();
            EnsureEventSystem();

            Image bg = CreateImage("Background", canvas.transform, BackgroundColor);
            StretchFull(bg.rectTransform);

            GameObject directorGO = new GameObject("CreditsDirector");
            CreditsScrollController controller = directorGO.AddComponent<CreditsScrollController>();

            TextMeshProUGUI hintText = CreateText("HintText", canvas.transform,
                "Tekan tombol apa saja untuk melewati", 24, TextAlignmentOptions.Bottom, new Color(0.55f, 0.55f, 0.6f, 1f), defaultFont);
            StretchBottom(hintText.rectTransform, 24f, 36f);

            RectTransform viewportRT = CreateViewport(canvas.transform);
            RectTransform contentRT = CreateCreditsContent(viewportRT);

            GameObject rowPrefab = CreateAndSavePrefab("CreditRow", CreateRow);
            GameObject headerPrefab = CreateAndSavePrefab("CreditHeader", () => CreateHeader(38, TitleGold));
            GameObject titlePrefab = CreateAndSavePrefab("CreditTitle", () => CreateHeader(72, TitleGold));

            CreditsContentSO content = EnsureCreditsAsset();

            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("content").objectReferenceValue = content;
            so.FindProperty("viewport").objectReferenceValue = viewportRT;
            so.FindProperty("contentTransform").objectReferenceValue = contentRT;
            so.FindProperty("titlePrefab").objectReferenceValue = titlePrefab;
            so.FindProperty("headerPrefab").objectReferenceValue = headerPrefab;
            so.FindProperty("rowPrefab").objectReferenceValue = rowPrefab;
            so.FindProperty("scrollSpeed").floatValue = 120f;
            so.FindProperty("endHoldSeconds").floatValue = 1.5f;
            so.FindProperty("exitSceneName").stringValue = "Leaderboard";
            so.FindProperty("skipOnAnyInput").boolValue = true;
            so.ApplyModifiedProperties();

            AddSceneToBuildSettings();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Credits Setup",
                "Scene Credit berhasil dibangun.\n\nSekarang buka aset 'CreditsContent.asset' (Assets/ScriptableObjects) " +
                "dan isi nama & role tiap bagian. Setelah Tycoon selesai, game masuk ke Credits lalu otomatis lanjut ke Leaderboard.", "OK");
        }

        [MenuItem("Tools/Credits/Create Credit List")]
        public static void CreateCreditListAsset()
        {
            CreditsContentSO asset = EnsureCreditsAsset();
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }

        #region Data Asset

        private static CreditsContentSO EnsureCreditsAsset()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:CreditsContentSO"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<CreditsContentSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) return asset;
            }

            if (!Directory.Exists(SCRIPTABLE_OBJECTS_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
                AssetDatabase.Refresh();
            }

            CreditsContentSO so = ScriptableObject.CreateInstance<CreditsContentSO>();
            so.gameTitle = "ROOKIE BAKUL LELE";
            so.thanksMessage = "TERIMA KASIH SUDAH BERMAIN!";

            so.sections.Add(new CreditsSection
            {
                sectionTitle = "Tim Pengembang",
                entries = { new CreditEntry { name = "Nama Pembuat", role = "Role" } }
            });

            AssetDatabase.CreateAsset(so, CREDIT_LIST_PATH);
            AssetDatabase.SaveAssets();
            return so;
        }

        #endregion

        #region Scene Objects

        private static void EnsureCreditsSceneOpen()
        {
            Scene active = EditorSceneManager.GetActiveScene();
            if (active.path == CREDITS_SCENE_PATH) return;

            if (File.Exists(CREDITS_SCENE_PATH))
            {
                EditorSceneManager.OpenScene(CREDITS_SCENE_PATH);
            }
            else
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), CREDITS_SCENE_PATH);
            }
        }

        private static Canvas EnsureCanvas()
        {
            Canvas existing = Object.FindFirstObjectByType<Canvas>();
            if (existing != null) return existing;

            GameObject canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static EventSystem EnsureEventSystem()
        {
            EventSystem existing = Object.FindFirstObjectByType<EventSystem>();
            if (existing == null)
            {
                GameObject esGO = new GameObject("EventSystem", typeof(EventSystem));
                existing = esGO.GetComponent<EventSystem>();
            }

            var legacy = existing.GetComponent<StandaloneInputModule>();
            if (legacy != null) Object.DestroyImmediate(legacy);

            if (existing.GetComponent<InputSystemUIInputModule>() == null)
            {
                existing.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            return existing;
        }

        private static RectTransform CreateViewport(Transform canvas)
        {
            GameObject viewportGO = CreateUIObject("CreditsViewport", canvas);
            Image viewportImage = viewportGO.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
            viewportGO.AddComponent<RectMask2D>();

            RectTransform viewportRT = viewportGO.GetComponent<RectTransform>();
            StretchFull(viewportRT);
            return viewportRT;
        }

        private static RectTransform CreateCreditsContent(RectTransform viewport)
        {
            GameObject contentGO = CreateUIObject("CreditContent", viewport);
            RectTransform contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(80, 80, 140, 320);
            vlg.spacing = 18f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            ContentSizeFitter csf = contentGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            return contentRT;
        }

        private static GameObject CreateRow()
        {
            GameObject row = CreateUIObject("Row_Template", null);
            row.AddComponent<LayoutElement>().preferredHeight = 120f;

            RectTransform rowRT = row.GetComponent<RectTransform>();
            rowRT.sizeDelta = new Vector2(0f, 120f);

            TextMeshProUGUI nameText = CreateText("NameText", row.transform, "Nama Pembuat", 42,
                TextAlignmentOptions.Top, NameColor, defaultFont);
            RectTransform nameRT = nameText.rectTransform;
            nameRT.anchorMin = new Vector2(0f, 1f);
            nameRT.anchorMax = new Vector2(1f, 1f);
            nameRT.pivot = new Vector2(0.5f, 1f);
            nameRT.anchoredPosition = new Vector2(0f, -8f);
            nameRT.sizeDelta = new Vector2(0f, 56f);

            TextMeshProUGUI roleText = CreateText("RoleText", row.transform, "Role", 26,
                TextAlignmentOptions.Top, RoleColor, defaultFont);
            RectTransform roleRT = roleText.rectTransform;
            roleRT.anchorMin = new Vector2(0f, 1f);
            roleRT.anchorMax = new Vector2(1f, 1f);
            roleRT.pivot = new Vector2(0.5f, 1f);
            roleRT.anchoredPosition = new Vector2(0f, -70f);
            roleRT.sizeDelta = new Vector2(0f, 40f);

            CreditRowUI rowUI = row.AddComponent<CreditRowUI>();
            var so = new SerializedObject(rowUI);
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("roleText").objectReferenceValue = roleText;
            so.ApplyModifiedProperties();

            return row;
        }

        private static GameObject CreateHeader(float fontSize, Color color)
        {
            GameObject header = CreateUIObject("Header_Template", null);
            header.AddComponent<LayoutElement>().preferredHeight = 72f;

            RectTransform headerRT = header.GetComponent<RectTransform>();
            headerRT.sizeDelta = new Vector2(0f, 72f);

            TextMeshProUGUI text = CreateText("HeaderText", header.transform, "JUDUL BAGIAN", (int)fontSize,
                TextAlignmentOptions.Center, color, defaultFont);
            StretchFull(text.rectTransform);

            CreditHeaderUI headerUI = header.AddComponent<CreditHeaderUI>();
            var so = new SerializedObject(headerUI);
            so.FindProperty("headerText").objectReferenceValue = text;
            so.ApplyModifiedProperties();

            return header;
        }

        private static GameObject CreateAndSavePrefab(string prefabName, System.Func<GameObject> builder)
        {
            GameObject go = builder();
            go.name = prefabName;

            if (!Directory.Exists(PREFABS_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
                AssetDatabase.Refresh();
            }

            string path = PREFABS_FOLDER + "/" + prefabName + ".prefab";
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefabAsset;
        }

        private static void AddSceneToBuildSettings()
        {
            if (!File.Exists(CREDITS_SCENE_PATH)) return;

            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == CREDITS_SCENE_PATH)) return;

            int leaderboardIndex = scenes.FindIndex(s => s.path == LEADERBOARD_SCENE_PATH);
            var entry = new EditorBuildSettingsScene(CREDITS_SCENE_PATH, true);

            if (leaderboardIndex >= 0)
            {
                scenes.Insert(leaderboardIndex, entry);
            }
            else
            {
                scenes.Add(entry);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        #endregion

        #region Primitives & Helpers

        private static void ResolveFonts()
        {
            defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont == null)
            {
                foreach (var guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
                {
                    var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
                    if (font != null) { defaultFont = font; break; }
                }
            }

            titleFont = FindFontAsset("JockeyOne-Regular SDF");
            if (titleFont == null) titleFont = defaultFont;
        }

        private static TMP_FontAsset FindFontAsset(string nameFragment)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains(nameFragment))
                {
                    return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                }
            }
            return null;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            Image img = CreateUIObject(name, parent).AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, int fontSize,
            TextAlignmentOptions alignment, Color color, TMP_FontAsset font)
        {
            TextMeshProUGUI tmp = CreateUIObject(name, parent).AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            if (font != null) tmp.font = font;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void StretchBottom(RectTransform rt, float bottomOffset, float height)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, bottomOffset);
            rt.sizeDelta = new Vector2(0f, height);
        }

        #endregion
    }
}
#endif
