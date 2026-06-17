using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using TGame.Battle;

public class UI_TUBarItem : MonoBehaviour
{
    [Header("美术组件引用")]
    public TextMeshProUGUI txtName;
    public Image imgPortrait;

    [Header("时间轴容器")]
    [Tooltip("此底框需要挂载 Horizontal Layout Group 组件")]
    public RectTransform barBackground;

    private int _boundUnitID;
    private float _maxBarWidth;

    private List<GameObject> _segmentPool = new List<GameObject>();

    public void Init(int unitID, string characterName, bool isPlayer, Sprite portrait)
    {
        _boundUnitID = unitID;
        if (txtName) txtName.text = characterName;

        if (imgPortrait != null)
        {
            if (portrait != null)
            {
                imgPortrait.sprite = portrait;
                imgPortrait.gameObject.SetActive(true);
            }
            else imgPortrait.gameObject.SetActive(false);
        }

        // 初始化时获取背景条的物理宽度
        if (barBackground)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(barBackground);
            _maxBarWidth = barBackground.rect.width;
        }
    }

    public void UpdateTimeline(List<TurnManager.ScheduledAction> actions, int maxTU, bool isSelected)
    {
        if (txtName) txtName.color = isSelected ? Color.yellow : Color.white;
        if (barBackground == null || _maxBarWidth <= 0) return;

        // 隐藏所有旧的色块
        foreach (var seg in _segmentPool) seg.SetActive(false);

        int totalCost = 0;

        for (int i = 0; i < actions.Count; i++)
        {
            int cost = actions[i].cost;
            string aName = actions[i].name;
            totalCost += cost;

            // 越界保护
            if (totalCost > maxTU) cost = cost - (totalCost - maxTU);
            if (cost <= 0) break;

            // 【🔥修改】如果是 LayoutGroup 控制，如果设置了 Spacing (间距)，
            // 严谨的做法是需要减去间距的损耗，但这里为了简单直观，我们依然按比例分配基础宽度
            float ratio = (float)cost / maxTU;
            float segmentWidth = ratio * _maxBarWidth;

            GameObject segmentObj = GetOrCreateSegment(i);
            segmentObj.SetActive(true);
            segmentObj.transform.SetAsLastSibling(); // 确保它在 LayoutGroup 中的顺序排在最后

            // 【🔥核心】现在我们只设置宽度，X坐标和高度完全交给 Horizontal Layout Group 管理！
            RectTransform rt = segmentObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(segmentWidth, rt.sizeDelta.y);

            Image img = segmentObj.GetComponent<Image>();
            img.color = GetColorForAction(aName);

            TextMeshProUGUI txt = segmentObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                if (segmentWidth > 45f) txt.text = $"{aName}\n-{cost}";
                else txt.text = $"-{cost}";
            }
        }
    }

    private Color GetColorForAction(string actionName)
    {
        switch (actionName)
        {
            case "移动": return new Color(0.2f, 0.8f, 0.4f, 0.9f); // 绿色
            case "攻击": return new Color(0.9f, 0.3f, 0.2f, 0.9f); // 红色
            case "技能": return new Color(0.7f, 0.2f, 0.9f, 0.9f); // 紫色
            case "道具": return new Color(0.2f, 0.6f, 1.0f, 0.9f); // 蓝色
            default: return new Color(0.5f, 0.5f, 0.5f, 0.9f);     // 灰色
        }
    }

    private GameObject GetOrCreateSegment(int index)
    {
        if (index < _segmentPool.Count) return _segmentPool[index];

        GameObject segObj = new GameObject($"Segment_{index}");
        segObj.transform.SetParent(barBackground, false);

        RectTransform rt = segObj.AddComponent<RectTransform>();
        segObj.AddComponent<Image>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(rt, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();

        // 文字铺满整个色块
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontSize = 12;
        txt.color = Color.white;
        txt.fontStyle = FontStyles.Bold;
        txt.enableWordWrapping = false;
        txt.overflowMode = TextOverflowModes.Truncate;

        _segmentPool.Add(segObj);
        return segObj;
    }

    public int GetBoundUnitID() => _boundUnitID;
}