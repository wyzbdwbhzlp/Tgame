using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TGame.Battle;
using Game.UI;
using System.Collections.Generic;
using System.Linq;

public class UI_BattleMain : UIBase
{
    [Header("文字与按钮引用")]
    public TextMeshProUGUI txtRound;
    public TextMeshProUGUI txtState;
    public Button btnEndTurn;
    public Button btnSkill;
    public Button btnItem;
    public Button btnUndo;

    [Header("时素列表资产化引用")]
    public RectTransform tuListContainer; // 【目标父节点】UI 上的空物体容器
    public GameObject tuBarPrefab;        // 【要生成的子物体】时素条预制体

    private List<UI_TUBarItem> _tuBarItems = new List<UI_TUBarItem>();

    public override void OnInit()
    {
        base.OnInit();
        this.uiLayer = UILayer.Normal;

        if (btnEndTurn) btnEndTurn.onClick.AddListener(() => TurnManager.Instance.EndPlayerTurn());
        if (btnSkill) btnSkill.onClick.AddListener(() => RequestMockAction("Skill", 3));
        if (btnItem) btnItem.onClick.AddListener(() => RequestMockAction("Item", 2));
        if (btnUndo) btnUndo.onClick.AddListener(() => TurnManager.Instance.UndoLastCommand());
    }

    protected override void OnShow()
    {
        base.OnShow();
        SpawnTUBars();
    }

    private void SpawnTUBars()
    {
        // 1. 防呆检测：确保父节点和预制体都已经拖拽赋值
        if (tuListContainer == null)
        {
            Debug.LogError("❌ [UI_BattleMain] 时素条容器 (Tu List Container) 丢失！");
            return;
        }
        if (tuBarPrefab == null)
        {
            Debug.LogError("❌ [UI_BattleMain] 时素条预制体 (Tu Bar Prefab) 丢失！");
            return;
        }
        if (UnitManager.Instance == null) return;

        // 2. 清空旧数据：把容器 (父节点) 下面所有的旧时素条 (子物体) 全部删掉
        foreach (Transform child in tuListContainer) Destroy(child.gameObject);
        _tuBarItems.Clear();

        var allUnits = UnitManager.Instance.GetAllUnits().ToList();

        if (allUnits.Count == 0)
        {
            Debug.LogWarning("⚠️ [UI_BattleMain] 场上没有任何角色！所以不生成时素条。");
            return;
        }

        // 3. 遍历场上角色，生成时素条
        foreach (var unit in allUnits)
        {
            // ==============================================================
            // 【🔥核心逻辑】将时素条 prefab 生成为 Tu List Container 的子 object
            // 参数1: tuBarPrefab (你要生成的东西)
            // 参数2: tuListContainer (你要把它挂在谁的下面当儿子)
            // 参数3: false (强制保持 UI 自身的 RectTransform 大小，不要被乱缩放)
            // ==============================================================
            GameObject instObj = Instantiate(tuBarPrefab, tuListContainer, false);

            // 强制将缩放比例锁死为 1，将本地坐标归零（防重叠和乱飘）
            instObj.transform.localScale = Vector3.one;
            instObj.transform.localPosition = Vector3.zero;

            UI_TUBarItem itemScript = instObj.GetComponent<UI_TUBarItem>();

            if (itemScript != null)
            {
                bool isPlayer = unit.ConfigData.characterID == 1001;
                itemScript.Init(unit.InstanceID, unit.ConfigData.characterName, isPlayer, unit.ConfigData.portraitSprite);
                _tuBarItems.Add(itemScript);
            }
        }

        Debug.Log($"<color=green>✅ [UI_BattleMain] 成功将 {allUnits.Count} 个时素条生成为容器的子物体！</color>");
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
        if (btnUndo) btnUndo.interactable = isPlanning;
    }

    private void RequestMockAction(string actionName, int cost)
    {
        if (HexMapView.Instance == null || HexMapView.Instance.SelectedUnit == null) return;
        MockActionCommand cmd = new MockActionCommand(HexMapView.Instance.SelectedUnit.InstanceID, actionName, cost);
        if (cmd.Validate()) TurnManager.Instance.AddCommand(cmd);
    }
}