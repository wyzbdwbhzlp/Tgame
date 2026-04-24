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

    // 【🔥核心修改】直接接收 Sprite 对象
    public void Init(int unitID, string characterName, bool isPlayer, Sprite portrait)
    {
        _boundUnitID = unitID;

        if (txtName) txtName.text = characterName;
        if (fillImage != null) fillImage.color = isPlayer ? Color.cyan : new Color(1f, 0.3f, 0.3f);

        // 【🔥核心修改】直接赋值，不用再去 Resources 里面找了
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
            tuSlider.value = remainRatio;
        }
        if (txtName) txtName.color = isSelected ? Color.yellow : Color.white;
    }

    public int GetBoundUnitID() => _boundUnitID;
}