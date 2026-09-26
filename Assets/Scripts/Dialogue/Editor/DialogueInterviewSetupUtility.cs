#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GT.Dialogue.UI;
using GT.Dialogue.Core;

namespace GT.Dialogue.Editor
{
    /// <summary>
    /// Utility editor untuk membuat dan mengonfigurasi komponen UI CV Paper & CV Modal di scene DialogueInterview secara otomatis.
    /// Akses lewat menu: Tools -> Dialogue -> Setup CV Inspection UI
    /// </summary>
    public static class DialogueInterviewSetupUtility
    {
        [MenuItem("Tools/Dialogue/Setup CV Inspection UI")]
        public static void SetupCVInspectionUI()
        {
            var ui = Object.FindFirstObjectByType<DialogueInterviewUI>();
            if (ui == null)
            {
                EditorUtility.DisplayDialog("Dialogue Setup Error", 
                    "DialogueInterviewUI tidak ditemukan di scene saat ini!\nPastikan Anda sedang membuka scene DialogueInterview.", "OK");
                return;
            }

            Canvas canvas = ui.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Dialogue Setup Error", "Canvas tidak ditemukan!", "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Setup CV Inspection UI");

            // 1. Setup CV Paper Button di Kiri Bawah
            Transform paperTransform = canvas.transform.Find("CVPaperContainer");
            GameObject paperObj;
            if (paperTransform == null)
            {
                paperObj = new GameObject("CVPaperContainer", typeof(RectTransform), typeof(Image), typeof(Button));
                paperObj.transform.SetParent(canvas.transform, false);
            }
            else
            {
                paperObj = paperTransform.gameObject;
            }

            RectTransform paperRect = paperObj.GetComponent<RectTransform>();
            paperRect.anchorMin = new Vector2(0f, 0f);
            paperRect.anchorMax = new Vector2(0f, 0f);
            paperRect.pivot = new Vector2(0f, 0f);
            paperRect.anchoredPosition = new Vector2(40f, 40f);
            paperRect.sizeDelta = new Vector2(170f, 130f);

            Image paperImg = paperObj.GetComponent<Image>();
            paperImg.color = new Color(0.95f, 0.92f, 0.84f, 1f); // Warna kertas dokumen krem hangat

            Button paperBtn = paperObj.GetComponent<Button>();
            ColorBlock paperColors = paperBtn.colors;
            paperColors.normalColor = Color.white;
            paperColors.highlightedColor = new Color(1f, 1f, 0.8f, 1f);
            paperColors.pressedColor = new Color(0.85f, 0.82f, 0.75f, 1f);
            paperBtn.colors = paperColors;

            // Label pada kertas
            Transform paperLabelTransform = paperObj.transform.Find("Label");
            GameObject paperLabelObj;
            if (paperLabelTransform == null)
            {
                paperLabelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                paperLabelObj.transform.SetParent(paperObj.transform, false);
            }
            else
            {
                paperLabelObj = paperLabelTransform.gameObject;
            }

            RectTransform labelRect = paperLabelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 8f);
            labelRect.offsetMax = new Vector2(-8f, -8f);

            TextMeshProUGUI paperLabel = paperLabelObj.GetComponent<TextMeshProUGUI>();
            paperLabel.text = "<size=130%>📄</size>\n<b>BERKAS CV</b>\n<size=75%><i>(Klik Meja)</i></size>";
            paperLabel.alignment = TextAlignmentOptions.Center;
            paperLabel.color = new Color(0.2f, 0.15f, 0.1f, 1f);
            paperLabel.raycastTarget = false;

            // 2. Setup CV Modal Pop-up (Tengah Layar)
            Transform modalTransform = canvas.transform.Find("CVModalPanel");
            GameObject modalObj;
            if (modalTransform == null)
            {
                modalObj = new GameObject("CVModalPanel", typeof(RectTransform), typeof(Image));
                modalObj.transform.SetParent(canvas.transform, false);
            }
            else
            {
                modalObj = modalTransform.gameObject;
            }

            RectTransform modalRect = modalObj.GetComponent<RectTransform>();
            modalRect.anchorMin = Vector2.zero;
            modalRect.anchorMax = Vector2.one;
            modalRect.offsetMin = Vector2.zero;
            modalRect.offsetMax = Vector2.zero;

            Image modalBgImg = modalObj.GetComponent<Image>();
            modalBgImg.color = new Color(0.05f, 0.05f, 0.08f, 0.88f); // Overlay redup belakang

            // Frame Dokumen CV
            Transform docFrameTransform = modalObj.transform.Find("DocumentCard");
            GameObject docCardObj;
            if (docFrameTransform == null)
            {
                docCardObj = new GameObject("DocumentCard", typeof(RectTransform), typeof(Image));
                docCardObj.transform.SetParent(modalObj.transform, false);
            }
            else
            {
                docCardObj = docFrameTransform.gameObject;
            }

            RectTransform docRect = docCardObj.GetComponent<RectTransform>();
            docRect.anchorMin = new Vector2(0.5f, 0.5f);
            docRect.anchorMax = new Vector2(0.5f, 0.5f);
            docRect.pivot = new Vector2(0.5f, 0.5f);
            docRect.anchoredPosition = new Vector2(0f, 30f);
            docRect.sizeDelta = new Vector2(620f, 750f);

            Image docCardImg = docCardObj.GetComponent<Image>();
            docCardImg.color = new Color(0.97f, 0.95f, 0.90f, 1f);

            // Gambar Tampilan CV (cvDisplayImage)
            Transform cvImgTransform = docCardObj.transform.Find("CVDisplayImage");
            GameObject cvImgObj;
            if (cvImgTransform == null)
            {
                cvImgObj = new GameObject("CVDisplayImage", typeof(RectTransform), typeof(Image));
                cvImgObj.transform.SetParent(docCardObj.transform, false);
            }
            else
            {
                cvImgObj = cvImgTransform.gameObject;
            }

            RectTransform cvDisplayRect = cvImgObj.GetComponent<RectTransform>();
            cvDisplayRect.anchorMin = Vector2.zero;
            cvDisplayRect.anchorMax = Vector2.one;
            cvDisplayRect.offsetMin = new Vector2(15f, 15f);
            cvDisplayRect.offsetMax = new Vector2(-15f, -15f);

            Image cvDisplayImage = cvImgObj.GetComponent<Image>();
            cvDisplayImage.preserveAspect = true;

            // Tombol "Mulai Wawancara"
            Transform startBtnTransform = modalObj.transform.Find("StartInterviewButton");
            GameObject startBtnObj;
            if (startBtnTransform == null)
            {
                startBtnObj = new GameObject("StartInterviewButton", typeof(RectTransform), typeof(Image), typeof(Button));
                startBtnObj.transform.SetParent(modalObj.transform, false);
            }
            else
            {
                startBtnObj = startBtnTransform.gameObject;
            }

            RectTransform startBtnRect = startBtnObj.GetComponent<RectTransform>();
            startBtnRect.anchorMin = new Vector2(0.5f, 0.5f);
            startBtnRect.anchorMax = new Vector2(0.5f, 0.5f);
            startBtnRect.pivot = new Vector2(0.5f, 0.5f);
            startBtnRect.anchoredPosition = new Vector2(0f, -390f);
            startBtnRect.sizeDelta = new Vector2(300f, 65f);

            Image startBtnImg = startBtnObj.GetComponent<Image>();
            startBtnImg.color = new Color(0.18f, 0.72f, 0.36f, 1f); // Hijau segar

            Button startBtn = startBtnObj.GetComponent<Button>();

            // Teks Tombol Mulai
            Transform startTextTransform = startBtnObj.transform.Find("Text");
            GameObject startTextObj;
            if (startTextTransform == null)
            {
                startTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                startTextObj.transform.SetParent(startBtnObj.transform, false);
            }
            else
            {
                startTextObj = startTextTransform.gameObject;
            }

            RectTransform startTextRect = startTextObj.GetComponent<RectTransform>();
            startTextRect.anchorMin = Vector2.zero;
            startTextRect.anchorMax = Vector2.one;
            startTextRect.offsetMin = Vector2.zero;
            startTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI startText = startTextObj.GetComponent<TextMeshProUGUI>();
            startText.text = "<b>MULAI WAWANCARA</b>";
            startText.alignment = TextAlignmentOptions.Center;
            startText.color = Color.white;
            startText.fontSize = 24f;
            startText.raycastTarget = false;

            // Tombol Tutup (X) di pojok kartu
            Transform closeBtnTransform = docCardObj.transform.Find("CloseButton");
            GameObject closeBtnObj;
            if (closeBtnTransform == null)
            {
                closeBtnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
                closeBtnObj.transform.SetParent(docCardObj.transform, false);
            }
            else
            {
                closeBtnObj = closeBtnTransform.gameObject;
            }

            RectTransform closeBtnRect = closeBtnObj.GetComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(1f, 1f);
            closeBtnRect.anchorMax = new Vector2(1f, 1f);
            closeBtnRect.pivot = new Vector2(1f, 1f);
            closeBtnRect.anchoredPosition = new Vector2(-10f, -10f);
            closeBtnRect.sizeDelta = new Vector2(40f, 40f);

            Image closeBtnImg = closeBtnObj.GetComponent<Image>();
            closeBtnImg.color = new Color(0.85f, 0.25f, 0.25f, 1f);

            Button closeBtn = closeBtnObj.GetComponent<Button>();

            Transform closeTextTransform = closeBtnObj.transform.Find("Text");
            GameObject closeTextObj;
            if (closeTextTransform == null)
            {
                closeTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                closeTextObj.transform.SetParent(closeBtnObj.transform, false);
            }
            else
            {
                closeTextObj = closeTextTransform.gameObject;
            }

            RectTransform closeTextRect = closeTextObj.GetComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI closeText = closeTextObj.GetComponent<TextMeshProUGUI>();
            closeText.text = "<b>X</b>";
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.color = Color.white;
            closeText.fontSize = 20f;
            closeText.raycastTarget = false;

            // Set modal aktif false secara default saat awal
            modalObj.SetActive(false);

            // 3. Sambungkan Referensi ke SerializedFields di DialogueInterviewUI
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("cvPaperContainer").objectReferenceValue = paperObj;
            so.FindProperty("cvPaperButton").objectReferenceValue = paperBtn;
            so.FindProperty("cvModalPanel").objectReferenceValue = modalObj;
            so.FindProperty("cvDisplayImage").objectReferenceValue = cvDisplayImage;
            so.FindProperty("startInterviewButton").objectReferenceValue = startBtn;
            so.FindProperty("closeCVButton").objectReferenceValue = closeBtn;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[DialogueInterviewSetupUtility] Sukses menata & menyambungkan UI CV Paper dan CV Modal!");
            EditorUtility.DisplayDialog("Dialogue Setup Success", 
                "Berhasil mengonfigurasi UI Kertas CV (kiri bawah) dan Modal CV (tengah)!\n\nReferensi telah terhubung otomatis ke DialogueInterviewUI.", "Mantap!");
        }
    }
}
#endif
