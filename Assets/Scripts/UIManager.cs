using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panels (PanelsContainer)")]
    public GameObject homePanel;
    public GameObject coursesPanel;
    public GameObject practisePanel;
    public GameObject ratingPanel;

    [Header("Bottom Nav Buttons")]
    public Button homeButton;
    public Button coursesButton;
    public Button practiseButton;
    public Button ratingButton;

    void Start()
    {
        // ������ �����
        homeButton.onClick.AddListener(() => ShowPanel(homePanel));
        coursesButton.onClick.AddListener(() => ShowPanel(coursesPanel));
        practiseButton.onClick.AddListener(() => ShowPanel(practisePanel));
        ratingButton.onClick.AddListener(() => ShowPanel(ratingPanel));
    }

    /// <summary>
    /// �������� ��������� ������ � ��������� ��� ���������.
    /// </summary>
    void ShowPanel(GameObject panelToShow)
    {
        // ����� ��������� ���
        homePanel.SetActive(false);
        coursesPanel.SetActive(false);
        practisePanel.SetActive(false);
        ratingPanel.SetActive(false);

        // ���������� ������
        panelToShow.SetActive(true);
    }
}
