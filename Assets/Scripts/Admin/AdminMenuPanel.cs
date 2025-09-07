using UnityEngine;
using UnityEngine.UI;
using TMPro;

// AdminMenuPanel v2.1: ���� � �������� (CreateUser / Users / Settings)
// ������ �� ��������� ������������� ������ ��� ������ � ��� ������ ������ �� �����.
public class AdminMenuPanel : MonoBehaviour
{
    [Header("Header/Status (optional)")]
    public TextMeshProUGUI titleText;      // ��������� ������
    public TextMeshProUGUI hintText;       // ���������/������

    [Header("Nav Buttons")]
    public Button backButton;              // ������� ����
    public Button openCreateUserButton;    // ������� ����� ��������
    public Button openUsersButton;         // ������� ������ ������������� (���.)
    public Button openSettingsButton;      // ������� ��������� (���.)

    [Header("Sections (Panels)")]
    public GameObject createUserPanelRoot; // ������ � AdminPanelController
    public GameObject usersPanelRoot;      // ������ �� ������� �������������
    public GameObject settingsPanelRoot;   // ������ � �����������

    void Awake()
    {
        if (backButton) backButton.onClick.AddListener(Close);
        if (openCreateUserButton) openCreateUserButton.onClick.AddListener(() => ShowSection(createUserPanelRoot, "������� ������������"));
        if (openUsersButton) openUsersButton.onClick.AddListener(() => ShowSection(usersPanelRoot, "������������"));
        if (openSettingsButton) openSettingsButton.onClick.AddListener(() => ShowSection(settingsPanelRoot, "���������"));

        // ��� ������ �������� ��� ������
        HideAllSections();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        HideAllSections(); // ���� ����������� ������, ���� �� ������� ������
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
