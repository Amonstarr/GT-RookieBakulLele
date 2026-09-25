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

namespace GT.Leaderboard.Editor
{
    /// <summary>
    /// Utility editor untuk membangun UI scene Leaderboard secara otomatis (placeholder).
    /// Semua elemen memakai warna/konten sederhana sebagai placeholder sehingga mudah
    /// diganti dengan aset desain milik user.
    /// </summary>
    public static class LeaderboardSceneSetupUtility
    {
        private const string ROW_PREFAB_PATH = "Assets/Prefabs/LeaderboardRow.prefab";
        private const string LEADERBOARD_SCENE_PATH = "Assets/Scenes/Leaderboard.unity";
        private const string PREFABS_FOLDER = "Assets/Prefabs";

        private static readonly Color BackgroundColor = new Color(0.08f, 0.09f, 0.16f, 1f);
        private static readonly Color PanelColor = new Color(0.14f, 0.15f, 0.26f, 0.95f);
        private static readonly Color AccentColor = new Color(0.18f, 0.22f, 0.45f, 1f);
        private static readonly Color TitleGold = new Color(1f, 0.84f, 0.24f, 1f);

        private static TMP_FontAsset defaultFont;
        private static TMP_FontAsset titleFont;

        [MenuItem("Tools/Leaderboard/Build Scene UI")]
        public static void BuildSceneUI()
        {
            if (EditorSceneManager.GetActiveScene().name != "Leaderboard")
            {
                bool proceed = EditorUtility.DisplayDialog("Leaderboard Setup",
                    "Scene aktif bukan 'Leaderboard'.\n\nDisarankan membuka Assets/Scenes/Leaderboard.unity terlebih dahulu.\nLanjutkan di scene aktif?",
                    "Ya, lanjutkan", "Batal");
                if (!proceed) return;
            }

            if (Object.FindFirstObjectByType<LeaderboardUI>() != null)
            {
                EditorUtility.DisplayDialog("Leaderboard Setup",
                    "LeaderboardUI sudah ada di scene. Hapus komponen/object LeaderboardUI lama jika ingin membangun ulang.", "OK");
                return;
            }

            ResolveFonts();

            Canvas canvas = EnsureCanvas();
            EnsureEventSystem();

            // ===== Background =====
            Image bg = CreateImage("Background", canvas.transform, BackgroundColor);
            StretchFull(bg.rectTransform);

            // ===== Title =====
            TextMeshProUGUI title = CreateText("TitleText", canvas.transform, "PAPAN PERINGKAT", 72,
                TextAlignmentOptions.Center, TitleGold, titleFont);
            StretchTop(title.rectTransform, 45f, 100f);

            // ===== Ringkasan peringkat player =====
            TextMeshProUGUI playerRankText = CreateText("PlayerRankText", canvas.transform, "Peringkat kamu: #0", 44,
                TextAlignmentOptions.Center, Color.white, defaultFont);
            StretchTop(playerRankText.rectTransform, 165f, 70f);

            TextMeshProUGUI playerScoreText = CreateText("PlayerScoreText", canvas.transform, "Skor: 0 poin", 34,
                TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.9f, 1f), defaultFont);
            StretchTop(playerScoreText.rectTransform, 230f, 55f);

            // ===== ScrollView daftar skor =====
            Transform content = CreateScrollView(canvas.transform);

            // ===== Row prefab =====
            GameObject rowPrefab = CreateAndSaveRowPrefab(content);

            // ===== State UI (loading / feedback / retry) =====
            GameObject loadingIndicator = CreateText("LoadingIndicator", canvas.transform, "Memuat papan peringkat...", 36,
                TextAlignmentOptions.Center, Color.gray, defaultFont).gameObject;
            PlaceCenter(loadingIndicator.GetComponent<RectTransform>(), new Vector2(0f, -360f), new Vector2(500f, 50f));

            TextMeshProUGUI feedbackText = CreateText("FeedbackText", canvas.transform, "", 32,
                TextAlignmentOptions.Center, Color.red, defaultFont);
            PlaceCenter(feedbackText.rectTransform, new Vector2(0f, -420f), new Vector2(800f, 50f));
            feedbackText.gameObject.SetActive(false);

            Button retryButton = CreateButton("RetryButton", "Coba Lagi", canvas.transform, 34);
            PlaceCenter(retryButton.GetComponent<RectTransform>(), new Vector2(0f, -485f), new Vector2(240f, 64f));

            Button mainMenuButton = CreateButton("MainMenuButton", "Kembali ke Menu", canvas.transform, 36);
            RectTransform mmRt = mainMenuButton.GetComponent<RectTransform>();
            mmRt.anchorMin = new Vector2(0.5f, 0f);
            mmRt.anchorMax = new Vector2(0.5f, 0f);
            mmRt.pivot = new Vector2(0.5f, 0f);
            mmRt.anchoredPosition = new Vector2(0f, 40f);
            mmRt.sizeDelta = new Vector2(300f, 68f);

            LoadSceneOnClick sceneNav = mainMenuButton.gameObject.AddComponent<LoadSceneOnClick>();

            // ===== Manager & UI Controller =====
            LeaderboardManager manager = EnsureLeaderboardManager();
            LeaderboardUI ui = canvas.gameObject.AddComponent<LeaderboardUI>();

            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("leaderboardManager").objectReferenceValue = manager;
            so.FindProperty("topEntriesToShow").intValue = 10;
            so.FindProperty("rowsParent").objectReferenceValue = content;
            so.FindProperty("rowPrefab").objectReferenceValue = rowPrefab;
            so.FindProperty("playerRankText").objectReferenceValue = playerRankText;
            so.FindProperty("playerScoreText").objectReferenceValue = playerScoreText;
            so.FindProperty("loadingIndicator").objectReferenceValue = loadingIndicator;
            so.FindProperty("feedbackText").objectReferenceValue = feedbackText;
            so.FindProperty("retryButton").objectReferenceValue = retryButton;
            so.ApplyModifiedProperties();

            AddSceneToBuildSettings();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Leaderboard Setup",
                "Scene UI Leaderboard berhasil dibangun (placeholder).\n\n- Ganti warna/teks/Aset sesuai desain kamu.\n- Isi 'Leaderboard Id' pada komponen LeaderboardManager.", "OK");
        }

        [MenuItem("Tools/Leaderboard/Add LeaderboardManager to Current Scene")]
        public static void AddLeaderboardManagerToScene()
        {
            if (Object.FindFirstObjectByType<LeaderboardManager>() != null)
            {
                EditorUtility.DisplayDialog("Leaderboard Setup", "LeaderboardManager sudah ada di scene ini.", "OK");
                return;
            }

            GameObject go = new GameObject("LeaderboardManager");
            go.AddComponent<LeaderboardManager>();
            Undo.RegisterCreatedObjectUndo(go, "Add LeaderboardManager");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Leaderboard Setup", "LeaderboardManager ditambahkan. Isi 'Leaderboard Id' sesuai ID di Dashboard.", "OK");
        }

        #region Font & Primitives

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
            go.transform.SetParent(parent, false);
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

        private static Button CreateButton(string name, string label, Transform parent, int fontSize)
        {
            GameObject go = CreateUIObject(name, parent);
            Image img = go.AddComponent<Image>();
            img.color = AccentColor;
            img.raycastTarget = true;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            TextMeshProUGUI txt = CreateText("Text", go.transform, label, fontSize, TextAlignmentOptions.Center, Color.white, defaultFont);
            StretchFull(txt.rectTransform);
            return btn;
        }

        #endregion

        #region Rect Helpers

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void StretchTop(RectTransform rt, float topOffset, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -topOffset);
            rt.sizeDelta = new Vector2(0f, height);
        }

        private static void PlaceCenter(RectTransform rt, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        #endregion

        #region Scene Objects

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

        private static LeaderboardManager EnsureLeaderboardManager()
        {
            LeaderboardManager existing = Object.FindFirstObjectByType<LeaderboardManager>();
            if (existing != null) return existing;

            GameObject mgrGO = new GameObject("LeaderboardManager");
            return mgrGO.AddComponent<LeaderboardManager>();
        }

        private static Transform CreateScrollView(Transform canvas)
        {
            GameObject root = CreateUIObject("LeaderboardScrollView", canvas);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.12f);
            rootRt.anchorMax = new Vector2(0.5f, 0.82f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = Vector2.zero;
            rootRt.sizeDelta = new Vector2(880f, 0f);

            Image rootImage = root.AddComponent<Image>();
            rootImage.color = PanelColor;

            ScrollRect scrollRect = root.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 30f;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.verticalScrollbar = null;

            GameObject viewportGO = CreateUIObject("Viewport", root.transform);
            RectTransform viewportRt = viewportGO.GetComponent<RectTransform>();
            StretchFull(viewportRt);
            Image viewportImage = viewportGO.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.15f);
            viewportGO.AddComponent<RectMask2D>();

            GameObject contentGO = CreateUIObject("Content", viewportGO.transform);
            RectTransform contentRt = contentGO.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(14, 14, 14, 14);
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = contentGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;

            return contentRt;
        }

        private static GameObject CreateAndSaveRowPrefab(Transform content)
        {
            GameObject row = CreateUIObject("Row_Template", content);
            row.AddComponent<LayoutElement>().preferredHeight = 60f;

            Image rowBg = CreateImage("Background", row.transform, new Color(1f, 1f, 1f, 0.08f));
            StretchFull(rowBg.rectTransform);

            TextMeshProUGUI rankText = CreateText("RankText", row.transform, "#0", 32,
                TextAlignmentOptions.MidlineLeft, TitleGold, defaultFont);
            RectTransform rankRt = rankText.rectTransform;
            rankRt.anchorMin = new Vector2(0f, 0.5f);
            rankRt.anchorMax = new Vector2(0f, 0.5f);
            rankRt.pivot = new Vector2(0f, 0.5f);
            rankRt.anchoredPosition = new Vector2(50f, 0f);
            rankRt.sizeDelta = new Vector2(100f, 40f);

            TextMeshProUGUI nameText = CreateText("NameText", row.transform, "Pemain Baru", 30,
                TextAlignmentOptions.MidlineLeft, Color.white, defaultFont);
            RectTransform nameRt = nameText.rectTransform;
            nameRt.anchorMin = new Vector2(0f, 0.5f);
            nameRt.anchorMax = new Vector2(1f, 0.5f);
            nameRt.pivot = new Vector2(0.5f, 0.5f);
            nameRt.offsetMin = new Vector2(165f, -20f);
            nameRt.offsetMax = new Vector2(-155f, 20f);

            TextMeshProUGUI scoreText = CreateText("ScoreText", row.transform, "0", 30,
                TextAlignmentOptions.MidlineRight, new Color(0.85f, 0.85f, 0.9f, 1f), defaultFont);
            RectTransform scoreRt = scoreText.rectTransform;
            scoreRt.anchorMin = new Vector2(1f, 0.5f);
            scoreRt.anchorMax = new Vector2(1f, 0.5f);
            scoreRt.pivot = new Vector2(1f, 0.5f);
            scoreRt.anchoredPosition = new Vector2(-50f, 0f);
            scoreRt.sizeDelta = new Vector2(110f, 40f);

            LeaderboardRowUI rowScript = row.AddComponent<LeaderboardRowUI>();
            SerializedObject so = new SerializedObject(rowScript);
            so.FindProperty("rankText").objectReferenceValue = rankText;
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("scoreText").objectReferenceValue = scoreText;
            so.FindProperty("backgroundImage").objectReferenceValue = rowBg;
            so.ApplyModifiedProperties();

            row.transform.SetParent(null, false);
            if (!Directory.Exists(PREFABS_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
                AssetDatabase.Refresh();
            }

            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(row, ROW_PREFAB_PATH);
            Object.DestroyImmediate(row);
            return prefabAsset;
        }

        private static void AddSceneToBuildSettings()
        {
            if (!File.Exists(LEADERBOARD_SCENE_PATH)) return;

            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.All(s => s.path != LEADERBOARD_SCENE_PATH))
            {
                scenes.Add(new EditorBuildSettingsScene(LEADERBOARD_SCENE_PATH, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        #endregion
    }
}
#endif