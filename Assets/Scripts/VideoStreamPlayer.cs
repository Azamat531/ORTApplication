//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.Video;
//using UnityEngine.InputSystem; // Новый Input System
//using TMPro;
//using System.Collections;
//using System.IO;
//using System.Collections.Generic;

//[RequireComponent(typeof(RawImage), typeof(VideoPlayer), typeof(AudioSource))]
//public class VideoStreamPlayer : MonoBehaviour
//{
//    [Header("Видео")]
//    public string videoURL = ""; // Логический URL (Firebase https://... или file:///...)

//    [Header("Loading UI")]
//    public GameObject loadingPanel;   // Панель со спиннером (выкл по умолчанию)

//    // Core refs
//    private RawImage rawImage;
//    private RectTransform rectTransform;
//    private VideoPlayer videoPlayer;
//    private AspectRatioFitter fitter;
//    private RenderTexture rt; // RT — только при локальном воспроизведении

//    // Controls (опционально — можно не заполнять)
//    [Header("Иконки паузы/игры")] public Sprite playIcon; public Sprite pauseIcon; private Image pauseButtonImage;
//    [Header("UI-контролы")] public Button pauseButton; public Button rewindButton; public Button forwardButton; public Button screenOrientationButton;
//    public Sprite enterLandscapeIcon; public Sprite enterPortraitIcon; private Image screenOrientationButtonImage;
//    [Header("Скорость")] public Button speedButton; public TextMeshProUGUI speedButtonText; private readonly float[] speedOptions = { 1f, 1.25f, 1.5f, 2f, 0.5f }; private int speedIndex = 0;
//    [Header("Прогресс и время")] public Slider progressSlider; public TextMeshProUGUI timeText; private bool isDraggingSlider;
//    [Header("Затемнение контролов")] public CanvasGroup controlsOverlayGroup; private bool controlsVisible; private float lastToggleTime; private const float toggleDebounce = 0.25f; private Coroutine overlayFadeCoroutine;
//    [Header("Replay")] public Button replayButton;

//    [Header("Назад")] public Button backButton; public GameObject videoContainer;

//    [Header("Политика воспроизведения")]
//    [Tooltip("Если файла в кэше нет и интернет есть — стримим онлайн (без автоскачивания). Если выключено — при отсутствии кэша лишь логируем и выходим.")]
//    public bool streamIfNotCached = true;

//    private bool usingApiOnly = false; // true — когда играем онлайн без RT
//    private Vector2 lastScreenSize;
//    private ScreenOrientation lastOrientation;

//    void Awake()
//    {
//        rawImage = GetComponent<RawImage>();
//        rectTransform = rawImage.rectTransform;
//        fitter = rawImage.GetComponent<AspectRatioFitter>() ?? rawImage.gameObject.AddComponent<AspectRatioFitter>();

//        videoPlayer = GetComponent<VideoPlayer>();
//        var audio = GetComponent<AudioSource>();

//        // Видеоплеер — базовые настройки
//        videoPlayer.playOnAwake = false;
//        videoPlayer.waitForFirstFrame = true;
//        videoPlayer.skipOnDrop = true;
//        videoPlayer.isLooping = false;

//        // Аудио → AudioSource
//        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
//        audio.playOnAwake = false; audio.mute = false; audio.volume = 1f; audio.spatialBlend = 0f;
//        videoPlayer.controlledAudioTrackCount = 1; videoPlayer.EnableAudioTrack(0, true); videoPlayer.SetTargetAudioSource(0, audio);

//        // События
//        videoPlayer.prepareCompleted += OnVideoPrepared;
//        videoPlayer.loopPointReached += OnVideoFinished;
//        videoPlayer.errorReceived += OnVideoError;

//        videoPlayer.playbackSpeed = speedOptions[speedIndex];

//        if (videoContainer == null) videoContainer = this.gameObject;
//    }

//    void OnDestroy()
//    {
//        videoPlayer.prepareCompleted -= OnVideoPrepared;
//        videoPlayer.loopPointReached -= OnVideoFinished;
//        videoPlayer.errorReceived -= OnVideoError;
//        ReleaseRT();
//    }

//    void OnEnable()
//    {
//        if (controlsOverlayGroup != null)
//        {
//            if (overlayFadeCoroutine != null) { StopCoroutine(overlayFadeCoroutine); overlayFadeCoroutine = null; }
//            controlsOverlayGroup.alpha = 0f;
//            controlsOverlayGroup.blocksRaycasts = false;
//        }
//        controlsVisible = false; ToggleUI(false);
//    }

//    void Start()
//    {
//        lastScreenSize = new Vector2(Screen.width, Screen.height);
//        lastOrientation = Screen.orientation;

//        pauseButtonImage = pauseButton ? pauseButton.GetComponent<Image>() : null;
//        if (pauseButton) { pauseButton.onClick.RemoveAllListeners(); pauseButton.onClick.AddListener(TogglePause); if (pauseButtonImage) pauseButtonImage.sprite = pauseIcon; }
//        if (rewindButton) { rewindButton.onClick.RemoveAllListeners(); rewindButton.onClick.AddListener(() => Skip(-10)); }
//        if (forwardButton) { forwardButton.onClick.RemoveAllListeners(); forwardButton.onClick.AddListener(() => Skip(10)); }
//        if (screenOrientationButton)
//        {
//            screenOrientationButton.onClick.RemoveAllListeners(); screenOrientationButton.onClick.AddListener(ToggleOrientation);
//            screenOrientationButtonImage = screenOrientationButton.GetComponent<Image>(); UpdateOrientationButtonIcon();
//        }
//        if (speedButton) { speedButton.onClick.RemoveAllListeners(); speedButton.onClick.AddListener(ToggleSpeed); if (speedButtonText) speedButtonText.text = $"{speedOptions[speedIndex]}x"; }
//        if (progressSlider) { progressSlider.onValueChanged.RemoveAllListeners(); progressSlider.onValueChanged.AddListener(OnSliderChanged); }
//        if (replayButton) { replayButton.onClick.RemoveAllListeners(); replayButton.onClick.AddListener(OnReplayButtonClicked); replayButton.gameObject.SetActive(false); }
//        if (backButton) { backButton.onClick.RemoveAllListeners(); backButton.onClick.AddListener(CloseVideoPanel); backButton.gameObject.SetActive(false); }

//        if (controlsOverlayGroup != null) { HideControls(); controlsOverlayGroup.alpha = 0f; controlsOverlayGroup.blocksRaycasts = false; }

//        if (!string.IsNullOrEmpty(videoURL)) StartCoroutine(PlayWithLoading(videoURL));
//    }

//    // === Публичный API ===
//    public void SetVideoURL(string url)
//    {
//        videoURL = url;
//        StopAllCoroutines();
//        StartCoroutine(PlayWithLoading(url));
//        if (videoContainer != null && !videoContainer.activeSelf) videoContainer.SetActive(true);
//    }

//    public void Pause() { if (videoPlayer != null) videoPlayer.Pause(); }
//    public void StopPlayback() { if (videoPlayer != null) { videoPlayer.Stop(); rawImage.texture = null; } }

//    // === Закрыть панель видео (кнопка Назад) ===
//    public void CloseVideoPanel()
//    {
//        StopAllCoroutines();
//        try { if (videoPlayer != null) videoPlayer.Stop(); } catch { }
//        if (rawImage != null) rawImage.texture = null;

//        if (overlayFadeCoroutine != null) { StopCoroutine(overlayFadeCoroutine); overlayFadeCoroutine = null; }
//        if (controlsOverlayGroup != null) { controlsOverlayGroup.alpha = 0f; controlsOverlayGroup.blocksRaycasts = false; }
//        controlsVisible = false; ToggleUI(false);

//        if (loadingPanel != null) loadingPanel.SetActive(false);
//        ReleaseRT();
//        if (videoContainer != null) videoContainer.SetActive(false);
//    }

//    // === Основная логика: ЛОКАЛЬНО если есть кэш; если кэша НЕТ →
//    //     (1) если есть интернет и streamIfNotCached=true → СТРИМ
//    //     (2) если интернета нет или streamIfNotCached=false → просто лог и выход (без автоскачивания)
//    private IEnumerator PlayWithLoading(string url)
//    {
//        if (loadingPanel != null) { loadingPanel.SetActive(true); yield return null; yield return new WaitForEndOfFrame(); }

//        string reqUrl = ForceMp4Url(url);
//        const string ext = ".mp4";

//        // --- Пытаемся найти локальный кэш (.enc) разными вариантами ключа ---
//        string encPath = FindCachedVideoPath(reqUrl, ext);
//        usingApiOnly = false;
//        string localPlain = null;

//        if (string.IsNullOrEmpty(encPath))
//        {
//            // Кэша нет
//            if (IsOnline() && streamIfNotCached)
//            {
//                // → Онлайн-стрим (без автодонлоада)
//                videoPlayer.renderMode = VideoRenderMode.APIOnly;
//                videoPlayer.source = VideoSource.Url;
//                videoPlayer.url = reqUrl;
//                usingApiOnly = true;

//                float timeout = 12f, t = 0f;
//                videoPlayer.Prepare();
//                while (!videoPlayer.isPrepared && t < timeout) { t += Time.deltaTime; yield return null; }
//                if (!videoPlayer.isPrepared)
//                {
//                    if (loadingPanel != null) loadingPanel.SetActive(false);
//                    Debug.LogWarning("[VideoStreamPlayer] Не удалось подготовить потоковое видео");
//                    yield break;
//                }

//                rawImage.texture = videoPlayer.texture; // APIOnly → напрямую
//                UpdateVideoSize();
//                if (loadingPanel != null) loadingPanel.SetActive(false);
//                videoPlayer.playbackSpeed = speedOptions[speedIndex];
//                videoPlayer.Play();
//                yield break;
//            }
//            else
//            {
//                // → Без интернета ИЛИ стрим отключён → выходим (без скачивания)
//                if (loadingPanel != null) loadingPanel.SetActive(false);
//                Debug.LogWarning("[VideoStreamPlayer] No cache. Streaming disabled or offline. No auto-download.");
//                yield break;
//            }
//        }

//        // --- Кэш есть → дешифруем в детерминированный temp и играем локально ---
//        yield return null; // дать кадр UI
//        localPlain = CacheService.GetOrMakePlainTemp(encPath, ext);
//        if (string.IsNullOrEmpty(localPlain) || !File.Exists(localPlain))
//        {
//            if (loadingPanel != null) loadingPanel.SetActive(false);
//            Debug.LogWarning("[VideoStreamPlayer] Не удалось открыть локальный файл");
//            yield break;
//        }

//        // Локальный файл: через RenderTexture
//        videoPlayer.source = VideoSource.Url;
//        videoPlayer.url = AsFileUrl(localPlain);
//        usingApiOnly = false;

//        ReleaseRT(); rawImage.texture = null; videoPlayer.targetTexture = null; videoPlayer.renderMode = VideoRenderMode.RenderTexture;

//        float timeout2 = 12f, t2 = 0f;
//        videoPlayer.Prepare();
//        while (!videoPlayer.isPrepared && t2 < timeout2) { t2 += Time.deltaTime; yield return null; }
//        if (!videoPlayer.isPrepared)
//        {
//            if (loadingPanel != null) loadingPanel.SetActive(false);
//            Debug.LogWarning("[VideoStreamPlayer] Не удалось подготовить локальное видео");
//            yield break;
//        }

//        int w = Mathf.Max(2, (int)(videoPlayer.width > 0 ? videoPlayer.width : 1280));
//        int h = Mathf.Max(2, (int)(videoPlayer.height > 0 ? videoPlayer.height : 720));
//        CreateRT(w, h);
//        videoPlayer.targetTexture = rt; rawImage.texture = rt;

//        if (loadingPanel != null) loadingPanel.SetActive(false);
//        videoPlayer.playbackSpeed = speedOptions[speedIndex];
//        videoPlayer.Play();
//        UpdateVideoSize();
//    }

//    // === VideoPlayer callbacks ===
//    private void OnVideoPrepared(VideoPlayer vp)
//    {
//        if (usingApiOnly)
//        {
//            rawImage.texture = vp.texture; UpdateVideoSize(); return;
//        }
//        int w = Mathf.Max(2, (int)(vp.width > 0 ? vp.width : 1280));
//        int h = Mathf.Max(2, (int)(vp.height > 0 ? vp.height : 720));
//        if (rt == null || rt.width != w || rt.height != h) { CreateRT(w, h); vp.targetTexture = rt; }
//        rawImage.texture = rt; UpdateVideoSize();
//    }
//    private void OnVideoFinished(VideoPlayer vp)
//    {
//        HideControls();
//        if (replayButton != null) { replayButton.gameObject.SetActive(true); FadeOverlay(controlsOverlayGroup, 1f, 0.25f); }
//    }
//    private void OnVideoError(VideoPlayer vp, string msg)
//    {
//        Debug.LogError("[VideoStreamPlayer] Error: " + msg);
//        if (loadingPanel != null) loadingPanel.SetActive(false);
//    }

//    void Update()
//    {
//        HandlePointerToggle();

//        if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
//        { lastScreenSize = new Vector2(Screen.width, Screen.height); UpdateVideoSize(); }
//        if (Screen.orientation != lastOrientation)
//        { lastOrientation = Screen.orientation; UpdateOrientationButtonIcon(); UpdateVideoSize(); }

//        if (!isDraggingSlider && videoPlayer && videoPlayer.length > 0 && progressSlider != null)
//            progressSlider.value = (float)(videoPlayer.time / videoPlayer.length);

//        UpdateTimeText();
//        if (usingApiOnly && rawImage.texture == null && videoPlayer.texture != null) rawImage.texture = videoPlayer.texture; // страховка
//    }

//    // === UI ===
//    private void HandlePointerToggle()
//    {
//        bool pressed = false; Vector2 pos = default;
//        if (Touchscreen.current != null)
//        {
//            var touch = Touchscreen.current.primaryTouch; if (touch.press.wasPressedThisFrame) { pressed = true; pos = touch.position.ReadValue(); }
//        }
//        if (!pressed && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
//        { pressed = true; pos = Mouse.current.position.ReadValue(); }
//        if (!pressed) return; if (Time.unscaledTime - lastToggleTime < toggleDebounce) return; if (HitUIButton(pos)) return; lastToggleTime = Time.unscaledTime; ToggleControls();
//    }
//    private bool HitUIButton(Vector2 screenPos)
//    {
//        bool Hit(RectTransform rt) => rt && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, null);
//        return (pauseButton && Hit(pauseButton.transform as RectTransform)) ||
//               (rewindButton && Hit(rewindButton.transform as RectTransform)) ||
//               (forwardButton && Hit(forwardButton.transform as RectTransform)) ||
//               (progressSlider && Hit(progressSlider.transform as RectTransform)) ||
//               (screenOrientationButton && Hit(screenOrientationButton.transform as RectTransform)) ||
//               (speedButton && Hit(speedButton.transform as RectTransform)) ||
//               (replayButton && Hit(replayButton.transform as RectTransform)) ||
//               (backButton && Hit(backButton.transform as RectTransform));
//    }
//    private void ToggleControls() { if (controlsVisible) HideControls(); else ShowControls(); }
//    private void ShowControls()
//    {
//        if (controlsOverlayGroup == null) return; if (replayButton != null && replayButton.gameObject.activeSelf) return;
//        ToggleUI(true); FadeOverlay(controlsOverlayGroup, 1f, 0.2f); controlsVisible = true;
//    }
//    private void HideControls()
//    {
//        if (controlsOverlayGroup == null) return; ToggleUI(false); FadeOverlay(controlsOverlayGroup, 0f, 0.2f); controlsVisible = false;
//    }
//    private void ToggleUI(bool on)
//    {
//        if (pauseButton) pauseButton.gameObject.SetActive(on);
//        if (rewindButton) rewindButton.gameObject.SetActive(on);
//        if (forwardButton) forwardButton.gameObject.SetActive(on);
//        if (screenOrientationButton) screenOrientationButton.gameObject.SetActive(on);
//        if (speedButton) speedButton.gameObject.SetActive(on);
//        if (progressSlider) progressSlider.gameObject.SetActive(on);
//        if (timeText) timeText.gameObject.SetActive(on);
//        if (backButton) backButton.gameObject.SetActive(on);
//    }
//    private void TogglePause()
//    {
//        if (!videoPlayer || !videoPlayer.isPrepared) return;
//        if (videoPlayer.isPlaying) { videoPlayer.Pause(); if (pauseButtonImage) pauseButtonImage.sprite = playIcon; }
//        else { videoPlayer.Play(); if (pauseButtonImage) pauseButtonImage.sprite = pauseIcon; }
//    }
//    private void Skip(double seconds)
//    {
//        if (!videoPlayer || !videoPlayer.isPrepared || videoPlayer.length <= 0) return;
//        double target = Mathf.Clamp((float)(videoPlayer.time + seconds), 0f, (float)videoPlayer.length);
//        videoPlayer.time = target;
//    }
//    private void OnReplayButtonClicked()
//    {
//        if (replayButton) replayButton.gameObject.SetActive(false);
//        FadeOverlay(controlsOverlayGroup, 0f, 0.25f);
//        if (videoPlayer) { videoPlayer.time = 0; videoPlayer.Play(); }
//    }
//    private void ToggleOrientation()
//    {
//        if (Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown)
//            Screen.orientation = ScreenOrientation.LandscapeLeft;
//        else
//            Screen.orientation = ScreenOrientation.Portrait;
//        UpdateOrientationButtonIcon();
//    }
//    private void UpdateOrientationButtonIcon()
//    {
//        if (!screenOrientationButtonImage) return;
//        bool portrait = Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown;
//        screenOrientationButtonImage.sprite = portrait ? enterLandscapeIcon : enterPortraitIcon;
//    }
//    private void ToggleSpeed()
//    {
//        speedIndex = (speedIndex + 1) % speedOptions.Length; float newSpeed = speedOptions[speedIndex];
//        if (videoPlayer) videoPlayer.playbackSpeed = newSpeed; if (speedButtonText) speedButtonText.text = $"{newSpeed}x";
//    }
//    private void OnSliderChanged(float value)
//    {
//        if (!isDraggingSlider || videoPlayer == null || videoPlayer.length <= 0) return; videoPlayer.time = value * videoPlayer.length;
//    }
//    public void OnBeginDrag() { isDraggingSlider = true; }
//    public void OnEndDrag() { isDraggingSlider = false; }

//    // === Layout / размеры ===
//    private void UpdateVideoSize()
//    {
//        if (!fitter || !videoPlayer) return;
//        float vw = (videoPlayer.width > 0) ? videoPlayer.width : (rt != null ? rt.width : 0);
//        float vh = (videoPlayer.height > 0) ? videoPlayer.height : (rt != null ? rt.height : 0);
//        if (usingApiOnly && videoPlayer.texture != null) { vw = videoPlayer.texture.width; vh = videoPlayer.texture.height; }
//        if (vw <= 0 || vh <= 0) return;

//        float aspect = vw / vh;
//        if (aspect < 1f)
//        { fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight; fitter.aspectRatio = aspect; }
//        else
//        { fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fitter.aspectRatio = aspect; }
//        if (!usingApiOnly && rawImage && rawImage.texture != rt && rt != null) rawImage.texture = rt;
//    }
//    private void UpdateTimeText()
//    {
//        if (!timeText || videoPlayer == null) return;
//        if (videoPlayer.length > 0)
//        { int m1 = (int)(videoPlayer.time / 60), s1 = (int)(videoPlayer.time % 60); int m2 = (int)(videoPlayer.length / 60), s2 = (int)(videoPlayer.length % 60); timeText.text = $"{m1}:{s1:D2} / {m2}:{s2:D2}"; }
//        else timeText.text = "0:00 / 0:00";
//    }

//    private IEnumerator FadeCanvasGroup(CanvasGroup g, float target, float dur)
//    {
//        if (!g) yield break; float start = g.alpha, t = 0f; g.blocksRaycasts = target > 0f; g.interactable = target > 0f;
//        while (t < dur) { t += Time.deltaTime; g.alpha = Mathf.Lerp(start, target, t / dur); yield return null; }
//        g.alpha = target; g.blocksRaycasts = target > 0f; g.interactable = target > 0f;
//    }
//    private void FadeOverlay(CanvasGroup g, float target, float dur)
//    {
//        if (!g) return; if (overlayFadeCoroutine != null) StopCoroutine(overlayFadeCoroutine); overlayFadeCoroutine = StartCoroutine(FadeCanvasGroup(g, target, dur));
//    }

//    // === Helpers ===
//    private static bool IsOnline() => Application.internetReachability != NetworkReachability.NotReachable;

//    private static string ForceMp4Url(string url)
//    {
//        if (string.IsNullOrEmpty(url)) return url; if (url.StartsWith("file:")) return url;
//        int q = url.IndexOf('?'); string baseUrl = q >= 0 ? url.Substring(0, q) : url; string query = q >= 0 ? url.Substring(q) : string.Empty;
//        string ext = Path.GetExtension(baseUrl);
//        if (string.IsNullOrEmpty(ext)) return baseUrl + ".mp4" + query;
//        if (!ext.Equals(".mp4", System.StringComparison.OrdinalIgnoreCase)) { baseUrl = baseUrl.Substring(0, baseUrl.Length - ext.Length) + ".mp4"; return baseUrl + query; }
//        return url;
//    }

//    private static string CanonicalCacheKey(string urlMp4)
//    {
//        return "video:" + urlMp4.Replace("%2F", "%2f");
//    }

//    private static string FindCachedVideoPath(string urlMp4, string ext)
//    {
//        var candidates = new List<string>
//        {
//            urlMp4,
//            urlMp4.Replace("%2F", "%2f"),
//            urlMp4.Replace("%2f", "%2F"),
//            urlMp4.Replace(".mp4", ""),
//            urlMp4.Replace(".mp4", "").Replace("%2F","%2f"),
//            urlMp4.Replace(".mp4", "").Replace("%2f","%2F")
//        };
//        foreach (var u in candidates)
//        {
//            var p = CacheService.GetCachedPath("video:" + u, ext);
//            if (!string.IsNullOrEmpty(p)) return p;
//        }
//        return null;
//    }

//    private static string AsFileUrl(string localPath)
//    {
//        if (string.IsNullOrEmpty(localPath)) return localPath;
//        if (localPath.StartsWith("http")) return localPath; if (localPath.StartsWith("file:///")) return localPath.Replace('\\', '/');
//        var norm = localPath.Replace('\\', '/'); return "file:///" + norm.TrimStart('/');
//    }

//    private void CreateRT(int w, int h)
//    {
//        ReleaseRT();
//        rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
//        { useDynamicScale = false, autoGenerateMips = false, antiAliasing = 1, wrapMode = TextureWrapMode.Clamp };
//        rt.Create();
//    }
//    private void ReleaseRT()
//    {
//        if (videoPlayer) videoPlayer.targetTexture = null;
//        if (rt != null) { if (rt.IsCreated()) rt.Release(); Destroy(rt); rt = null; }
//    }
//}


using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.IO;
using System.Collections.Generic;

[RequireComponent(typeof(RawImage), typeof(VideoPlayer), typeof(AudioSource))]
public class VideoStreamPlayer : MonoBehaviour
{
    [Header("Видео")]
    public string videoURL = "";

    [Header("Loading UI")]
    public GameObject loadingPanel;

    // === Почти досмотрел ===
    [Header("Completion")]
    [Tooltip("Сколько секунд можно не досматривать до конца, чтобы зачесть просмотр.")]
    public float nearEndSeconds = 10f;
    public System.Action<string> NearEndReached; // url видео
    private bool nearEndFired = false;

    // Core refs
    private RawImage rawImage;
    private RectTransform rectTransform;
    private VideoPlayer videoPlayer;
    private AspectRatioFitter fitter;
    private RenderTexture rt;

    [Header("Иконки паузы/игры")] public Sprite playIcon; public Sprite pauseIcon; private Image pauseButtonImage;
    [Header("UI-контролы")] public Button pauseButton; public Button rewindButton; public Button forwardButton; public Button screenOrientationButton;
    public Sprite enterLandscapeIcon; public Sprite enterPortraitIcon; private Image screenOrientationButtonImage;
    [Header("Скорость")] public Button speedButton; public TextMeshProUGUI speedButtonText; private readonly float[] speedOptions = { 1f, 1.25f, 1.5f, 2f, 0.5f }; private int speedIndex = 0;
    [Header("Прогресс и время")] public Slider progressSlider; public TextMeshProUGUI timeText; private bool isDraggingSlider;
    [Header("Затемнение контролов")] public CanvasGroup controlsOverlayGroup; private bool controlsVisible; private float lastToggleTime; private const float toggleDebounce = 0.25f; private Coroutine overlayFadeCoroutine;
    [Header("Replay")] public Button replayButton;

    [Header("Назад")] public Button backButton; public GameObject videoContainer;

    [Header("Политика воспроизведения")]
    [Tooltip("Если файла в кэше нет и интернет есть — стримим онлайн (без автоскачивания). Если выключено — при отсутствии кэша лишь логируем и выходим.")]
    public bool streamIfNotCached = true;

    private bool usingApiOnly = false;
    private Vector2 lastScreenSize;
    private ScreenOrientation lastOrientation;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        rectTransform = rawImage.rectTransform;
        fitter = rawImage.GetComponent<AspectRatioFitter>() ?? rawImage.gameObject.AddComponent<AspectRatioFitter>();

        videoPlayer = GetComponent<VideoPlayer>();
        var audio = GetComponent<AudioSource>();

        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;
        videoPlayer.isLooping = false;

        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        audio.playOnAwake = false; audio.mute = false; audio.volume = 1f; audio.spatialBlend = 0f;
        videoPlayer.controlledAudioTrackCount = 1; videoPlayer.EnableAudioTrack(0, true); videoPlayer.SetTargetAudioSource(0, audio);

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.errorReceived += OnVideoError;

        videoPlayer.playbackSpeed = speedOptions[speedIndex];

        if (videoContainer == null) videoContainer = this.gameObject;
    }

    void OnDestroy()
    {
        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.errorReceived -= OnVideoError;
        ReleaseRT();
    }

    void OnEnable()
    {
        if (controlsOverlayGroup != null)
        {
            if (overlayFadeCoroutine != null) { StopCoroutine(overlayFadeCoroutine); overlayFadeCoroutine = null; }
            controlsOverlayGroup.alpha = 0f;
            controlsOverlayGroup.blocksRaycasts = false;
        }
        controlsVisible = false; ToggleUI(false);
    }

    void Start()
    {
        lastScreenSize = new Vector2(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;

        pauseButtonImage = pauseButton ? pauseButton.GetComponent<Image>() : null;
        if (pauseButton) { pauseButton.onClick.RemoveAllListeners(); pauseButton.onClick.AddListener(TogglePause); if (pauseButtonImage) pauseButtonImage.sprite = pauseIcon; }
        if (rewindButton) { rewindButton.onClick.RemoveAllListeners(); rewindButton.onClick.AddListener(() => Skip(-10)); }
        if (forwardButton) { forwardButton.onClick.RemoveAllListeners(); forwardButton.onClick.AddListener(() => Skip(10)); }
        if (screenOrientationButton)
        {
            screenOrientationButton.onClick.RemoveAllListeners(); screenOrientationButton.onClick.AddListener(ToggleOrientation);
            screenOrientationButtonImage = screenOrientationButton.GetComponent<Image>(); UpdateOrientationButtonIcon();
        }
        if (speedButton) { speedButton.onClick.RemoveAllListeners(); speedButton.onClick.AddListener(ToggleSpeed); if (speedButtonText) speedButtonText.text = $"{speedOptions[speedIndex]}x"; }
        if (progressSlider) { progressSlider.onValueChanged.RemoveAllListeners(); progressSlider.onValueChanged.AddListener(OnSliderChanged); }
        if (replayButton) { replayButton.onClick.RemoveAllListeners(); replayButton.onClick.AddListener(OnReplayButtonClicked); replayButton.gameObject.SetActive(false); }
        if (backButton) { backButton.onClick.RemoveAllListeners(); backButton.onClick.AddListener(CloseVideoPanel); backButton.gameObject.SetActive(false); }

        if (controlsOverlayGroup != null) { HideControls(); controlsOverlayGroup.alpha = 0f; controlsOverlayGroup.blocksRaycasts = false; }

        if (!string.IsNullOrEmpty(videoURL)) StartCoroutine(PlayWithLoading(videoURL));
    }

    // === Публичный API ===
    public void SetVideoURL(string url)
    {
        videoURL = url;
        nearEndFired = false; // сброс
        StopAllCoroutines();
        StartCoroutine(PlayWithLoading(url));
        if (videoContainer != null && !videoContainer.activeSelf) videoContainer.SetActive(true);
    }

    public void Pause() { if (videoPlayer != null) videoPlayer.Pause(); }
    public void StopPlayback() { if (videoPlayer != null) { videoPlayer.Stop(); rawImage.texture = null; } }

    // === Закрыть панель видео (кнопка Назад) ===
    public void CloseVideoPanel()
    {
        StopAllCoroutines();
        try { if (videoPlayer != null) videoPlayer.Stop(); } catch { }
        if (rawImage != null) rawImage.texture = null;

        if (overlayFadeCoroutine != null) { StopCoroutine(overlayFadeCoroutine); overlayFadeCoroutine = null; }
        if (controlsOverlayGroup != null) { controlsOverlayGroup.alpha = 0f; controlsOverlayGroup.blocksRaycasts = false; }
        controlsVisible = false; ToggleUI(false);

        if (loadingPanel != null) loadingPanel.SetActive(false);
        ReleaseRT();
        if (videoContainer != null) videoContainer.SetActive(false);
    }

    // === Основная логика воспроизведения ===
    private IEnumerator PlayWithLoading(string url)
    {
        if (loadingPanel != null) { loadingPanel.SetActive(true); yield return null; yield return new WaitForEndOfFrame(); }

        string reqUrl = ForceMp4Url(url);
        const string ext = ".mp4";

        string encPath = FindCachedVideoPath(reqUrl, ext);
        usingApiOnly = false;
        string localPlain = null;

        if (string.IsNullOrEmpty(encPath))
        {
            if (IsOnline() && streamIfNotCached)
            {
                videoPlayer.renderMode = VideoRenderMode.APIOnly;
                videoPlayer.source = VideoSource.Url;
                videoPlayer.url = reqUrl;
                usingApiOnly = true;

                float timeout = 12f, t = 0f;
                videoPlayer.Prepare();
                while (!videoPlayer.isPrepared && t < timeout) { t += Time.deltaTime; yield return null; }
                if (!videoPlayer.isPrepared)
                {
                    if (loadingPanel != null) loadingPanel.SetActive(false);
                    Debug.LogWarning("[VideoStreamPlayer] Не удалось подготовить потоковое видео");
                    yield break;
                }

                rawImage.texture = videoPlayer.texture;
                UpdateVideoSize();
                if (loadingPanel != null) loadingPanel.SetActive(false);
                videoPlayer.playbackSpeed = speedOptions[speedIndex];
                videoPlayer.Play();
                yield break;
            }
            else
            {
                if (loadingPanel != null) loadingPanel.SetActive(false);
                Debug.LogWarning("[VideoStreamPlayer] No cache. Streaming disabled or offline. No auto-download.");
                yield break;
            }
        }

        yield return null;
        localPlain = CacheService.GetOrMakePlainTemp(encPath, ext);
        if (string.IsNullOrEmpty(localPlain) || !File.Exists(localPlain))
        {
            if (loadingPanel != null) loadingPanel.SetActive(false);
            Debug.LogWarning("[VideoStreamPlayer] Не удалось открыть локальный файл");
            yield break;
        }

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = AsFileUrl(localPlain);
        usingApiOnly = false;

        ReleaseRT(); rawImage.texture = null; videoPlayer.targetTexture = null; videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        float timeout2 = 12f, t2 = 0f;
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared && t2 < timeout2) { t2 += Time.deltaTime; yield return null; }
        if (!videoPlayer.isPrepared)
        {
            if (loadingPanel != null) loadingPanel.SetActive(false);
            Debug.LogWarning("[VideoStreamPlayer] Не удалось подготовить локальное видео");
            yield break;
        }

        int w = Mathf.Max(2, (int)(videoPlayer.width > 0 ? videoPlayer.width : 1280));
        int h = Mathf.Max(2, (int)(videoPlayer.height > 0 ? videoPlayer.height : 720));
        CreateRT(w, h);
        videoPlayer.targetTexture = rt; rawImage.texture = rt;

        if (loadingPanel != null) loadingPanel.SetActive(false);
        videoPlayer.playbackSpeed = speedOptions[speedIndex];
        videoPlayer.Play();
        UpdateVideoSize();
    }

    // === VideoPlayer callbacks ===
    private void OnVideoPrepared(VideoPlayer vp)
    {
        if (usingApiOnly)
        {
            rawImage.texture = vp.texture; UpdateVideoSize(); return;
        }
        int w = Mathf.Max(2, (int)(vp.width > 0 ? vp.width : 1280));
        int h = Mathf.Max(2, (int)(vp.height > 0 ? vp.height : 720));
        if (rt == null || rt.width != w || rt.height != h) { CreateRT(w, h); vp.targetTexture = rt; }
        rawImage.texture = rt; UpdateVideoSize();
    }
    private void OnVideoFinished(VideoPlayer vp)
    {
        HideControls();
        if (replayButton != null) { replayButton.gameObject.SetActive(true); FadeOverlay(controlsOverlayGroup, 1f, 0.25f); }
    }
    private void OnVideoError(VideoPlayer vp, string msg)
    {
        Debug.LogError("[VideoStreamPlayer] Error: " + msg);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    void Update()
    {
        HandlePointerToggle();

        // Почти конец — шлём сигнал один раз
        if (!nearEndFired && videoPlayer && videoPlayer.isPlaying && videoPlayer.length > 0)
        {
            double remaining = videoPlayer.length - videoPlayer.time;
            if (remaining <= nearEndSeconds)
            {
                nearEndFired = true;
                NearEndReached?.Invoke(videoURL);
            }
        }

        if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
        { lastScreenSize = new Vector2(Screen.width, Screen.height); UpdateVideoSize(); }
        if (Screen.orientation != lastOrientation)
        { lastOrientation = Screen.orientation; UpdateOrientationButtonIcon(); UpdateVideoSize(); }

        if (!isDraggingSlider && videoPlayer && videoPlayer.length > 0 && progressSlider != null)
            progressSlider.value = (float)(videoPlayer.time / videoPlayer.length);

        UpdateTimeText();
        if (usingApiOnly && rawImage.texture == null && videoPlayer.texture != null) rawImage.texture = videoPlayer.texture;
    }

    // ===== UI/Controls =====
    private void HandlePointerToggle()
    {
        bool pressed = false; Vector2 pos = default;
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch; if (touch.press.wasPressedThisFrame) { pressed = true; pos = touch.position.ReadValue(); }
        }
        if (!pressed && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        { pressed = true; pos = Mouse.current.position.ReadValue(); }
        if (!pressed) return; if (Time.unscaledTime - lastToggleTime < 0.25f) return; if (HitUIButton(pos)) return; lastToggleTime = Time.unscaledTime; ToggleControls();
    }
    private bool HitUIButton(Vector2 screenPos)
    {
        bool Hit(RectTransform rt) => rt && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, null);
        return (pauseButton && Hit(pauseButton.transform as RectTransform)) ||
               (rewindButton && Hit(rewindButton.transform as RectTransform)) ||
               (forwardButton && Hit(forwardButton.transform as RectTransform)) ||
               (progressSlider && Hit(progressSlider.transform as RectTransform)) ||
               (screenOrientationButton && Hit(screenOrientationButton.transform as RectTransform)) ||
               (speedButton && Hit(speedButton.transform as RectTransform)) ||
               (replayButton && Hit(replayButton.transform as RectTransform)) ||
               (backButton && Hit(backButton.transform as RectTransform));
    }
    private void ToggleControls() { if (controlsVisible) HideControls(); else ShowControls(); }
    private void ShowControls()
    {
        if (controlsOverlayGroup == null) return; if (replayButton != null && replayButton.gameObject.activeSelf) return;
        ToggleUI(true); FadeOverlay(controlsOverlayGroup, 1f, 0.2f); controlsVisible = true;
    }
    private void HideControls()
    {
        if (controlsOverlayGroup == null) return; ToggleUI(false); FadeOverlay(controlsOverlayGroup, 0f, 0.2f); controlsVisible = false;
    }
    private void ToggleUI(bool on)
    {
        if (pauseButton) pauseButton.gameObject.SetActive(on);
        if (rewindButton) rewindButton.gameObject.SetActive(on);
        if (forwardButton) forwardButton.gameObject.SetActive(on);
        if (screenOrientationButton) screenOrientationButton.gameObject.SetActive(on);
        if (speedButton) speedButton.gameObject.SetActive(on);
        if (progressSlider) progressSlider.gameObject.SetActive(on);
        if (timeText) timeText.gameObject.SetActive(on);
        if (backButton) backButton.gameObject.SetActive(on);
    }
    private void TogglePause()
    {
        if (!videoPlayer || !videoPlayer.isPrepared) return;
        if (videoPlayer.isPlaying) { videoPlayer.Pause(); if (pauseButtonImage) pauseButtonImage.sprite = playIcon; }
        else { videoPlayer.Play(); if (pauseButtonImage) pauseButtonImage.sprite = pauseIcon; }
    }
    private void Skip(double seconds)
    {
        if (!videoPlayer || !videoPlayer.isPrepared || videoPlayer.length <= 0) return;
        double target = Mathf.Clamp((float)(videoPlayer.time + seconds), 0f, (float)videoPlayer.length);
        videoPlayer.time = target;
    }
    private void OnReplayButtonClicked()
    {
        if (replayButton) replayButton.gameObject.SetActive(false);
        FadeOverlay(controlsOverlayGroup, 0f, 0.25f);
        if (videoPlayer) { videoPlayer.time = 0; videoPlayer.Play(); }
    }
    private void ToggleOrientation()
    {
        if (Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown)
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        else
            Screen.orientation = ScreenOrientation.Portrait;
        UpdateOrientationButtonIcon();
    }
    private void UpdateOrientationButtonIcon()
    {
        if (!screenOrientationButtonImage) return;
        bool portrait = Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown;
        screenOrientationButtonImage.sprite = portrait ? enterLandscapeIcon : enterPortraitIcon;
    }
    private void ToggleSpeed()
    {
        speedIndex = (speedIndex + 1) % speedOptions.Length; float newSpeed = speedOptions[speedIndex];
        if (videoPlayer) videoPlayer.playbackSpeed = newSpeed; if (speedButtonText) speedButtonText.text = $"{newSpeed}x";
    }
    private void OnSliderChanged(float value)
    {
        if (!isDraggingSlider || videoPlayer == null || videoPlayer.length <= 0) return; videoPlayer.time = value * videoPlayer.length;
    }
    public void OnBeginDrag() { isDraggingSlider = true; }
    public void OnEndDrag() { isDraggingSlider = false; }

    // ===== Анимация CanvasGroup =====
    private IEnumerator FadeCanvasGroup(CanvasGroup g, float target, float dur)
    {
        if (!g) yield break;
        float start = g.alpha, t = 0f;
        g.blocksRaycasts = target > 0f;
        g.interactable = target > 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            g.alpha = Mathf.Lerp(start, target, t / dur);
            yield return null;
        }
        g.alpha = target;
        g.blocksRaycasts = target > 0f;
        g.interactable = target > 0f;
    }
    private void FadeOverlay(CanvasGroup g, float target, float dur)
    {
        if (!g) return;
        if (overlayFadeCoroutine != null) StopCoroutine(overlayFadeCoroutine);
        overlayFadeCoroutine = StartCoroutine(FadeCanvasGroup(g, target, dur));
    }

    // ===== Low-level helpers =====
    private static bool IsOnline() => Application.internetReachability != NetworkReachability.NotReachable;
    private static string ForceMp4Url(string url)
    {
        if (string.IsNullOrEmpty(url)) return url; if (url.StartsWith("file:")) return url;
        int q = url.IndexOf('?'); string baseUrl = q >= 0 ? url.Substring(0, q) : url; string query = q >= 0 ? url.Substring(q) : string.Empty;
        string ext = Path.GetExtension(baseUrl);
        if (string.IsNullOrEmpty(ext)) return baseUrl + ".mp4" + query;
        if (!ext.Equals(".mp4", System.StringComparison.OrdinalIgnoreCase)) { baseUrl = baseUrl.Substring(0, baseUrl.Length - ext.Length) + ".mp4"; return baseUrl + query; }
        return url;
    }
    private static string FindCachedVideoPath(string urlMp4, string ext)
    {
        var candidates = new List<string>
        {
            urlMp4, urlMp4.Replace("%2F", "%2f"), urlMp4.Replace("%2f", "%2F"),
            urlMp4.Replace(".mp4", ""), urlMp4.Replace(".mp4","").Replace("%2F","%2f"), urlMp4.Replace(".mp4","").Replace("%2f","%2F")
        };
        foreach (var u in candidates)
        {
            var p = CacheService.GetCachedPath("video:" + u, ext);
            if (!string.IsNullOrEmpty(p)) return p;
        }
        return null;
    }
    private static string AsFileUrl(string localPath)
    {
        if (string.IsNullOrEmpty(localPath)) return localPath;
        if (localPath.StartsWith("http")) return localPath; if (localPath.StartsWith("file:///")) return localPath.Replace('\\', '/');
        var norm = localPath.Replace('\\', '/'); return "file:///" + norm.TrimStart('/');
    }
    private void CreateRT(int w, int h)
    {
        ReleaseRT();
        rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
        { useDynamicScale = false, autoGenerateMips = false, antiAliasing = 1, wrapMode = TextureWrapMode.Clamp };
        rt.Create();
    }
    private void ReleaseRT()
    {
        if (videoPlayer) videoPlayer.targetTexture = null;
        if (rt != null) { if (rt.IsCreated()) rt.Release(); Destroy(rt); rt = null; }
    }

    // === ВАЖНО: эти два метода были причиной ошибок ===
    private void UpdateVideoSize()
    {
        if (!fitter || !videoPlayer) return;
        float vw = (videoPlayer.width > 0) ? videoPlayer.width : (rt != null ? rt.width : 0);
        float vh = (videoPlayer.height > 0) ? videoPlayer.height : (rt != null ? rt.height : 0);
        if (usingApiOnly && videoPlayer.texture != null) { vw = videoPlayer.texture.width; vh = videoPlayer.texture.height; }
        if (vw <= 0 || vh <= 0) return;

        float aspect = vw / vh;
        if (aspect < 1f) { fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight; fitter.aspectRatio = aspect; }
        else { fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fitter.aspectRatio = aspect; }
        if (!usingApiOnly && rawImage && rawImage.texture != rt && rt != null) rawImage.texture = rt;
    }

    private void UpdateTimeText()
    {
        if (!timeText || videoPlayer == null) return;
        if (videoPlayer.length > 0)
        { int m1 = (int)(videoPlayer.time / 60), s1 = (int)(videoPlayer.time % 60); int m2 = (int)(videoPlayer.length / 60), s2 = (int)(videoPlayer.length % 60); timeText.text = $"{m1}:{s1:D2} / {m2}:{s2:D2}"; }
        else timeText.text = "0:00 / 0:00";
    }
}
