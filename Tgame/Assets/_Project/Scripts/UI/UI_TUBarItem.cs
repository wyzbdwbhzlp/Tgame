using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_TUBarItem : MonoBehaviour
{
    [Header("美术组件引用")]
    public TextMeshProUGUI txtName;
    public Slider tuSlider;
    public Image fillImage;
    public Image imgPortrait;

    private int _boundUnitID;

    public void Init(int unitID, string characterName, bool isPlayer, Sprite portrait)
    {
        _boundUnitID = unitID;

        if (txtName) txtName.text = characterName;
        // 玩家颜色为青色，敌人颜色为红色
        if (fillImage != null) fillImage.color = isPlayer ? Color.cyan : new Color(1f, 0.3f, 0.3f);

        if (imgPortrait != null)
        {
            if (portrait != null)
            {
                imgPortrait.sprite = portrait;
                imgPortrait.gameObject.SetActive(true);
            }
            else
            {
                imgPortrait.gameObject.SetActive(false);
            }
        }
    }

    public void UpdateState(int plannedTU, int maxTU, bool isSelected)
    {
        if (tuSlider)
        {
            // ==========================================
            // 【🔥核心修改】时间轴机制 (Timeline)
            // 一开始是 0，消耗时间会让进度条“涨”起来
            // ==========================================
            float timeRatio = Mathf.Clamp01((float)plannedTU / maxTU);

            // 依然保留 0.001f 的保底，防止 Slider 的 Fill 完全归零导致 Unity 底层射线检测报错
            tuSlider.value = Mathf.Max(0.001f, timeRatio);
        }

        if (txtName) txtName.color = isSelected ? Color.yellow : Color.white;
    }

    public int GetBoundUnitID() => _boundUnitID;
}