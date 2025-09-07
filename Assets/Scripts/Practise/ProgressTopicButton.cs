using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ProgressTopicButton : MonoBehaviour
{
    [Header("UI Refs")]
    public Button button;
    public Image fill;         // Type=Filled, Method=Horizontal, Origin=Left
    public TMP_Text titleText;
    public TMP_Text pointsText; // число очков справа

    private string _subjectId;
    private string _topicId;
    private UnityAction _onClick;

    public void Setup(string subjectId, string topicId, string title, UnityAction onClick)
    {
        _subjectId = (subjectId ?? "").Trim();
        _topicId = (topicId ?? "").Trim();
        _onClick = onClick;

        if (titleText) titleText.text = title;

        if (button)
        {
            button.onClick.RemoveAllListeners();
            if (_onClick != null) button.onClick.AddListener(_onClick);
        }

        RefreshFromLocal();
    }

    public void Setup(string subjectId, string topicId, UnityAction onClick)
        => Setup(subjectId, topicId, "", onClick);

    public void RefreshFromLocal()
    {
        int pts = PointsService.GetPoints(_subjectId, _topicId);
        SetPointsImmediate(pts);
    }

    public void SetPointsImmediate(int pts)
    {
        pts = Mathf.Clamp(pts, 0, 100);
        SetFill01(pts / 100f);
        if (pointsText) pointsText.text = pts.ToString();
    }

    public void SetFill01(float v01)
    {
        v01 = Mathf.Clamp01(v01);
        if (!fill) return;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = v01;
    }
}
