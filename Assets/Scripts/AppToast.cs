using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Простой «тост»: временное окошко с текстом. Вызывай: AppToast.Show("Жакында ачылат", 1.8f);
/// Не требует подготовки сцены — сам создаёт элементы под ближайшим Canvas.
/// </summary>
public class AppToast : MonoBehaviour
{
    private static AppToast _runner;

    public static void Show(string message, float duration = 1.8f)
    {
        if (string.IsNullOrEmpty(message)) return;
        if (_runner == null)
        {
            var go = new GameObject("[AppToastRunner]");
            _runner = go.AddComponent<AppToast>();
            Object.DontDestroyOnLoad(go);
        }
        _runner.StartCoroutine(_runner.ShowRoutine(message, duration));
    }

    private IEnumerator ShowRoutine(string message, float duration)
    {
        var canvas = FindTopCanvas();
        if (canvas == null)
        {
            var goCanvas = new GameObject("ToastCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = goCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = goCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            Object.DontDestroyOnLoad(goCanvas);
        }

        // Контейнер
        var go = new GameObject("Toast", typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(canvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.15f);
        rt.anchorMax = new Vector2(0.5f, 0.15f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        var group = go.GetComponent<CanvasGroup>();
        group.alpha = 0f;

        // Фон
        var bgGo = new GameObject("BG", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(go.transform, false);
        var bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        var bg = bgGo.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);
        bg.raycastTarget = false;

        // Текст
        var txGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        txGo.transform.SetParent(go.transform, false);
        var tx = txGo.GetComponent<TextMeshProUGUI>();
        tx.text = message;
        tx.alignment = TextAlignmentOptions.Midline;
        tx.enableAutoSizing = true; tx.fontSizeMin = 28; tx.fontSizeMax = 48;
        tx.fontSize = 40; tx.color = Color.white;
        tx.raycastTarget = false;

        // Паддинги
        var pad = go.AddComponent<HorizontalLayoutGroup>();
        pad.childAlignment = TextAnchor.MiddleCenter;
        pad.padding = new RectOffset(36, 36, 22, 22);

        var fitter = go.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Плавное появление ? пауза ? исчезновение
        float t = 0f, fade = 0.18f;
        while (t < fade) { t += Time.unscaledDeltaTime; group.alpha = Mathf.SmoothStep(0, 1, t / fade); yield return null; }
        yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, duration));
        t = 0f;
        while (t < fade) { t += Time.unscaledDeltaTime; group.alpha = Mathf.SmoothStep(1, 0, t / fade); yield return null; }

        Object.Destroy(go);
    }

    private static Canvas FindTopCanvas()
    {
        Canvas best = null; int bestOrder = int.MinValue;
        foreach (var c in Object.FindObjectsOfType<Canvas>())
        {
            if (!c.isActiveAndEnabled) continue;
            if (c.renderMode == RenderMode.WorldSpace) continue;
            if (c.sortingOrder >= bestOrder) { best = c; bestOrder = c.sortingOrder; }
        }
        return best;
    }
}
