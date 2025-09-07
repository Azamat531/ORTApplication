//// ============================================
//// File: Assets/Scripts/VideoStreamPlayer.cs
//// Purpose: Локальное воспроизведение видео (cache-first, encrypted cache) с плавным UX
//// Policy:  MP4-only (H.264+AAC). Кэш: ВСЕГДА local file:/// (офлайн работает).
//// UX:      Тап по пустой области видео показывает/прячет контролы; кнопки не скрывают оверлей.
//// Render:  Всегда через RenderTexture. Без двойных аллокаций.
//// Back:    Кнопка слева сверху останавливает видео и скрывает панель видео.
//// Notes:   Если в кэше .enc → расшифровка во временный .mp4, путь file:///
//// ============================================

//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.Video;
//using UnityEngine.InputSystem;
//using TMPro;
//using System.Collections;
//using System.IO;
//using System.Collections.Generic;

//[RequireComponent(typeof(AudioSource))]
//public class VideoStreamPlayer : MonoBehaviour
//{
//    [Header("Видео")]
//    [Tooltip("Оригинальный URL без расширения или с .mp4; расширение будет принудительно .mp4")]
//    public string videoURL = "";

//    [Header("Loading UI")]
//    public GameObject loadingPanel; // Спиннер (по умолчанию выключен в инспекторе)

//    // Core refs
//    [Header("Core Refs")]
//    public RawImage screen;                    // ОБЯЗАТЕЛЬНО присвоить RawImage
//    private RectTransform rectTransform;
//    private VideoPlayer videoPlayer;
//    private AspectRatioFitter fitter;
//    private RenderTexture rt;

//    // Controls
//    [Header("Иконки паузы/игры")]
//    public Sprite playIcon;
//    public Sprite pauseIcon;
//    private Image pauseButtonImage;

//    [Header("UI-контролы")]
//    public Button pauseButton;
//    public Button rewindButton;    // -10 сек
//    public Button forwardButton;   // +10 сек
//    public Button screenOrientationButton;
//    public Sprite enterLandscapeIcon;
//    public Sprite enterPortraitIcon;
//    private Image screenOrientationButtonImage;

//    [Header("Скорость")]
//    public Button speedButton;
//    public TextMeshProUGUI speedButtonText;
//    private readonly float[] speedOptions = { 1f, 1.25f, 1.5f, 2f, 0.5f };
//    private int speedIndex = 0;

//    [Header("Прогресс и время")]
//    public Slider progressSlider;
//    public TextMeshProUGUI timeText;
//    private bool isDraggingSlider;

//    [Header("Оверлей контролов")]
//    public CanvasGroup controlsOverlayGroup;
//    private bool controlsVisible;
//    private float lastToggleTime;
//    private const float toggleDebounce = 0.25f;
//    private Coroutine overlayFadeCoroutine;

//    [Header("Replay")]
//    public Button replayButton;

//    [Header("Назад")]
//    public Button backButton;           // Кнопка слева сверху
//    public GameObject videoContainer;   // Что скрывать при выходе (если пусто — берём gameObject)

//    private Vector2 lastScreenSize;
//    private ScreenOrientation lastOrientation;

//    // ===== Unity =====
//    void Awake()
//    {
//        if (!screen) screen = GetComponentInChildren<RawImage>(true);
//        rectTransform = screen ? screen.rectTransform : null;
//        fitter = screen ? (screen.GetComponent<AspectRatioFitter>() ?? screen.gameObject.AddComponent<AspectRatioFitter>()) : null;

//        // Поднимаем/конфигурим VideoPlayer на том же объекте, где RawImage легче всего его найти
//        videoPlayer = screen ? (screen.GetComponent<VideoPlayer>() ?? screen.gameObject.AddComponent<VideoPlayer>()) : gameObject.AddComponent<VideoPlayer>();
//        var audio = GetComponent<AudioSource>();

//        // Видеоплеер
//        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
//        videoPlayer.playOnAwake = false;
//        videoPlayer.waitForFirstFrame = true;
//        videoPlayer.skipOnDrop = true;
//        videoPlayer.isLooping = false;

//        // Аудио
//        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
//        audio.playOnAwake = false; audio.mute = false; audio.volume = 1f; audio.spatialBlend = 0f;
//        videoPlayer.controlledAudioTrackCount = 1; videoPlayer.EnableAudioTrack(0, true); videoPlayer.SetTargetAudioSource(0, audio);

//        // Подписки
//        videoPlayer.prepareCompleted += OnVideoPrepared;
//        videoPlayer.loopPointReached += OnVideoFinished;
//        videoPlayer.errorReceived += OnVideoError;

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
//        // Сброс оверлея при каждом повторном открытии панели
//        if (controlsOverlayGroup != null)
//        {
//            if (overlayFadeCoroutine != null) { StopCoroutine(overlayFadeCoroutine); overlayFadeCoroutine = null; }
//            controlsOverlayGroup.alpha = 0f;
//            controlsOverlayGroup.blocksRaycasts = false;
//        }
//        controlsVisible = false;
//        ToggleUI(false);
//    }

//    void Start()
//    {
//        lastScreenSize = new Vector2(Screen.width, Screen.height);
//        lastOrientation = Screen.orientation;

//        // Кнопки
//        pauseButtonImage = pauseButton ? pauseButton.GetComponent<Image>() : null;
//        if (pauseButton) { pauseButton.onClick.RemoveAllListeners(); pauseButton.onClick.AddListener(TogglePause); if (pauseButtonImage) pauseButtonImage.sprite = pauseIcon; }
//        if (rewindButton) { rewindButton.onClick.RemoveAllListeners(); rewindButton.onClick.AddListener(() => Skip(-10)); }
//        if (forwardButton) { forwardButton.onClick.RemoveAllListeners(); forwardButton.onClick.AddListener(() => Skip(10)); }
//        if (screenOrientationButton)
//        {
//            screenOrientationButton.onClick.RemoveAllListeners();
//            screenOrientationButton.onClick.AddListener(ToggleOrientation);
//            screenOrientationButtonImage = screenOrientationButton.GetComponent<Image>();
//            UpdateOrientationButtonIcon();
//        }
//        if (speedButton) { speedButton.onClick.RemoveAllListeners(); speedButton.onClick.AddListener(ToggleSpeed); if (speedButtonText) speedButtonText.text = $"{speedOptions[speedIndex]}x"; }
//        if (progressSlider)
//        {
//            progressSlider.onValueChanged.RemoveAllListeners();
//            progressSlider.onValueChanged.AddListener(OnSliderChanged);
//        }
//        if (replayButton) { replayButton.onClick.RemoveAllListeners(); replayButton.onClick.AddListener(OnReplayButtonClicked); replayButton.gameObject.SetActive(false); }
//        if (backButton)
//        {
//            backButton.onClick.RemoveAllListeners();
//            backButton.onClick.AddListener(CloseVideoPanel);
//            backButton.gameObject.SetActive(false); // back следует за оверлеем
//        }

//        // Изначально контролы скрыты
//        if (controlsOverlayGroup != null)
//        {
//            HideControls();
//            controlsOverlayGroup.alpha = 0f;
//            controlsOverlayGroup.blocksRaycasts = false;
//        }

//        if (!string.IsNullOrEmpty(videoURL))
//            StartCoroutine(PlayWithLoading(videoURL)); // запуск с экраном загрузки
//    }

//    // ===== Публичный API =====
//    public void SetVideoURL(string url)
//    {
//        videoURL = url;
//        StopAllCoroutines();
//        StartCoroutine(PlayWithLoading(url));
//        if (videoContainer != null && !videoContainer.activeSelf) videoContainer.SetActive(true);
//    }

//    public void Pause() { if (videoPlayer != null) videoPlayer.Pause(); }
//    public void StopPlayback() { if (videoPlayer != null) { videoPlayer.Stop(); if (screen) screen.texture = null; } }

//    public void CloseVideoPanel()
//    {
//        StopAllCoroutines();
//        try { if (videoPlayer != null) videoPlayer.Stop(); } catch { }
//        if (screen != null) screen.texture = null;

//        if (overlayFadeCoroutine != null) { StopCoroutine(overlayFadeCoroutine); overlayFadeCoroutine = null; }
//        if (controlsOverlayGroup != null) { controlsOverlayGroup.alpha = 0f; controlsOverlayGroup.blocksRaycasts = false; }
//        controlsVisible = false;
//        ToggleUI(false);

//        if (loadingPanel != null) loadingPanel.SetActive(false);
//        ReleaseRT();
//        if (videoContainer != null) videoContainer.SetActive(false);
//    }

//    // ===== Основная логика: cache-first (шифрованный кэш) =====
//    private IEnumerator PlayWithLoading(string url)
//    {
//        if (loadingPanel != null) loadingPanel.SetActive(true);

//        // Приводим к .mp4 (добавим/заменим расширение перед ?alt=media)
//        string reqUrl = ForceMp4Url(url);
//        const string ext = ".mp4";

//        // Канонический ключ кэша
//        string key = CanonicalCacheKey(reqUrl);

//        // 1) Проверяем наличие в кэше (в .enc)
//        string encPath = CacheService.GetCachedPath(key, ext); // вернёт путь к .enc или null
//        string localPlain = null;

//        if (!string.IsNullOrEmpty(encPath) && File.Exists(encPath))
//        {
//            // Расшифровать во временный plaintext .mp4
//            localPlain = CacheService.DecryptToTemp(encPath, ext);
//        }
//        else
//        {
//            // 2) Если кэша нет — качаем (временный plaintext), шифруем, и сразу играем из plaintext
//            bool ok = false; string tmp = null;
//            yield return CacheService.GetFile(
//                reqUrl,
//                cacheKey: key,
//                onDone: path => { ok = true; tmp = path; },
//                forcedExt: ext,
//                onError: _ => { ok = false; }
//            );
//            if (!ok)
//            {
//                if (loadingPanel != null) loadingPanel.SetActive(false);
//                yield break;
//            }
//            localPlain = tmp;
//        }

//        // 3) Готовим и запускаем (через RT) из локального файла
//        videoPlayer.source = VideoSource.Url;
//        videoPlayer.url = AsFileUrl(localPlain);

//        // Сброс старого RT/текстур
//        ReleaseRT();
//        if (screen) screen.texture = null;
//        videoPlayer.targetTexture = null;
//        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

//        // Prepare (с таймаутом)
//        float timeout = 12f, t = 0f;
//        videoPlayer.Prepare();
//        while (!videoPlayer.isPrepared && t < timeout) { t += Time.deltaTime; yield return null; }
//        if (!videoPlayer.isPrepared)
//        {
//            Debug.LogError("[VideoStreamPlayer] Prepare timeout. Possibly unsupported codec/container.");
//            if (loadingPanel != null) loadingPanel.SetActive(false);
//            yield break;
//        }

//        // Создаём RT под фактический размер (один раз)
//        int w = Mathf.Max(2, (int)(videoPlayer.width > 0 ? videoPlayer.width : 1280));
//        int h = Mathf.Max(2, (int)(videoPlayer.height > 0 ? videoPlayer.height : 720));
//        EnsureRT(w, h);
//        videoPlayer.targetTexture = rt;
//        if (screen) screen.texture = rt;

//        // GO
//        videoPlayer.playbackSpeed = speedOptions[speedIndex];
//        videoPlayer.Play();

//        if (loadingPanel != null) loadingPanel.SetActive(false);
//        UpdateVideoSize(); // корректно выставить аспект
//    }

//    // ===== События VideoPlayer =====
//    private void OnVideoPrepared(VideoPlayer vp)
//    {
//        // Размер мог смениться — проверим RT, но не переаллочим без нужды
//        int w = Mathf.Max(2, (int)(vp.width > 0 ? vp.width : 1280));
//        int h = Mathf.Max(2, (int)(vp.height > 0 ? vp.height : 720));
//        EnsureRT(w, h);
//        vp.targetTexture = rt;
//        if (screen) screen.texture = rt;

//        UpdateVideoSize();
//    }

//    private void OnVideoFinished(VideoPlayer vp)
//    {
//        HideControls();
//        if (replayButton != null)
//        {
//            replayButton.gameObject.SetActive(true);
//            FadeOverlay(controlsOverlayGroup, 1f, 0.25f);
//        }
//    }

//    private void OnVideoError(VideoPlayer vp, string msg)
//    {
//        Debug.LogError("[VideoStreamPlayer] Error: " + msg);
//        if (loadingPanel != null) loadingPanel.SetActive(false);
//    }

//    // ===== Update =====
//    void Update()
//    {
//        // Тоги оверлея: поддерживаем и тач, и мышь
//        HandlePointerToggle();

//        // Реакция на смену размера/ориентации
//        if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
//        {
//            lastScreenSize = new Vector2(Screen.width, Screen.height);
//            UpdateVideoSize();
//        }
//        if (Screen.orientation != lastOrientation)
//        {
//            lastOrientation = Screen.orientation;
//            UpdateOrientationButtonIcon();
//            UpdateVideoSize();
//        }

//        // Прогресс
//        if (!isDraggingSlider && videoPlayer && videoPlayer.length > 0 && progressSlider != null)
//            progressSlider.value = (float)(videoPlayer.time / videoPlayer.length);

//        UpdateTimeText();
//        if (screen != null && screen.texture == null && rt != null) screen.texture = rt; // страховка
//    }

//    private void HandlePointerToggle()
//    {
//        bool pressed = false;
//        Vector2 pos = default;

//        // Touch
//        if (Touchscreen.current != null)
//        {
//            var touch = Touchscreen.current.primaryTouch;
//            if (touch.press.wasPressedThisFrame) { pressed = true; pos = touch.position.ReadValue(); }
//        }
//        // Mouse
//        if (!pressed && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
//        { pressed = true; pos = Mouse.current.position.ReadValue(); }

//        if (!pressed) return;
//        if (Time.unscaledTime - lastToggleTime < toggleDebounce) return;

//        // Если тап по любой кнопке — игнорируем, не прячем оверлей
//        if (HitUIButton(pos)) return;

//        lastToggleTime = Time.unscaledTime;
//        ToggleControls();
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
//        if (controlsOverlayGroup == null) return;
//        if (replayButton != null && replayButton.gameObject.activeSelf) return;
//        ToggleUI(true);
//        FadeOverlay(controlsOverlayGroup, 1f, 0.2f);
//        controlsVisible = true;
//    }

//    private void HideControls()
//    {
//        if (controlsOverlayGroup == null) return;
//        ToggleUI(false);
//        FadeOverlay(controlsOverlayGroup, 0f, 0.2f);
//        controlsVisible = false;
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
//        if (backButton) backButton.gameObject.SetActive(on); // back следует за оверлеем
//    }

//    private void TogglePause()
//    {
//        if (!videoPlayer || !videoPlayer.isPrepared) return;
//        if (videoPlayer.isPlaying)
//        {
//            videoPlayer.Pause();
//            if (pauseButtonImage) pauseButtonImage.sprite = playIcon;
//        }
//        else
//        {
//            videoPlayer.Play();
//            if (pauseButtonImage) pauseButtonImage.sprite = pauseIcon;
//        }
//    }

//    private void Skip(double seconds)
//    {
//        if (!videoPlayer || !videoPlayer.isPrepared || videoPlayer.length <= 0) return;
//        double target = Mathf.Clamp((float)(videoPlayer.time + seconds), 0f, (float)videoPlayer.length);
//        videoPlayer.time = target;
//        // Если стояли на паузе — не автозапускать
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
//        speedIndex = (speedIndex + 1) % speedOptions.Length;
//        float newSpeed = speedOptions[speedIndex];
//        if (videoPlayer) videoPlayer.playbackSpeed = newSpeed;
//        if (speedButtonText) speedButtonText.text = $"{newSpeed}x";
//    }

//    private void OnSliderChanged(float value)
//    {
//        if (!isDraggingSlider || videoPlayer == null || videoPlayer.length <= 0) return;
//        videoPlayer.time = value * videoPlayer.length;
//    }

//    // Для EventTrigger на слайдере (BeginDrag/EndDrag)
//    public void OnBeginDrag() { isDraggingSlider = true; }
//    public void OnEndDrag() { isDraggingSlider = false; }

//    private void UpdateVideoSize()
//    {
//        if (!fitter || !videoPlayer) return;
//        float vw = (videoPlayer.width > 0) ? videoPlayer.width : (rt != null ? rt.width : 0);
//        float vh = (videoPlayer.height > 0) ? videoPlayer.height : (rt != null ? rt.height : 0);
//        if (vw <= 0 || vh <= 0) return;

//        float aspect = vw / vh;
//        if (aspect < 1f)
//        {
//            fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
//            fitter.aspectRatio = aspect;
//        }
//        else
//        {
//            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
//            fitter.aspectRatio = aspect;
//        }
//        if (screen && screen.texture != rt && rt != null) screen.texture = rt;
//    }

//    private void UpdateTimeText()
//    {
//        if (!timeText || videoPlayer == null) return;
//        if (videoPlayer.length > 0)
//        {
//            int m1 = (int)(videoPlayer.time / 60), s1 = (int)(videoPlayer.time % 60);
//            int m2 = (int)(videoPlayer.length / 60), s2 = (int)(videoPlayer.length % 60);
//            timeText.text = $"{m1}:{s1:D2} / {m2}:{s2:D2}";
//        }
//        else timeText.text = "0:00 / 0:00";
//    }

//    // ===== Fade =====
//    private IEnumerator FadeCanvasGroup(CanvasGroup g, float target, float dur)
//    {
//        float start = g.alpha, t = 0f; g.blocksRaycasts = target > 0f;
//        while (t < dur) { t += Time.deltaTime; g.alpha = Mathf.Lerp(start, target, t / dur); yield return null; }
//        g.alpha = target;
//    }

//    private void FadeOverlay(CanvasGroup g, float target, float dur)
//    {
//        if (!g) return;
//        if (overlayFadeCoroutine != null) StopCoroutine(overlayFadeCoroutine);
//        overlayFadeCoroutine = StartCoroutine(FadeCanvasGroup(g, target, dur));
//    }

//    // ===== Helpers =====
//    private static string ForceMp4Url(string url)
//    {
//        if (string.IsNullOrEmpty(url)) return url;
//        if (url.StartsWith("file:")) return url;
//        int q = url.IndexOf('?');
//        string baseUrl = q >= 0 ? url.Substring(0, q) : url;
//        string query = q >= 0 ? url.Substring(q) : string.Empty;
//        string ext = Path.GetExtension(baseUrl);
//        if (string.IsNullOrEmpty(ext)) return baseUrl + ".mp4" + query;
//        if (!ext.Equals(".mp4", System.StringComparison.OrdinalIgnoreCase))
//        {
//            baseUrl = baseUrl.Substring(0, baseUrl.Length - ext.Length) + ".mp4";
//            return baseUrl + query;
//        }
//        return url;
//    }

//    private static string CanonicalCacheKey(string urlMp4)
//    {
//        return "video:" + urlMp4.Replace("%2F", "%2f"); // стабилизируем регистр
//    }

//    private static string AsFileUrl(string localPath)
//    {
//        if (string.IsNullOrEmpty(localPath)) return localPath;
//        if (localPath.StartsWith("http")) return localPath;
//        if (localPath.StartsWith("file:///")) return localPath.Replace('\\', '/');
//        var norm = localPath.Replace('\\', '/');
//        return "file:///" + norm.TrimStart('/');
//    }

//    private void EnsureRT(int w, int h)
//    {
//        if (rt != null && rt.width == w && rt.height == h) return;
//        ReleaseRT();
//        rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
//        {
//            useDynamicScale = false,
//            autoGenerateMips = false,
//            antiAliasing = 1,
//            wrapMode = TextureWrapMode.Clamp
//        };
//        rt.Create();
//    }

//    private void ReleaseRT()
//    {
//        if (videoPlayer) videoPlayer.targetTexture = null;
//        if (rt != null)
//        {
//            if (rt.IsCreated()) rt.Release();
//            Destroy(rt);
//            rt = null;
//        }
//    }
//}

// ============================================
// File: Assets/Scripts/VideoStreamPlayer.cs
// Purpose: Плавное воспроизведение: cache-first, НО если нет кэша — стрим онлайн без RT
// Policy:  MP4-only (H.264+AAC). Кэш: локально; оффлайн работает.
// UX:      Тап по пустой области видео показывает/прячет контролы; кнопки не трогают оверлей.
// Render:  Локально → RenderTexture; Онлайн-стрим → APIOnly (без RT).
// Back:    Кнопка слева сверху останавливает видео и скрывает панель.
// Notes:   Поиск кэша по вариантам ключа; skipOnDrop=true; без двойных аллокаций.
// ============================================
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem; // новый Input System
using TMPro;
using System.Collections;
using System.IO;
using System.Collections.Generic;

[RequireComponent(typeof(RawImage), typeof(VideoPlayer), typeof(AudioSource))]
public class VideoStreamPlayer : MonoBehaviour
{
    [Header("Видео")] public string videoURL = "";

    [Header("Loading UI")] public GameObject loadingPanel;   // Панель со спиннером (SetActive(false) по умолчанию)

    // Core refs
    private RawImage rawImage;
    private RectTransform rectTransform;
    private VideoPlayer videoPlayer;
    private AspectRatioFitter fitter;
    private RenderTexture rt; // RT — только при локальном воспроизведении

    // Controls (опционально — можно не заполнять)
    [Header("Иконки паузы/игры")] public Sprite playIcon; public Sprite pauseIcon; private Image pauseButtonImage;
    [Header("UI-контролы")] public Button pauseButton; public Button rewindButton; public Button forwardButton; public Button screenOrientationButton;
    public Sprite enterLandscapeIcon; public Sprite enterPortraitIcon; private Image screenOrientationButtonImage;
    [Header("Скорость")] public Button speedButton; public TextMeshProUGUI speedButtonText; private readonly float[] speedOptions = { 1f, 1.25f, 1.5f, 2f, 0.5f }; private int speedIndex = 0;
    [Header("Прогресс и время")] public Slider progressSlider; public TextMeshProUGUI timeText; private bool isDraggingSlider;
    [Header("Затемнение контролов")] public CanvasGroup controlsOverlayGroup; private bool controlsVisible; private float lastToggleTime; private const float toggleDebounce = 0.25f; private Coroutine overlayFadeCoroutine;
    [Header("Replay")] public Button replayButton;

    [Header("Назад")] public Button backButton;                         // Кнопка слева сверху
    public GameObject videoContainer;                                    // Что скрывать при выходе (если пусто — берём gameObject)

    [Header("Стриминг при отсутствии кэша")]
    [Tooltip("Если файла в кэше нет и интернет есть — сразу стримим онлайн (APIOnly, без RT).")]
    public bool streamIfNotCached = true;

    private bool usingApiOnly = false; // true — когда играем онлайн без RT
    private Vector2 lastScreenSize;
    private ScreenOrientation lastOrientation;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        rectTransform = rawImage.rectTransform;
        fitter = rawImage.GetComponent<AspectRatioFitter>() ?? rawImage.gameObject.AddComponent<AspectRatioFitter>();

        videoPlayer = GetComponent<VideoPlayer>();
        var audio = GetComponent<AudioSource>();

        // Видеоплеер — базовые настройки
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;           // ключ к плавности на слабом железе
        videoPlayer.isLooping = false;

        // Аудио → AudioSource
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        audio.playOnAwake = false; audio.mute = false; audio.volume = 1f; audio.spatialBlend = 0f;
        videoPlayer.controlledAudioTrackCount = 1; videoPlayer.EnableAudioTrack(0, true); videoPlayer.SetTargetAudioSource(0, audio);

        // События
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
        // Сброс оверлея при каждом повторном открытии панели
        if (controlsOverlayGroup != null)
        {
            if (overlayFadeCoroutine != null) { StopCoroutine(overlayFadeCoroutine); overlayFadeCoroutine = null; }
            controlsOverlayGroup.alpha = 0f;
            controlsOverlayGroup.blocksRaycasts = false;
        }
        controlsVisible = false;
        ToggleUI(false);
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
            screenOrientationButton.onClick.RemoveAllListeners();
            screenOrientationButton.onClick.AddListener(ToggleOrientation);
            screenOrientationButtonImage = screenOrientationButton.GetComponent<Image>();
            UpdateOrientationButtonIcon();
        }
        if (speedButton) { speedButton.onClick.RemoveAllListeners(); speedButton.onClick.AddListener(ToggleSpeed); if (speedButtonText) speedButtonText.text = $"{speedOptions[speedIndex]}Х"; }
        if (progressSlider)
        {
            progressSlider.onValueChanged.RemoveAllListeners();
            progressSlider.onValueChanged.AddListener(OnSliderChanged);
        }
        if (replayButton) { replayButton.onClick.RemoveAllListeners(); replayButton.onClick.AddListener(OnReplayButtonClicked); replayButton.gameObject.SetActive(false); }
        if (backButton)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(CloseVideoPanel);
            backButton.gameObject.SetActive(false); // back всегда следует за оверлеем
        }

        // Изначально — как раньше — контролы скрыты
        if (controlsOverlayGroup != null)
        {
            HideControls();
            controlsOverlayGroup.alpha = 0f;
            controlsOverlayGroup.blocksRaycasts = false;
        }

        if (!string.IsNullOrEmpty(videoURL))
            StartCoroutine(PlayWithLoading(videoURL)); // запуск с экраном загрузки
    }

    // === Публичный API ===
    public void SetVideoURL(string url)
    {
        videoURL = url;
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

        // Жёсткий сброс оверлея, чтобы не оставалось затемнения после SetActive(false)
        if (overlayFadeCoroutine != null) { StopCoroutine(overlayFadeCoroutine); overlayFadeCoroutine = null; }
        if (controlsOverlayGroup != null) { controlsOverlayGroup.alpha = 0f; controlsOverlayGroup.blocksRaycasts = false; }
        controlsVisible = false;
        ToggleUI(false);

        if (loadingPanel != null) loadingPanel.SetActive(false);
        ReleaseRT();
        if (videoContainer != null) videoContainer.SetActive(false);
    }

    // === Основная логика: cache-first, НО при отсутствии кэша — онлайн-стрим ===
    private IEnumerator PlayWithLoading(string url)
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);

        // Приводим к .mp4 (добавим/заменим расширение перед ?alt=media)
        string reqUrl = ForceMp4Url(url);
        const string ext = ".mp4";

        // --- Cache-first: ищем в кэше по всем безопасным вариантам ключа ---
        string encPath = FindCachedVideoPath(reqUrl, ext); // это путь к .enc, если есть
        usingApiOnly = false;

        string localPlain = null;

        // --- Если не нашли в кэше ---
        if (string.IsNullOrEmpty(encPath))
        {
            if (IsOnline() && streamIfNotCached)
            {
                // → Онлайн-стрим без RT (минимум аллокаций)
                videoPlayer.renderMode = VideoRenderMode.APIOnly;
                videoPlayer.source = VideoSource.Url;
                videoPlayer.url = reqUrl;
                usingApiOnly = true;

                // Prepare (с таймаутом)
                float timeout = 12f, t = 0f;
                videoPlayer.Prepare();
                while (!videoPlayer.isPrepared && t < timeout) { t += Time.deltaTime; yield return null; }
                if (!videoPlayer.isPrepared)
                {
                    Debug.LogError("[VideoStreamPlayer] Prepare timeout (stream).");
                    if (loadingPanel != null) loadingPanel.SetActive(false);
                    yield break;
                }

                rawImage.texture = videoPlayer.texture; // без RT используем texture напрямую
                UpdateVideoSize();

                if (loadingPanel != null) loadingPanel.SetActive(false);
                videoPlayer.playbackSpeed = speedOptions[speedIndex];
                videoPlayer.Play();
                yield break;
            }
            else
            {
                // → Оффлайн ИЛИ стрим отключён: докачиваем и используем локально
                bool ok = false;
                yield return CacheService.GetFile(
                    reqUrl,
                    cacheKey: CanonicalCacheKey(reqUrl), // единый стабильный ключ
                    onDone: path => { ok = true; localPlain = path; },
                    forcedExt: ext,
                    onError: _ => { ok = false; }
                );
                if (!ok)
                {
                    if (loadingPanel != null) loadingPanel.SetActive(false);
                    yield break;
                }
            }
        }
        else
        {
            // --- Кэша .enc достаточно: дешифруем в детерминированный temp и используем локально ---
            localPlain = CacheService.GetOrMakePlainTemp(encPath, ext);
            if (string.IsNullOrEmpty(localPlain) || !File.Exists(localPlain))
            {
                if (loadingPanel != null) loadingPanel.SetActive(false);
                Debug.LogError("[VideoStreamPlayer] Failed to decrypt cached video.");
                yield break;
            }
        }

        // --- Локальный файл: играем из file:/// через RenderTexture ---
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = AsFileUrl(localPlain);
        usingApiOnly = false;

        // Сброс старого RT/текстур
        ReleaseRT();
        rawImage.texture = null;
        videoPlayer.targetTexture = null;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        // Prepare (с таймаутом)
        float timeout2 = 12f, t2 = 0f;
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared && t2 < timeout2) { t2 += Time.deltaTime; yield return null; }
        if (!videoPlayer.isPrepared)
        {
            Debug.LogError("[VideoStreamPlayer] Prepare timeout (local).");
            if (loadingPanel != null) loadingPanel.SetActive(false);
            yield break;
        }

        // Создаём RT под фактический размер
        int w = Mathf.Max(2, (int)(videoPlayer.width > 0 ? videoPlayer.width : 1280));
        int h = Mathf.Max(2, (int)(videoPlayer.height > 0 ? videoPlayer.height : 720));
        CreateRT(w, h);
        videoPlayer.targetTexture = rt;
        rawImage.texture = rt;

        if (loadingPanel != null) loadingPanel.SetActive(false);
        videoPlayer.playbackSpeed = speedOptions[speedIndex];
        videoPlayer.Play();
        UpdateVideoSize();
    }

    // === Вспомогательное ===
    private void OnVideoPrepared(VideoPlayer vp)
    {
        if (usingApiOnly)
        {
            // Стрим: просто обновляем текстуру и аспект
            rawImage.texture = vp.texture;
            UpdateVideoSize();
            return;
        }

        // Локально (RT)
        int w = Mathf.Max(2, (int)(vp.width > 0 ? vp.width : 1280));
        int h = Mathf.Max(2, (int)(vp.height > 0 ? vp.height : 720));
        if (rt == null || rt.width != w || rt.height != h)
        {
            CreateRT(w, h);
            vp.targetTexture = rt;
        }
        rawImage.texture = rt;
        UpdateVideoSize();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        HideControls();
        if (replayButton != null)
        {
            replayButton.gameObject.SetActive(true);
            FadeOverlay(controlsOverlayGroup, 1f, 0.25f);
        }
    }

    private void OnVideoError(VideoPlayer vp, string msg)
    {
        Debug.LogError("[VideoStreamPlayer] Error: " + msg);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    void Update()
    {
        // Тап по свободной области видео показывает/прячет контролы; попадание по кнопкам — игнорируется
        HandleTouch();

        if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
        {
            lastScreenSize = new Vector2(Screen.width, Screen.height);
            UpdateVideoSize();
        }
        if (Screen.orientation != lastOrientation)
        {
            lastOrientation = Screen.orientation;
            UpdateOrientationButtonIcon();
        }

        if (!isDraggingSlider && videoPlayer.length > 0 && progressSlider != null)
            progressSlider.value = (float)(videoPlayer.time / videoPlayer.length);

        UpdateTimeText();

        // Подстраховка для APIOnly (иногда texture появляется с задержкой)
        if (usingApiOnly && rawImage.texture == null && videoPlayer.texture != null)
            rawImage.texture = videoPlayer.texture;
    }

    private void HandleTouch()
    {
        bool pressed = false; Vector2 pos = default;
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame) { pressed = true; pos = touch.position.ReadValue(); }
        }
        if (!pressed && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        { pressed = true; pos = Mouse.current.position.ReadValue(); }

        if (!pressed) return;
        if (Time.unscaledTime - lastToggleTime < toggleDebounce) return;

        // Если тап по любой кнопке — игнорируем
        if ((pauseButton && Hit(pauseButton.transform as RectTransform, pos)) ||
            (rewindButton && Hit(rewindButton.transform as RectTransform, pos)) ||
            (forwardButton && Hit(forwardButton.transform as RectTransform, pos)) ||
            (progressSlider && Hit(progressSlider.transform as RectTransform, pos)) ||
            (screenOrientationButton && Hit(screenOrientationButton.transform as RectTransform, pos)) ||
            (speedButton && Hit(speedButton.transform as RectTransform, pos)) ||
            (replayButton && Hit(replayButton.transform as RectTransform, pos)) ||
            (backButton && Hit(backButton.transform as RectTransform, pos)))
            return;

        lastToggleTime = Time.unscaledTime;
        ToggleControls();
    }

    private bool Hit(RectTransform rt, Vector2 p) => rt && RectTransformUtility.RectangleContainsScreenPoint(rt, p, null);

    private void ToggleControls() { if (controlsVisible) HideControls(); else ShowControls(); }

    private void ShowControls()
    {
        if (controlsOverlayGroup == null) return;
        if (replayButton != null && replayButton.gameObject.activeSelf) return;
        ToggleUI(true);
        FadeOverlay(controlsOverlayGroup, 1f, 0.2f);
        controlsVisible = true;
    }

    private void HideControls()
    {
        if (controlsOverlayGroup == null) return;
        ToggleUI(false);
        FadeOverlay(controlsOverlayGroup, 0f, 0.2f);
        controlsVisible = false;
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
        if (backButton) backButton.gameObject.SetActive(on); // back всегда следует за оверлеем
    }

    private void TogglePause()
    {
        if (!videoPlayer || !videoPlayer.isPrepared) return;
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            if (pauseButtonImage) pauseButtonImage.sprite = playIcon;
        }
        else
        {
            videoPlayer.Play();
            if (pauseButtonImage) pauseButtonImage.sprite = pauseIcon;
        }
    }

    private void Skip(double seconds)
    {
        if (!videoPlayer || !videoPlayer.isPrepared || videoPlayer.length <= 0) return;
        double target = Mathf.Clamp((float)(videoPlayer.time + seconds), 0f, (float)videoPlayer.length);
        videoPlayer.time = target;
        // Если стояли на паузе — не автозапускать
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
        speedIndex = (speedIndex + 1) % speedOptions.Length;
        float newSpeed = speedOptions[speedIndex];
        if (videoPlayer) videoPlayer.playbackSpeed = newSpeed;
        if (speedButtonText) speedButtonText.text = $"{newSpeed}Х";
    }

    private void OnSliderChanged(float value)
    {
        if (!isDraggingSlider || videoPlayer == null || videoPlayer.length <= 0) return;
        videoPlayer.time = value * videoPlayer.length;
    }

    public void OnBeginDrag() { isDraggingSlider = true; }
    public void OnEndDrag() { isDraggingSlider = false; }

    private void UpdateVideoSize()
    {
        if (!fitter || !videoPlayer) return;
        float vw = (videoPlayer.width > 0) ? videoPlayer.width : (rt != null ? rt.width : 0);
        float vh = (videoPlayer.height > 0) ? videoPlayer.height : (rt != null ? rt.height : 0);
        if (usingApiOnly && videoPlayer.texture != null)
        {
            vw = videoPlayer.texture.width;
            vh = videoPlayer.texture.height;
        }
        if (vw <= 0 || vh <= 0) return;

        float aspect = vw / vh;
        if (aspect < 1f)
        {
            fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fitter.aspectRatio = aspect;
        }
        else
        {
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = aspect;
        }
        if (!usingApiOnly && rawImage && rawImage.texture != rt && rt != null) rawImage.texture = rt;
    }

    private void UpdateTimeText()
    {
        if (!timeText || videoPlayer == null) return;
        if (videoPlayer.length > 0)
        {
            int m1 = (int)(videoPlayer.time / 60), s1 = (int)(videoPlayer.time % 60);
            int m2 = (int)(videoPlayer.length / 60), s2 = (int)(videoPlayer.length % 60);
            timeText.text = $"{m1}:{s1:D2} / {m2}:{s2:D2}";
        }
        else timeText.text = "0:00 / 0:00";
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup g, float target, float dur)
    {
        float start = g.alpha, t = 0f; g.blocksRaycasts = target > 0f;
        while (t < dur) { t += Time.deltaTime; g.alpha = Mathf.Lerp(start, target, t / dur); yield return null; }
        g.alpha = target;
    }

    private void FadeOverlay(CanvasGroup g, float target, float dur)
    {
        if (!g) return;
        if (overlayFadeCoroutine != null) StopCoroutine(overlayFadeCoroutine);
        overlayFadeCoroutine = StartCoroutine(FadeCanvasGroup(g, target, dur));
    }

    // ===== Helpers =====
    private static bool IsOnline() => Application.internetReachability != NetworkReachability.NotReachable;

    // Принудительно приводим URL к .mp4: если расширение не .mp4 — заменяем/добавляем .mp4 перед ?alt=media
    private static string ForceMp4Url(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;
        if (url.StartsWith("file:")) return url;
        int q = url.IndexOf('?');
        string baseUrl = q >= 0 ? url.Substring(0, q) : url;
        string query = q >= 0 ? url.Substring(q) : string.Empty;
        string ext = Path.GetExtension(baseUrl);
        if (string.IsNullOrEmpty(ext)) return baseUrl + ".mp4" + query;
        if (!ext.Equals(".mp4", System.StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = baseUrl.Substring(0, baseUrl.Length - ext.Length) + ".mp4";
            return baseUrl + query;
        }
        return url;
    }

    // Канонический ключ для кэша (согласованный с префетчем)
    private static string CanonicalCacheKey(string urlMp4)
    {
        // используем .mp4 + нижний регистр для %2f → получим стабильный ключ
        return "video:" + urlMp4.Replace("%2F", "%2f");
    }

    // Ищем любой существующий вариант кэша (совместимость со старыми ключами)
    private static string FindCachedVideoPath(string urlMp4, string ext)
    {
        var candidates = new List<string>
        {
            urlMp4,
            urlMp4.Replace("%2F", "%2f"),
            urlMp4.Replace("%2f", "%2F"),
            urlMp4.Replace(".mp4", ""),                // без .mp4
            urlMp4.Replace(".mp4", "").Replace("%2F","%2f"),
            urlMp4.Replace(".mp4", "").Replace("%2f","%2F")
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
        if (localPath.StartsWith("http")) return localPath;
        if (localPath.StartsWith("file:///")) return localPath.Replace('\\', '/');
        var norm = localPath.Replace('\\', '/');
        return "file:///" + norm.TrimStart('/');
    }

    private void CreateRT(int w, int h)
    {
        ReleaseRT();
        rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
        {
            useDynamicScale = false,
            autoGenerateMips = false,
            antiAliasing = 1,
            wrapMode = TextureWrapMode.Clamp
        };
        rt.Create();
    }

    private void ReleaseRT()
    {
        if (videoPlayer) videoPlayer.targetTexture = null;
        if (rt != null)
        {
            if (rt.IsCreated()) rt.Release();
            Destroy(rt);
            rt = null;
        }
    }
}
