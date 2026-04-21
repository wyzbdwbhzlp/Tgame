using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_TUBarItem : MonoBehaviour
{
    [Header("美术组件引用")]
    public TextMeshProUGUI txtName;

    [Tooltip("使用 Slider 替代 Scrollbar")]
    public Slider tuSlider;

    [Tooltip("Slider 下方的 Fill 物体上挂载的 Image (用于改变颜色)")]
    public Image fillImage;

    // 缓存绑定的角色 ID
    private int _boundUnitID;

    /// <summary>
    /// 初始化：绑定角色并设置初始表现
    /// </summary>
    public void Init(int unitID, string characterName, bool isPlayer)
    {
        _boundUnitID = unitID;

        if (txtName)
        {
            txtName.text = characterName;
        }

        // 修改 Slider 填充区域的颜色：玩家青色，敌人红色
        if (fillImage != null)
        {
            fillImage.color = isPlayer ? Color.cyan : new Color(1f, 0.3f, 0.3f);
        }
    }

    /// <summary>
    /// 刷新：根据当前时素和选中状态更新表现
    /// </summary>
    public void UpdateState(int plannedTU, int maxTU, bool isSelected)
    {
        if (tuSlider)
        {
            // 计算剩余比例。注意：请确保在 Prefab 中把 Slider 的 Min Value 设为 0，Max Value 设为 1
            float remainRatio = 1f - Mathf.Clamp01((float)plannedTU / maxTU);
            tuSlider.value = remainRatio;
        }

        if (txtName)
        {
            // 选中时名字高亮
            txtName.color = isSelected ? Color.yellow : Color.white;
        }
    }

    public int GetBoundUnitID()
    {
        return _boundUnitID;
    }
}