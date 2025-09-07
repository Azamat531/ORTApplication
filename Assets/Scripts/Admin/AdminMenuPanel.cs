using UnityEngine;
using UnityEngine.UI;
using TMPro;

// AdminMenuPanel v2.1: меню с секциями (CreateUser / Users / Settings)
// Теперь НЕ открывает автоматически секцию при старте — все панели скрыты до клика.
public class AdminMenuPanel : MonoBehaviour
{
    [Header("Header/Status (optional)")]
    public TextMeshProUGUI titleText;      // заголовок секции
    public TextMeshProUGUI hintText;       // подсказки/ошибки

    [Header("Nav Buttons")]
    public Button backButton;              // закрыть меню
    public Button openCreateUserButton;    // открыть форму создания
    public Button openUsersButton;         // открыть список пользователей (опц.)
    public Button openSettingsButton;      // открыть настройки (опц.)

    [Header("Sections (Panels)")]
    public GameObject createUserPanelRoot; // панель с AdminPanelController
    public GameObject usersPanelRoot;      // панель со списком пользователей
    public GameObject settingsPanelRoot;   // панель с настройками

    void Awake()
    {
        if (backButton) backButton.onClick.AddListener(Close);
        if (openCreateUserButton) openCreateUserButton.onClick.AddListener(() => ShowSection(createUserPanelRoot, "Создать пользователя"));
        if (openUsersButton) openUsersButton.onClick.AddListener(() => ShowSection(usersPanelRoot, "Пользователи"));
        if (openSettingsButton) openSettingsButton.onClick.AddListener(() => ShowSection(settingsPanelRoot, "Настройки"));

        // При старте скрываем все секции
        HideAllSections();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        HideAllSections(); // меню открывается пустым, пока не выберут секцию
    }

    public void Close() => gameObject.SetActive(false);

    private void ShowSection(GameObject target, string title)
    {
        HideAllSections();
        if (target) target.SetActive(true);
        if (titleText) titleText.text = title;
        if (hintText) hintText.text = string.Empty;
    }

    private void HideAllSections()
    {
        if (createUserPanelRoot) createUserPanelRoot.SetActive(false);
        if (usersPanelRoot) usersPanelRoot.SetActive(false);
        if (settingsPanelRoot) settingsPanelRoot.SetActive(false);
        if (titleText) titleText.text = string.Empty;
        if (hintText) hintText.text = string.Empty;
    }
}
