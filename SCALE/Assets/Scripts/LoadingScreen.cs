using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// A full-screen cover that sits above the game UI. Builds its own canvas so it
// needs no wiring, and runs on unscaled time so it still animates while the game
// is paused for a load.
public class LoadingScreen : MonoBehaviour
{
    [Tooltip("Shown while a chamber is loading.")]
    public string message = "LOADING...";

    [Tooltip("Colour the screen is covered with.")]
    public Color coverColor = Color.black;

    [Tooltip("Size of the loading text.")]
    public float fontSize = 48f;

    [Tooltip("Sorting order of the loading canvas. Has to sit above the game UI.")]
    public int sortingOrder = 1000;

    private CanvasGroup group;
    private TextMeshProUGUI label;

    public static LoadingScreen Build(Transform parent)
    {
        GameObject holder = new("LoadingScreen");
        holder.transform.SetParent(parent, false);
        return holder.AddComponent<LoadingScreen>();
    }

    private void Awake()
    {
        BuildUi();
    }

    public void Cover(bool showMessage)
    {
        BuildUi();

        group.alpha = 1f;
        label.gameObject.SetActive(showMessage);
    }

    public IEnumerator Reveal(float duration)
    {
        BuildUi();

        float start = group.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, 0f, elapsed / duration);
            yield return null;
        }

        group.alpha = 0f;
        label.gameObject.SetActive(false);
    }

    private void BuildUi()
    {
        if (group != null)
        {
            return;
        }

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject coverObject = new("Cover", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        coverObject.transform.SetParent(transform, false);

        Image cover = coverObject.GetComponent<Image>();
        cover.color = coverColor;
        cover.raycastTarget = false;
        Stretch(cover.rectTransform);

        GameObject labelObject = new("Message", typeof(RectTransform));
        labelObject.transform.SetParent(transform, false);

        label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = message;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.raycastTarget = false;
        Stretch(label.rectTransform);
        label.gameObject.SetActive(false);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
