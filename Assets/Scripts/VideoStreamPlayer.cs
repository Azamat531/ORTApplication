// Assets/Scripts/VideoStreamPlayer.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

[RequireComponent(typeof(RawImage), typeof(VideoPlayer))]
public class VideoStreamPlayer : MonoBehaviour
{
    [Header("Видео")]
    [Tooltip("URL ролика")]
    public string videoURL = "https://www.w3schools.com/html/mov_bbb.mp4";

    // Компоненты
    private RawImage rawImage;
    private RectTransform rectTransform;
    private VideoPlayer videoPlayer;
    private AspectRatioFitter fitter;
    private Vector2 lastScreenSize;

    [Header("Иконки паузы/игры")]
    public Sprite playIcon;
    public Sprite pauseIcon;
    private Image pauseButtonImage;

    [Header("UI-контролы")]
    public Button pauseButton;
    public Button rewindButton;
    public Button forwardButton;
    public Button screenOrientationButton;
    public Sprite enterLandscapeIcon;
    public Sprite enterPortraitIcon;
    private Image screenOrientationButtonImage;

    [Header("Скорость")]
    public Button speedButton;
    public TextMeshProUGUI speedButtonText;
    private readonly float[] speedOptions = { 1f, 1.25f, 1.5f, 2f, 0.5f };
    private int speedIndex = 0;

    [Header("Прогресс и время")]
    public Slider progressSlider;
    public TextMeshProUGUI timeText;
    private bool isDraggingSlider;

    [Header("Затемнение контролов")]
    public CanvasGroup controlsOverlayGroup;
    private bool controlsVisible;
    private float lastToggleTime;
    private const float toggleDebounce = 0.3f;
    private Coroutine overlayFadeCoroutine;

    [Header("Replay")]
    [Tooltip("Кнопка «Повторить»")]
    public Button replayButton;

    // Для отслеживания смены ориентации
    private ScreenOrientation lastOrientation;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        rectTransform = rawImage.rectTransform;

        fitter = rawImage.GetComponent<AspectRatioFitter>()
                 ?? rawImage.gameObject.AddComponent<AspectRatioFitter>();

        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = videoURL;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        var audio = gameObject.AddComponent<AudioSource>();
        videoPlayer.SetTargetAudioSource(0, audio);
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.playbackSpeed = speedOptions[speedIndex];
    }

    void Start()
    {
        lastScreenSize = new Vector2(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;

        // Pause
        pauseButtonImage = pauseButton.GetComponent<Image>();
        pauseButton.onClick.AddListener(TogglePause);
        pauseButtonImage.sprite = pauseIcon;

        // Rewind / Forward
        rewindButton.onClick.AddListener(() => Skip(-10));
        forwardButton.onClick.AddListener(() => Skip(10));

        // Orientation toggle
        screenOrientationButtonImage = screenOrientationButton.GetComponent<Image>();
        screenOrientationButton.onClick.AddListener(ToggleOrientation);
        UpdateOrientationButtonIcon();

        // Speed
        speedButton.onClick.AddListener(ToggleSpeed);
        speedButtonText.text = $"{speedOptions[speedIndex]}Х";

        // Progress
        progressSlider.onValueChanged.AddListener(OnSliderChanged);

        // Replay
        replayButton.onClick.AddListener(OnReplayButtonClicked);
        replayButton.gameObject.SetActive(false);

        // Initially hide all controls
        HideControls();
        controlsOverlayGroup.alpha = 0f;
        controlsOverlayGroup.blocksRaycasts = false;

        videoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        rawImage.texture = vp.texture;
        vp.Play();
        UpdateVideoSize();
    }

    void Update()
    {
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

        HandleTouch();

        if (!isDraggingSlider && videoPlayer.length > 0)
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
        if (RectTransformUtility.RectangleContainsScreenPoint(pauseButton.transform as RectTransform, pos, null) ||
            RectTransformUtility.RectangleContainsScreenPoint(rewindButton.transform as RectTransform, pos, null) ||
            RectTransformUtility.RectangleContainsScreenPoint(forwardButton.transform as RectTransform, pos, null) ||
            RectTransformUtility.RectangleContainsScreenPoint(progressSlider.transform as RectTransform, pos, null) ||
            RectTransformUtility.RectangleContainsScreenPoint(screenOrientationButton.transform as RectTransform, pos, null) ||
            RectTransformUtility.RectangleContainsScreenPoint(speedButton.transform as RectTransform, pos, null) ||
            RectTransformUtility.RectangleContainsScreenPoint(replayButton.transform as RectTransform, pos, null))
            return;

        ToggleControls();
    }

    private void ToggleControls()
    {
        if (controlsVisible) HideControls(); else ShowControls();
    }

    private void ShowControls()
    {
        if (replayButton.gameObject.activeSelf) return;

        pauseButton.gameObject.SetActive(true);
        rewindButton.gameObject.SetActive(true);
        forwardButton.gameObject.SetActive(true);
        screenOrientationButton.gameObject.SetActive(true);
        speedButton.gameObject.SetActive(true);
        progressSlider.gameObject.SetActive(true);
        timeText.gameObject.SetActive(true);
        FadeOverlay(controlsOverlayGroup, 1f, 0.25f);
        controlsVisible = true;
    }

    private void HideControls()
    {
        pauseButton.gameObject.SetActive(false);
        rewindButton.gameObject.SetActive(false);
        forwardButton.gameObject.SetActive(false);
        screenOrientationButton.gameObject.SetActive(false);
        speedButton.gameObject.SetActive(false);
        progressSlider.gameObject.SetActive(false);
        timeText.gameObject.SetActive(false);
        FadeOverlay(controlsOverlayGroup, 0f, 0.25f);
        controlsVisible = false;
    }

    private void TogglePause()
    {
        if (!videoPlayer.isPrepared) return;
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            pauseButtonImage.sprite = playIcon;
        }
        else
        {
            videoPlayer.Play();
            pauseButtonImage.sprite = pauseIcon;
        }
    }

    private void Skip(double seconds)
    {
        if (!videoPlayer.isPrepared || videoPlayer.length <= 0) return;
        bool wasPlaying = videoPlayer.isPlaying;
        double current = videoPlayer.time;
        double target = Mathf.Clamp((float)(current + seconds), 0f, (float)videoPlayer.length);
        videoPlayer.time = target;
        if (wasPlaying) videoPlayer.Play(); else videoPlayer.Pause();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        HideControls();
        replayButton.gameObject.SetActive(true);
        FadeOverlay(controlsOverlayGroup, 1f, 0.25f);
    }

    private void OnReplayButtonClicked()
    {
        replayButton.gameObject.SetActive(false);
        FadeOverlay(controlsOverlayGroup, 0f, 0.25f);

        videoPlayer.time = 0;
        videoPlayer.Play();
    }

    private void ToggleOrientation()
    {
        if (Screen.orientation == ScreenOrientation.Portrait ||
            Screen.orientation == ScreenOrientation.PortraitUpsideDown)
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        else
            Screen.orientation = ScreenOrientation.Portrait;
    }

    private void UpdateOrientationButtonIcon()
    {
        if (Screen.orientation == ScreenOrientation.Portrait ||
            Screen.orientation == ScreenOrientation.PortraitUpsideDown)
            screenOrientationButtonImage.sprite = enterLandscapeIcon;
        else
            screenOrientationButtonImage.sprite = enterPortraitIcon;
    }

    private void ToggleSpeed()
    {
        speedIndex = (speedIndex + 1) % speedOptions.Length;
        float newSpeed = speedOptions[speedIndex];
        videoPlayer.playbackSpeed = newSpeed;
        speedButtonText.text = $"{newSpeed}Х";
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
        if (!videoPlayer.isPrepared || videoPlayer.texture == null) return;
        float vw = videoPlayer.texture.width;
        float vh = videoPlayer.texture.height;
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
    }

    private void UpdateTimeText()
    {
        if (videoPlayer.length > 0)
        {
            int m1 = (int)(videoPlayer.time / 60), s1 = (int)(videoPlayer.time % 60);
            int m2 = (int)(videoPlayer.length / 60), s2 = (int)(videoPlayer.length % 60);
            timeText.text = $"{m1}:{s1:D2} / {m2}:{s2:D2}";
        }
        else
            timeText.text = "0:00 / 0:00";
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float target, float duration)
    {
        float start = group.alpha, t = 0f;
        group.blocksRaycasts = target > 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        group.alpha = target;
    }

    private void FadeOverlay(CanvasGroup group, float target, float duration)
    {
        if (overlayFadeCoroutine != null)
            StopCoroutine(overlayFadeCoroutine);
        overlayFadeCoroutine = StartCoroutine(FadeCanvasGroup(group, target, duration));
    }

    /// <summary>
    /// Задаёт новый URL и запускает подготовку/проигрывание.
    /// </summary>
    //public void SetVideoURL(string url)
    //{
    //    videoURL = url;
    //    videoPlayer.url = url;
    //    videoPlayer.Prepare();
    //}
    public void SetVideoURL(string url)
    {
        // Если по какой-то причине videoPlayer не был инициализирован в Awake
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                Debug.LogError("[VideoStreamPlayer] Не найден компонент VideoPlayer на том же объекте!");
                return;
            }
            // Восстановим подписки, если они ещё не добавлены
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.loopPointReached += OnVideoFinished;
        }

        videoURL = url;
        videoPlayer.url = url;
        videoPlayer.Prepare(); // после подготовки Play() вызовется в OnVideoPrepared
    }

}
