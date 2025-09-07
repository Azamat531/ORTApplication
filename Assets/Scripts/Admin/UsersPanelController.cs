using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ������ ������������� (+ ���. ���� �������)
public class UsersPanelController : MonoBehaviour
{
    [Header("UI Refs")]
    public Button refreshButton;
    public Button loadMoreButton;           // ����������� (���������)
    public TextMeshProUGUI statusText;      // ������ �������
    public ScrollRect scroll;               // ��� ScrollView
    public Transform listContent;           // ������ scroll.content
    public GameObject userRowPrefab;        // ������ � UserRowView

    [Header("Navigation")]
    public Button backButton;               // ������ "�����"
    public GameObject adminMenuRoot;        // ������ AdminMenuPanel (�������� ��� ������)

    [Header("Query")]
    [Range(5, 200)] public int pageSize = 20; // ������������� �� ��������

    private FirebaseFirestore _db;
    private DocumentSnapshot _lastDoc;      // ������ ���������
    private bool _busy;

    void Awake()
    {
        _db = FirebaseFirestore.DefaultInstance;
        if (refreshButton) refreshButton.onClick.AddListener(() => _ = Refresh());
        if (loadMoreButton) loadMoreButton.onClick.AddListener(() => _ = LoadMore());
        if (backButton) backButton.onClick.AddListener(Close);
    }

    void OnEnable()
    {
        // ����-�������� ��� ��������
        _ = Refresh();
    }

    public async Task Refresh()
    {
        if (_busy) return; _busy = true; SetStatus("��������...");
        _lastDoc = null; ClearList();
        try
        {
            var q = _db.Collection("users").OrderByDescending("createdAt").Limit(pageSize);
            var snap = await q.GetSnapshotAsync();

            Render(snap.Documents);
            _lastDoc = snap.Documents.LastOrDefault();
            UpdateLoadMoreVisibility(snap.Count == pageSize);
            SetStatus($"������������: {snap.Count}");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            SetStatus("������ �������� ������");
            UpdateLoadMoreVisibility(false);
        }
        finally { _busy = false; }
    }

    public async Task LoadMore()
    {
        if (_busy || _lastDoc == null) return; _busy = true; SetStatus("��������...");
        try
        {
            var q = _db.Collection("users")
                       .OrderByDescending("createdAt")
                       .StartAfter(_lastDoc)
                       .Limit(pageSize);

            var snap = await q.GetSnapshotAsync();
            RenderAppend(snap.Documents);
            _lastDoc = snap.Documents.LastOrDefault();
            UpdateLoadMoreVisibility(snap.Count == pageSize);
            SetStatus("������");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            SetStatus("������ ��� ����������");
            UpdateLoadMoreVisibility(false);
        }
        finally { _busy = false; }
    }

    private void Render(IEnumerable<DocumentSnapshot> docs)
    {
        foreach (var d in docs) CreateRow(d);
        if (scroll) scroll.verticalNormalizedPosition = 1f; // ������
    }

    private void RenderAppend(IEnumerable<DocumentSnapshot> docs)
    {
        foreach (var d in docs) CreateRow(d);
    }

    private void CreateRow(DocumentSnapshot d)
    {
        var go = Instantiate(userRowPrefab, listContent, false);
        var view = go.GetComponent<UserRowView>();
        if (!view) return;

        // ������� ����
        string uid = d.Id;
        string username = d.TryGetValue("username", out string u) ? u : "(no username)";
        string realName = d.TryGetValue("realName", out string rn) ? rn : "";
        string role = d.TryGetValue("role", out string r) ? r : "student";

        // createdAt ? dd.MM.yyyy HH:mm
        string created = "�";
        if (d.ContainsField("createdAt"))
        {
            try
            {
                var ts = d.GetValue<Timestamp>("createdAt");
                created = ts.ToDateTime().ToLocalTime().ToString("dd.MM.yyyy HH:mm");
            }
            catch { /* ignore */ }
        }

        // ����� ���������� ����
        string region = d.TryGetValue("region", out string reg) ? reg : "";
        string district = d.TryGetValue("district", out string dist) ? dist : "";
        string school = d.TryGetValue("school", out string sch) ? sch : "";
        string phone = d.TryGetValue("phone", out string ph) ? ph : "";
        string whatsapp = d.TryGetValue("whatsapp", out string wa) ? wa : "";

        // ����������� ����
        string localRole = ToLocalRole(role);

        // ������ �� � ������ ������
        view.Bind(username, realName, localRole, created, uid, region, district, school, phone, whatsapp);
    }

    private string ToLocalRole(string role)
    {
        switch ((role ?? "").ToLowerInvariant())
        {
            case "admin": return "�����";
            case "teacher": return "�������";
            case "student": return "������";
            default: return role ?? "";
        }
    }

    private void ClearList()
    {
        if (!listContent) return;
        for (int i = listContent.childCount - 1; i >= 0; i--)
            Destroy(listContent.GetChild(i).gameObject);
    }

    private void SetStatus(string s)
    {
        if (statusText) statusText.text = s ?? string.Empty;
    }

    private void UpdateLoadMoreVisibility(bool visible)
    {
        if (loadMoreButton) loadMoreButton.gameObject.SetActive(visible);
    }

    private void Close()
    {
        gameObject.SetActive(false);
        if (adminMenuRoot) adminMenuRoot.SetActive(true);
    }
}
