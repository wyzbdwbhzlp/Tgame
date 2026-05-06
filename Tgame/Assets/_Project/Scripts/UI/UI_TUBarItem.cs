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
            float remainRatio = 1f - Mathf.Clamp01((float)plannedTU / maxTU);

            // 【🔥核心修复】防止 Slider 值完全归零导致 Fill 宽度为 0，从而引发 Unity 底层射线检测的 NaN 报错
            tuSlider.value = Mathf.Max(0.001f, remainRatio);
        }

        if (txtName) txtName.color = isSelected ? Color.yellow : Color.white;
    }

    public int GetBoundUnitID() => _boundUnitID;
}