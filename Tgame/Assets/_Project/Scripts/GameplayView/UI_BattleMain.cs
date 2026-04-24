using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TGame.Battle;
using Game.UI;
using System.Collections.Generic;

public class UI_BattleMain : UIBase
{
    [Header("文字与按钮引用")]
    public TextMeshProUGUI txtRound;
    public TextMeshProUGUI txtState;
    public Button btnEndTurn;
    public Button btnSkill;
    public Button btnItem;

    [Header("时素列表资产化引用")]
    public RectTransform tuListContainer;
    public GameObject tuBarPrefab;

    private List<UI_TUBarItem> _tuBarItems = new List<UI_TUBarItem>();

    public override void OnInit()
    {
        base.OnInit();
        this.uiLayer = UILayer.Normal;

        if (btnEndTurn) btnEndTurn.onClick.AddListener(() => TurnManager.Instance.EndPlayerTurn());
        if (btnSkill) btnSkill.onClick.AddListener(() => RequestMockAction("Skill", 3));
        if (btnItem) btnItem.onClick.AddListener(() => RequestMockAction("Item", 2));
    }

    protected override void OnShow()
    {
        base.OnShow();
        SpawnTUBars();
    }

    private void SpawnTUBars()
    {
        if (tuListContainer == null || tuBarPrefab == null || UnitManager.Instance == null) return;

        foreach (Transform child in tuListContainer) Destroy(child.gameObject);
        _tuBarItems.Clear();

        foreach (var unit in UnitManager.Instance.GetAllUnits())
        {
            GameObject instObj = Instantiate(tuBarPrefab, tuListContainer);
            UI_TUBarItem itemScript = instObj.GetComponent<UI_TUBarItem>();

            if (itemScript != null)
            {
                bool isPlayer = unit.ConfigData.characterID == 1001;

                // 【🔥核心修改】传入强引用的 portraitSprite
                itemScript.Init(
                    unit.InstanceID,
                    unit.ConfigData.characterName,
                    isPlayer,
                    unit.ConfigData.portraitSprite
                );

                _tuBarItems.Add(itemScript);
            }
        }
    }

    private void Update()
    {
        if (TurnManager.Instance == null) return;

        if (txtRound) txtRound.text = $"回合: {TurnManager.Instance.CurrentRound}";
        if (txtState) txtState.text = $"阶段: {TurnManager.Instance.CurrentState}";

        int selectedUnitID = (HexMapView.Instance != null && HexMapView.Instance.SelectedUnit != null)
                           ? HexMapView.Instance.SelectedUnit.InstanceID : -1;

        int maxTU = TurnManager.Instance.MaxTUPerTurn;
        foreach (var item in _tuBarItems)
        {
            item.UpdateState(TurnManager.Instance.GetUnitPlannedTU(item.GetBoundUnitID()), maxTU, item.GetBoundUnitID() == selectedUnitID);
        }

        bool isPlanning = (TurnManager.Instance.CurrentState == TGame.Battle.BattleState.Planning);
        if (btnEndTurn) btnEndTurn.interactable = isPlanning;
        if (btnSkill) btnSkill.interactable = isPlanning;
        if (btnItem) btnItem.interactable = isPlanning;
    }

    private void RequestMockAction(string actionName, int cost)
    {
        if (HexMapView.Instance == null || HexMapView.Instance.SelectedUnit == null) return;
        MockActionCommand cmd = new MockActionCommand(HexMapView.Instance.SelectedUnit.InstanceID, actionName, cost);
        if (cmd.Validate()) TurnManager.Instance.AddCommand(cmd);
    }
}