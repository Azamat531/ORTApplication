//// ============================================
//// File: Assets/Scripts/VideoStreamPlayer.cs
//// Clean version: НИКАКИХ UI-полей для загрузки, тихая автодокачка при первом запуске
////  - Если видео уже в кэше → сразу играем из локального файла
////  - Если нет → скачиваем в кэш (без индикаторов) и затем воспроизводим
//// ============================================
//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.Video;
//using UnityEngine.InputSystem;
//using TMPro;
//using System.Collections;

//[RequireComponent(typeof(RawImage), typeof(VideoPlayer), typeof(AudioSource))]
//public class VideoStreamPlayer : MonoBehaviour
//{
//    [Header("Видео")] public string videoURL = "";

//    private RawImage rawImage; private RectTransform rectTransform; private VideoPlayer videoPlayer; private AspectRatioFitter fitter; private Vector2 lastScreenSize;

//    [Header("Иконки паузы/игры")] public Sprite playIcon; public Sprite pauseIcon; private Image pauseButtonImage;
//    [Header("UI-контролы")] public Button pauseButton; public Button rewindButton; public Button forwardButton; public Button screenOrientationButton; public Sprite enterLandscapeIcon; public Sprite enterPortraitIcon; private Image screenOrientationButtonImage;
//    [Header("Скорость")] public Button speedButton; public TextMeshProUGUI speedButtonText; private readonly float[] speedOptions = { 1f, 1.25f, 1.5f, 2f, 0.5f }; private int speedIndex = 0;
//    [Header("Прогресс и время")] public Slider progressSlider; public TextMeshProUGUI timeText; private bool isDraggingSlider;
//    [Header("Затемнение контролов")] public CanvasGroup controlsOverlayGroup; private bool controlsVisible; private float lastToggleTime; private const float toggleDebounce = 0.3f; private Coroutine overlayFadeCoroutine;
//    [Header("Replay")] public Button replayButton;

//    private ScreenOrientation lastOrientation;

//    void Awake()
//    {
//        rawImage = GetComponent<RawImage>(); rectTransform = rawImage.rectTransform;
//        fitter = rawImage.GetComponent<AspectRatioFitter>() ?? rawImage.gameObject.AddComponent<AspectRatioFitter>();
//        videoPlayer = GetComponent<VideoPlayer>();
//        var audio = GetComponent<AudioSource>();

//        videoPlayer.renderMode = VideoRenderMode.APIOnly;
//        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;

//        // Audio route
//        audio.playOnAwake = false; audio.mute = false; audio.volume = 1f; audio.spatialBlend = 0f;
//        videoPlayer.controlledAudioTrackCount = 1; videoPlayer.EnableAudioTrack(0, true); videoPlayer.SetTargetAudioSource(0, audio);

//        videoPlayer.prepareCompleted += OnVideoPrepared; videoPlayer.loopPointReached += OnVideoFinished;
//        videoPlayer.playbackSpeed = speedOptions[speedIndex];
//    }

//    void Start()
//    {
//        lastScreenSize = new Vector2(Screen.width, Screen.height);
//        lastOrientation = Screen.orientation;

//        pauseButtonImage = pauseButton ? pauseButton.GetComponent<Image>() : null;
//        if (pauseButton) { pauseButton.onClick.RemoveAllListeners(); pauseButton.onClick.AddListener(TogglePause); if (pauseButtonImage) pauseButtonImage.sprite = pauseIcon; }
//        if (rewindButton) rewindButton.onClick.AddListener(() => Skip(-10));
//        if (forwardButton) forwardButton.onClick.AddListener(() => Skip(10));
//        if (screenOrientationButton) { screenOrientationButtonImage = screenOrientationButton.GetComponent<Image>(); screenOrientationButton.onClick.AddListener(ToggleOrientation); UpdateOrientationButtonIcon(); }
//        if (speedButton) { speedButton.onClick.AddListener(ToggleSpeed); if (speedButtonText) speedButtonText.text = $"{speedOptions[speedIndex]}Х"; }
//        if (progressSlider) progressSlider.onValueChanged.AddListener(OnSliderChanged);
//        if (replayButton) { replayButton.onClick.AddListener(OnReplayButtonClicked); replayButton.gameObject.SetActive(false); }
//        if (controlsOverlayGroup) { HideControls(); controlsOverlayGroup.alpha = 0f; controlsOverlayGroup.blocksRaycasts = false; }

//        if (!string.IsNullOrEmpty(videoURL)) StartCoroutine(VideoPlaybackHelper.PlayVideoCached(videoPlayer, videoURL));
//    }

//    private void OnVideoPrepared(VideoPlayer vp)
//    { if (vp.audioTrackCount > 0) vp.EnableAudioTrack(0, true); rawImage.texture = vp.texture; vp.Play(); UpdateVideoSize(); }

//    void Update()
//    {
//        if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y) { lastScreenSize = new Vector2(Screen.width, Screen.height); UpdateVideoSize(); }
//        if (Screen.orientation != lastOrientation) { lastOrientation = Screen.orientation; UpdateOrientationButtonIcon(); }
//        HandleTouch();
//        if (!isDraggingSlider && videoPlayer.length > 0 && progressSlider) progressSlider.value = (float)(videoPlayer.time / videoPlayer.length);
//        UpdateTimeText();
//    }

//    public void Pause() { if (videoPlayer) videoPlayer.Pause(); }
//    public void StopPlayback() { if (videoPlayer) { videoPlayer.Stop(); rawImage.texture = null; } }

//    private void HandleTouch()
//    {
//        if (Touchscreen.current == null) return; var touch = Touchscreen.current.primaryTouch; if (!touch.press.wasPressedThisFrame) return; if (Time.time - lastToggleTime < toggleDebounce) return; lastToggleTime = Time.time;
//        Vector2 pos = touch.position.ReadValue();
//        if ((pauseButton && RectTransformUtility.RectangleContainsScreenPoint(pauseButton.transform as RectTransform, pos, null)) ||
//            (rewindButton && RectTransformUtility.RectangleContainsScreenPoint(rewindButton.transform as RectTransform, pos, null)) ||
//            (forwardButton && RectTransformUtility.RectangleContainsScreenPoint(forwardButton.transform as RectTransform, pos, null)) ||
//            (progressSlider && RectTransformUtility.RectangleContainsScreenPoint(progressSlider.transform as RectTransform, pos, null)) ||
//            (screenOrientationButton && RectTransformUtility.RectangleContainsScreenPoint(screenOrientationButton.transform as RectTransform, pos, null)) ||
//            (speedButton && RectTransformUtility.RectangleContainsScreenPoint(speedButton.transform as RectTransform, pos, null)) ||
//            (replayButton && RectTransformUtility.RectangleContainsScreenPoint(replayButton.transform as RectTransform, pos, null))) return;
//        ToggleControls();
//    }

//    private void ToggleControls() { if (controlsVisible) HideControls(); else ShowControls(); }
//    private void ShowControls() { if (!controlsOverlayGroup) return; if (replayButton && replayButton.gameObject.activeSelf) return; ToggleUI(true); FadeOverlay(controlsOverlayGroup, 1f, 0.25f); controlsVisible = true; }
//    private void HideControls() { if (!controlsOverlayGroup) return; ToggleUI(false); FadeOverlay(controlsOverlayGroup, 0f, 0.25f); controlsVisible = false; }
//    private void ToggleUI(bool on) { if (pauseButton) pauseButton.gameObject.SetActive(on); if (rewindButton) rewindButton.gameObject.SetActive(on); if (forwardButton) forwardButton.gameObject.SetActive(on); if (screenOrientationButton) screenOrientationButton.gameObject.SetActive(on); if (speedButton) speedButton.gameObject.SetActive(on); if (progressSlider) progressSlider.gameObject.SetActive(on); if (timeText) timeText.gameObject.SetActive(on); }

//    private void TogglePause() { if (!videoPlayer.isPrepared) return; if (videoPlayer.isPlaying) { videoPlayer.Pause(); if (pauseButtonImage) pauseButtonImage.sprite = playIcon; } else { videoPlayer.Play(); if (pauseButtonImage) pauseButtonImage.sprite = pauseIcon; } }
//    private void Skip(double seconds) { if (!videoPlayer.isPrepared || videoPlayer.length <= 0) return; bool wasPlaying = videoPlayer.isPlaying; double target = Mathf.Clamp((float)(videoPlayer.time + seconds), 0f, (float)videoPlayer.length); videoPlayer.time = target; if (wasPlaying) videoPlayer.Play(); else videoPlayer.Pause(); }
//    private void OnVideoFinished(VideoPlayer vp) { HideControls(); if (replayButton) { replayButton.gameObject.SetActive(true); FadeOverlay(controlsOverlayGroup, 1f, 0.25f); } }
//    private void OnReplayButtonClicked() { if (replayButton) replayButton.gameObject.SetActive(false); FadeOverlay(controlsOverlayGroup, 0f, 0.25f); videoPlayer.time = 0; videoPlayer.Play(); }
//    private void ToggleOrientation() { if (Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown) Screen.orientation = ScreenOrientation.LandscapeLeft; else Screen.orientation = ScreenOrientation.Portrait; }
//    private void UpdateOrientationButtonIcon() { if (!screenOrientationButtonImage) return; if (Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown) screenOrientationButtonImage.sprite = enterLandscapeIcon; else screenOrientationButtonImage.sprite = enterPortraitIcon; }
//    private void ToggleSpeed() { speedIndex = (speedIndex + 1) % speedOptions.Length; float newSpeed = speedOptions[speedIndex]; videoPlayer.playbackSpeed = newSpeed; if (speedButtonText) speedButtonText.text = $"{newSpeed}Х"; }
//    private void OnSliderChanged(float value) { if (!isDraggingSlider || videoPlayer.length <= 0) return; videoPlayer.time = value * videoPlayer.length; }
//    public void OnBeginDrag() => isDraggingSlider = true; public void OnEndDrag() => isDraggingSlider = false;

//    private void UpdateVideoSize() { if (!videoPlayer.isPrepared || videoPlayer.texture == null || !fitter) return; float vw = videoPlayer.texture.width; float vh = videoPlayer.texture.height; float aspect = vw / vh; if (aspect < 1f) { fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight; fitter.aspectRatio = aspect; } else { fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fitter.aspectRatio = aspect; } rawImage.texture = videoPlayer.texture; }
//    private void UpdateTimeText() { if (!timeText) return; if (videoPlayer.length > 0) { int m1 = (int)(videoPlayer.time / 60), s1 = (int)(videoPlayer.time % 60); int m2 = (int)(videoPlayer.length / 60), s2 = (int)(videoPlayer.length % 60); timeText.text = $"{m1}:{s1:D2} / {m2}:{s2:D2}"; } else timeText.text = "0:00 / 0:00"; }
//    private IEnumerator FadeCanvasGroup(CanvasGroup g, float target, float dur) { float start = g.alpha, t = 0f; g.blocksRaycasts = target > 0f; while (t < dur) { t += Time.deltaTime; g.alpha = Mathf.Lerp(start, target, t / dur); yield return null; } g.alpha = target; }
//    private void FadeOverlay(CanvasGroup g, float target, float dur) { if (!g) return; if (overlayFadeCoroutine != null) StopCoroutine(overlayFadeCoroutine); overlayFadeCoroutine = StartCoroutine(FadeCanvasGroup(g, target, dur)); }

//    public void SetVideoURL(string url)
//    {
//        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
//        videoURL = url;
//        StopAllCoroutines();
//        StartCoroutine(VideoPlaybackHelper.PlayVideoCached(videoPlayer, videoURL));
//    }
//}

// ============================================
// File: Assets/Scripts/VideoStreamPlayer.cs
// Purpose: Проигрывание видео с экраном загрузки перед стартом
// Policy:  "CachedWithDownload" — если в кэше нет, скачать и играть
// ============================================
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.IO;

[RequireComponent(typeof(RawImage), typeof(VideoPlayer), typeof(AudioSource))]
public class VideoStreamPlayer : MonoBehaviour
{
    [Header("Видео")]
    public string videoURL = "";

    [Header("Loading UI")]
    public GameObject loadingPanel;   // Панель со спиннером/лоадером (SetActive(false) по умолчанию)

    // Core refs
    private RawImage rawImage;
    private RectTransform rectTransform;
    private VideoPlayer videoPlayer;
    private AspectRatioFitter fitter;

    // Controls (опционально — можно не заполнять)
    [Header("Иконки паузы/игры")] public Sprite playIcon; public Sprite pauseIcon; private Image pauseButtonImage;
    [Header("UI-контролы")] public Button pauseButton; public Button rewindButton; public Button forwardButton; public Button screenOrientationButton;
    public Sprite enterLandscapeIcon; public Sprite enterPortraitIcon; private Image screenOrientationButtonImage;
    [Header("Скорость")] public Button speedButton; public TextMeshProUGUI speedButtonText; private readonly float[] speedOptions = { 1f, 1.25f, 1.5f, 2f, 0.5f }; private int speedIndex = 0;
    [Header("Прогресс и время")] public Slider progressSlider; public TextMeshProUGUI timeText; private bool isDraggingSlider;
    [Header("Затемнение контролов")] public CanvasGroup controlsOverlayGroup; private bool controlsVisible; private float lastToggleTime; private const float toggleDebounce = 0.3f; private Coroutine overlayFadeCoroutine;
    [Header("Replay")] public Button replayButton;

    private Vector2 lastScreenSize;
    private ScreenOrientation lastOrientation;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        rectTransform = rawImage.rectTransform;
        fitter = rawImage.GetComponent<AspectRatioFitter>() ?? rawImage.gameObject.AddComponent<AspectRatioFitter>();

        videoPlayer = GetComponent<VideoPlayer>();
        var audio = GetComponent<AudioSource>();

        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;

        // Audio route
        audio.playOnAwake = false; audio.mute = false; audio.volume = 1f; audio.spatialBlend = 0f;
        videoPlayer.controlledAudioTrackCount = 1; videoPlayer.EnableAudioTrack(0, true); videoPlayer.SetTargetAudioSource(0, audio);

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnVideoFinished;

        videoPlayer.playbackSpeed = speedOptions[speedIndex];
    }

    void Start()
    {
        lastScreenSize = new Vector2(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;

        pauseButtonImage = pauseButton ? pauseButton.GetComponent<Image>() : null;
        if (pauseButton) { pauseButton.onClick.RemoveAllListeners(); pauseButton.onClick.AddListener(TogglePause); if (pauseButtonImage) pauseButtonImage.sprite = pauseIcon; }
        if (rewindButton) rewindButton.onClick.AddListener(() => Skip(-10));
        if (forwardButton) forwardButton.onClick.AddListener(() => Skip(10));
        if (screenOrientationButton) { screenOrientationButtonImage = screenOrientationButton.GetComponent<Image>(); screenOrientationButton.onClick.AddListener(ToggleOrientation); UpdateOrientationButtonIcon(); }
        if (speedButton) { speedButton.onClick.AddListener(ToggleSpeed); if (speedButtonText) speedButtonText.text = $"{speedOptions[speedIndex]}Х"; }
        if (progressSlider) progressSlider.onValueChanged.AddListener(OnSliderChanged);
        if (replayButton) { replayButton.onClick.AddListener(OnReplayButtonClicked); replayButton.gameObject.SetActive(false); }
        if (controlsOverlayGroup) { HideControls(); controlsOverlayGroup.alpha = 0f; controlsOverlayGroup.blocksRaycasts = false; }

        if (!string.IsNullOrEmpty(videoURL))
            StartCoroutine(PlayWithLoading(videoURL)); // ← стартуем с экраном загрузки
    }

    // === Публичный API ===
    public void SetVideoURL(string url)
    {
        videoURL = url;
        StopAllCoroutines();
        StartCoroutine(PlayWithLoading(url)); // ← всегда через лоадер
    }

    public void Pause() { if (videoPlayer) videoPlayer.Pause(); }
    public void StopPlayback() { if (videoPlayer) { videoPlayer.Stop(); rawImage.texture = null; } }

    // === Основная логика с экраном загрузки ===
    private IEnumerator PlayWithLoading(string url)
    {
        if (loadingPanel) loadingPanel.SetActive(true);

        // 1) Если в кэше уже есть — сразу играем
        string ext = GuessExt(url, ".mp4");
        string cached = CacheService.GetCachedPath("video:" + url, ext);

        // 2) Если нет — докачиваем (если есть интернет)
        if (string.IsNullOrEmpty(cached))
        {
            if (IsOnline())
            {
                bool ok = false;
                yield return CacheService.GetFile(
                    url, "video:" + url,
                    path => { ok = true; cached = path; },
                    forcedExt: ext,
                    onError: _ => { ok = false; }
                );
                if (!ok) // не удалось скачать — выходим
                {
                    if (loadingPanel) loadingPanel.SetActive(false);
                    yield break;
                }
            }
            else
            {
                // оффлайн и файла нет — воспроизведение невозможно
                if (loadingPanel) loadingPanel.SetActive(false);
                yield break;
            }
        }

        // 3) Готовим и запускаем
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = string.IsNullOrEmpty(cached) ? url : AsFileUrl(cached);
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;

        if (loadingPanel) loadingPanel.SetActive(false);
        rawImage.texture = videoPlayer.texture;
        videoPlayer.Play();
    }

    // === Вспомогательное ===
    private void OnVideoPrepared(VideoPlayer vp)
    {
        if (vp.audioTrackCount > 0) vp.EnableAudioTrack(0, true);
        rawImage.texture = vp.texture;
        vp.Play();
        UpdateVideoSize();
    }

    void Update()
    {
        if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y) { lastScreenSize = new Vector2(Screen.width, Screen.height); UpdateVideoSize(); }
        if (Screen.orientation != lastOrientation) { lastOrientation = Screen.orientation; UpdateOrientationButtonIcon(); }
        HandleTouch();

        if (!isDraggingSlider && videoPlayer.length > 0 && progressSlider)
            progressSlider.value = (float)(videoPlayer.time / videoPlayer.length);

        UpdateTimeText();
    }

    private void HandleTouch()
    {
        if (Touchscreen.current == null) return;
        var touch = Touchscreen.current.primaryTouch;
        if (!touch.press.wasPressedThisFrame) return;
        if (Time.time - lastToggleTime < toggleDebounce) return;
        lastToggleTime = Time.time;

        Vector2 pos = touch.position.ReadValue();
        if ((pauseButton && RectTransformUtility.RectangleContainsScreenPoint(pauseButton.transform as RectTransform, pos, null)) ||
            (rewindButton && RectTransformUtility.RectangleContainsScreenPoint(rewindButton.transform as RectTransform, pos, null)) ||
            (forwardButton && RectTransformUtility.RectangleContainsScreenPoint(forwardButton.transform as RectTransform, pos, null)) ||
            (progressSlider && RectTransformUtility.RectangleContainsScreenPoint(progressSlider.transform as RectTransform, pos, null)) ||
            (screenOrientationButton && RectTransformUtility.RectangleContainsScreenPoint(screenOrientationButton.transform as RectTransform, pos, null)) ||
            (speedButton && RectTransformUtility.RectangleContainsScreenPoint(speedButton.transform as RectTransform, pos, null)) ||
            (replayButton && RectTransformUtility.RectangleContainsScreenPoint(replayButton.transform as RectTransform, pos, null)))
            return;

        ToggleControls();
    }

    private void ToggleControls() { if (controlsVisible) HideControls(); else ShowControls(); }
    private void ShowControls() { if (!controlsOverlayGroup) return; if (replayButton && replayButton.gameObject.activeSelf) return; ToggleUI(true); FadeOverlay(controlsOverlayGroup, 1f, 0.25f); controlsVisible = true; }
    private void HideControls() { if (!controlsOverlayGroup) return; ToggleUI(false); FadeOverlay(controlsOverlayGroup, 0f, 0.25f); controlsVisible = false; }
    private void ToggleUI(bool on)
    {
        if (pauseButton) pauseButton.gameObject.SetActive(on);
        if (rewindButton) rewindButton.gameObject.SetActive(on);
        if (forwardButton) forwardButton.gameObject.SetActive(on);
        if (screenOrientationButton) screenOrientationButton.gameObject.SetActive(on);
        if (speedButton) speedButton.gameObject.SetActive(on);
        if (progressSlider) progressSlider.gameObject.SetActive(on);
        if (timeText) timeText.gameObject.SetActive(on);
    }

    private void TogglePause()
    {
        if (!videoPlayer.isPrepared) return;
        if (videoPlayer.isPlaying) { videoPlayer.Pause(); if (pauseButtonImage) pauseButtonImage.sprite = playIcon; }
        else { videoPlayer.Play(); if (pauseButtonImage) pauseButtonImage.sprite = pauseIcon; }
    }

    private void Skip(double seconds)
    {
        if (!videoPlayer.isPrepared || videoPlayer.length <= 0) return;
        bool wasPlaying = videoPlayer.isPlaying;
        double target = Mathf.Clamp((float)(videoPlayer.time + seconds), 0f, (float)videoPlayer.length);
        videoPlayer.time = target;
        if (wasPlaying) videoPlayer.Play(); else videoPlayer.Pause();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        HideControls();
        if (replayButton)
        {
            replayButton.gameObject.SetActive(true);
            FadeOverlay(controlsOverlayGroup, 1f, 0.25f);
        }
    }

    private void OnReplayButtonClicked()
    {
        if (replayButton) replayButton.gameObject.SetActive(false);
        FadeOverlay(controlsOverlayGroup, 0f, 0.25f);
        videoPlayer.time = 0; videoPlayer.Play();
    }

    private void ToggleOrientation()
    {
        if (Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown)
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        else
            Screen.orientation = ScreenOrientation.Portrait;
    }

    private void UpdateOrientationButtonIcon()
    {
        if (!screenOrientationButtonImage) return;
        if (Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown)
            screenOrientationButtonImage.sprite = enterLandscapeIcon;
        else
            screenOrientationButtonImage.sprite = enterPortraitIcon;
    }

    private void ToggleSpeed()
    {
        speedIndex = (speedIndex + 1) % speedOptions.Length;
        float newSpeed = speedOptions[speedIndex];
        videoPlayer.playbackSpeed = newSpeed;
        if (speedButtonText) speedButtonText.text = $"{newSpeed}Х";
    }

    private void OnSliderChanged(float value)
    {
        if (!isDraggingSlider || videoPlayer.length <= 0) return;
        videoPlayer.time = value * videoPlayer.length;
    }

    public void OnBeginDrag() => isDraggingSlider = true;
    public void OnEndDrag() => isDraggingSlider = false;

    private void UpdateVideoSize()
    {
        if (!videoPlayer.isPrepared || videoPlayer.texture == null || !fitter) return;
        float vw = videoPlayer.texture.width; float vh = videoPlayer.texture.height; float aspect = vw / vh;
        if (aspect < 1f) { fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight; fitter.aspectRatio = aspect; }
        else { fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fitter.aspectRatio = aspect; }
        rawImage.texture = videoPlayer.texture;
    }

    private void UpdateTimeText()
    {
        if (!timeText) return;
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
    { if (!g) return; if (overlayFadeCoroutine != null) StopCoroutine(overlayFadeCoroutine); overlayFadeCoroutine = StartCoroutine(FadeCanvasGroup(g, target, dur)); }

    // Helpers
    private static bool IsOnline() => Application.internetReachability != NetworkReachability.NotReachable;
    private static string GuessExt(string url, string def = ".mp4")
    {
        try { var pure = url.Split('?')[0]; var ext = Path.GetExtension(pure); return string.IsNullOrEmpty(ext) ? def : ext; }
        catch { return def; }
    }
    private static string AsFileUrl(string localPath) =>
        string.IsNullOrEmpty(localPath) ? localPath : (localPath.StartsWith("file://") ? localPath : "file://" + localPath);
}
