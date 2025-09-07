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
        public Button button;       // ���� ������
        public Image icon;          // ������ ������ ������
        public Sprite activeSprite; // ���������
        public Sprite inactiveSprite; // �������
        public GameObject linkedPanel; // ������, ������� ���������
    }

    [Header("Bottom Nav Buttons")]
    public NavButtonConfig[] navButtons;

    void Start()
    {
        // === Auth ������ ===
        if (loginButton) loginButton.onClick.AddListener(OpenLoginPanel);
        if (registerButton) registerButton.onClick.AddListener(OpenRegisterPanel);

        // === ��������� ������� ������� ===
        foreach (var cfg in navButtons)
        {
            if (cfg.button != null && cfg.linkedPanel != null)
            {
                cfg.button.onClick.AddListener(() => ShowPanel(cfg.linkedPanel));
            }
        }

        // ��� ������ ���������� LoginPanel
        OpenLoginPanel();
    }

    // ===== AUTH ������������ =====
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

        ShowPanel(homePanel); // �������� � Home
    }

    // ===== MAIN ������������ =====
    private void ShowPanel(GameObject panelToShow)
    {
        HideMainPanels();

        if (panelToShow) panelToShow.SetActive(true);

        // �������� auth-������
        if (loginPanel) loginPanel.SetActive(false);
        if (registerPanel) registerPanel.SetActive(false);

        // ��������� ������� ���������
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
