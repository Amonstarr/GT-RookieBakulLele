using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class CutsceneController : MonoBehaviour
{
    [System.Serializable]
    public class CutsceneSlide
    {
        public Sprite image;
        [TextArea(3, 6)] public string narration;
    }

    [Header("Slides (urutan sesuai array)")]
    public CutsceneSlide[] slides;

    [Header("References")]
    public Image bgImage;
    public TMP_Text narrationText;
    public CanvasGroup fadeCanvasGroup;
    public Button nextButton;
    public string nextSceneName = "CharacterSelectionTycoon";

    [Header("Settings")]
    public float wordDelay = 0.15f;
    public float fadeDuration = 0.5f;

    private int currentIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    void Start()
    {
        nextButton.onClick.AddListener(OnNextButtonPressed);
        ShowSlide(0);
    }

    void Update()
    {
        
        if (!isTyping) return;

        bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool keyPressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;

        if ((mouseClicked || keyPressed) && !IsPointerOverNextButton())
        {
            CompleteTyping();
        }
    }

    bool IsPointerOverNextButton()
    {
        if (Mouse.current == null) return false;

        RectTransform rect = nextButton.GetComponent<RectTransform>();
        Vector2 mousePos = Mouse.current.position.ReadValue();

      
        return RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, null);
    }

    void ShowSlide(int index)
    {
        if (index >= slides.Length)
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        currentIndex = index;
        StopAllCoroutines();
        StartCoroutine(FadeAndShow(slides[index]));
    }

    IEnumerator FadeAndShow(CutsceneSlide slide)
    {
        yield return StartCoroutine(Fade(1f));
        bgImage.sprite = slide.image;
        narrationText.text = "";
        yield return StartCoroutine(Fade(0f));
        typingCoroutine = StartCoroutine(TypeWords(slide.narration));
    }

    IEnumerator Fade(float target)
    {
        float start = fadeCanvasGroup.alpha;
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = target;
    }

    IEnumerator TypeWords(string text)
    {
        isTyping = true;
        narrationText.text = "";
        string[] words = text.Split(' ');

        for (int i = 0; i < words.Length; i++)
        {
            narrationText.text += (i == 0 ? "" : " ") + words[i];
            yield return new WaitForSeconds(wordDelay);
        }
        isTyping = false;
    }

    void CompleteTyping()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        narrationText.text = slides[currentIndex].narration;
        isTyping = false;
    }

    void OnNextButtonPressed()
    {
        if (isTyping)
        {
           
            CompleteTyping();
        }
        else
        {
            ShowSlide(currentIndex + 1);
        }
    }
}