using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // ===== AUTH =====
    [Header("Auth Buttons")]
    public Button loginButton;
    public Button registerButton;

    [Header("Auth Panels")]
    public GameObject loginPanel;
    public GameObject registerPanel;

    // ===== MAIN PANELS =====
    [Header("Main Panels (PanelsContainer)")]
    public GameObject homePanel;
    public GameObject practisePanel;
    public GameObject ratingPanel;
    public GameObject profilePanel;

    [System.Serializable]
    public class NavButtonConfig
    {
        public Button button;       // сама кнопка
        public Image icon;          // иконка внутри кнопки
        public Sprite activeSprite; // выбранная
        public Sprite inactiveSprite; // обычная
        public GameObject linkedPanel; // панель, которую открывает
    }

    [Header("Bottom Nav Buttons")]
    public NavButtonConfig[] navButtons;

    void Start()
    {
        // === Auth кнопки ===
        if (loginButton) loginButton.onClick.AddListener(OpenLoginPanel);
        if (registerButton) registerButton.onClick.AddListener(OpenRegisterPanel);

        // === Навигация главных панелей ===
        foreach (var cfg in navButtons)
        {
            if (cfg.button != null && cfg.linkedPanel != null)
            {
                cfg.button.onClick.AddListener(() => ShowPanel(cfg.linkedPanel));
            }
        }

        // при старте показываем LoginPanel
        OpenLoginPanel();
    }

    // ===== AUTH переключение =====
    public void OpenLoginPanel()
    {
        if (loginPanel) loginPanel.SetActive(true);
        if (registerPanel) registerPanel.SetActive(false);

        HideMainPanels();
    }

    public void OpenRegisterPanel()
    {
        if (loginPanel) loginPanel.SetActive(false);
        if (registerPanel) registerPanel.SetActive(true);

        HideMainPanels();
    }

    public void OpenMainApp()
    {
        if (loginPanel) loginPanel.SetActive(false);
        if (registerPanel) registerPanel.SetActive(false);

        ShowPanel(homePanel); // начинаем с Home
    }

    // ===== MAIN переключение =====
    private void ShowPanel(GameObject panelToShow)
    {
        HideMainPanels();

        if (panelToShow) panelToShow.SetActive(true);

        // скрываем auth-панели
        if (loginPanel) loginPanel.SetActive(false);
        if (registerPanel) registerPanel.SetActive(false);

        // обновляем спрайты навигации
        foreach (var cfg in navButtons)
        {
            if (cfg.icon != null)
            {
                if (cfg.linkedPanel == panelToShow)
                    cfg.icon.sprite = cfg.activeSprite;
                else
                    cfg.icon.sprite = cfg.inactiveSprite;
            }
        }
    }

    private void HideMainPanels()
    {
        if (homePanel) homePanel.SetActive(false);
        if (practisePanel) practisePanel.SetActive(false);
        if (ratingPanel) ratingPanel.SetActive(false);
        if (profilePanel) profilePanel.SetActive(false);
    }
}
