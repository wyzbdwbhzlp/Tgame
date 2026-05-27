using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TGame.Battle;
using Game.UI;
using System.Collections.Generic;
using System.Linq;
using TGame.Core;
using TGame.Data;
using TGame.UI;

public class UI_BattleMain : UIBase
{
    [Header("全局主菜单")]
    public TextMeshProUGUI txtRound;
    public TextMeshProUGUI txtState;
    public Button btnEndTurn;
    public Button btnUndo;

    [Header("二级弹窗 (Action Popup 主指令)")]
    public GameObject actionPopupPanel;
    public TextMeshProUGUI txtPopupTitle;
    public Button btnPopupMove;
    public Button btnPopupAttack;
    public Button btnPopupSkill;  // 【🔥恢复】点击展开技能列表的按钮
    public Button btnPopupItem;
    public Button btnPopupCancel;

    [Header("三级弹窗 (Skill List 技能列表)")]
    public GameObject skillListPanel;           // 技能列表的独立面板
    public RectTransform skillListContainer;    // 挂载VerticalLayoutGroup的容器
    public GameObject skillItemPrefab;          // UI_SkillItem 预制体
    public Button btnCloseSkillList;            // 返回按钮（关掉技能表，回到主指令）

    [Header("四级弹窗 (Skill Tooltip 悬浮描述)")]
    public GameObject skillTooltipPanel;
    public TextMeshProUGUI txtTooltipName;
    public TextMeshProUGUI txtTooltipCost;
    public TextMeshProUGUI txtTooltipDesc;

    [Header("时素列表")]
    public RectTransform tuListContainer;
    public GameObject tuBarPrefab;
    private List<UI_TUBarItem> _tuBarItems = new List<UI_TUBarItem>();

    private bool _isInitialized = false;
    private bool _hasBoundMapEvents = false;
    private RuntimeUnit _currentFocusedUnit = null;
    [Header("左下角角色状态列表")]
    public RectTransform unitStatusListContainer; // 挂载了 VerticalLayoutGroup 的空物体
    public GameObject unitStatusItemPrefab;       // 刚刚写的 UI_UnitStatusItem 预制体
    private void Start() { InitUIBindings(); }
    public override void OnInit() { base.OnInit(); InitUIBindings(); }

    private void InitUIBindings()
    {
        if (_isInitialized) return;
        _isInitialized = true;
        this.uiLayer = UILayer.Normal;

        HideAllPopups();

        if (btnEndTurn) btnEndTurn.onClick.AddListener(() => TurnManager.Instance.EndPlayerTurn());
        if (btnUndo) btnUndo.onClick.AddListener(() => TurnManager.Instance.UndoLastCommand());

        if (btnPopupMove) btnPopupMove.onClick.AddListener(OnBtnMoveClicked);
        if (btnPopupAttack) btnPopupAttack.onClick.AddListener(OnBtnAttackClicked);
        if (btnPopupItem) btnPopupItem.onClick.AddListener(() => RequestMockAction("Item", 2));

        // 【🔥核心修改】绑定打开技能列表
        if (btnPopupSkill) btnPopupSkill.onClick.AddListener(OpenSkillList);

        if (btnPopupCancel) btnPopupCancel.onClick.AddListener(() =>
        {
            if (HexMapView.Instance != null) HexMapView.Instance.CancelSelection();
        });

        // 【🔥核心修改】绑定返回按钮
        if (btnCloseSkillList) btnCloseSkillList.onClick.AddListener(() =>
        {
            if (skillListPanel) skillListPanel.SetActive(false);
            if (skillTooltipPanel) skillTooltipPanel.SetActive(false);
            if (actionPopupPanel) actionPopupPanel.SetActive(true); // 退回主指令面板
        });

        SpawnTUBars();
        SpawnUnitStatusList();
    }

    private void OnDestroy()
    {
        if (HexMapView.Instance != null && _hasBoundMapEvents)
        {
            HexMapView.Instance.OnUnitSelected -= ShowActionPopup;
            HexMapView.Instance.OnUnitDeselected -= HideAllPopups;
        }
    }

    // ==========================================
    // 二级弹窗：主指令列表 (移动/攻击/技能/道具)
    // ==========================================
    private void ShowActionPopup(RuntimeUnit unit)
    {
        if (unit != null && unit.Side != 1001) return;

        if (actionPopupPanel != null && unit != null)
        {
            HideAllPopups(); // 先清理掉所有遗留的面板

            actionPopupPanel.SetActive(true);

            if (txtPopupTitle) txtPopupTitle.text = $"指挥：{unit.ConfigData.characterName}";
            _currentFocusedUnit = unit;

            UpdatePopupPosition(actionPopupPanel.GetComponent<RectTransform>());
        }
    }
    // ==========================================
    // 【🔥新增】生成左下角的角色状态列表
    // ==========================================
    private void SpawnUnitStatusList()
    {
        if (unitStatusListContainer == null || unitStatusItemPrefab == null || UnitManager.Instance == null) return;

        // 清理旧列表
        foreach (Transform child in unitStatusListContainer) Destroy(child.gameObject);

        var allUnits = UnitManager.Instance.GetAllUnits().ToList();
        foreach (var unit in allUnits)
        {
            // 生成预制体
            GameObject instObj = Instantiate(unitStatusItemPrefab, unitStatusListContainer);
            UI_UnitStatusItem itemScript = instObj.GetComponent<UI_UnitStatusItem>();

            if (itemScript != null)
            {
                // 初始化，并把点击回调传进去
                itemScript.Init(unit, OnUnitStatusClicked);
            }
        }
    }

    private void OnUnitStatusClicked(int clickedUnitID)
    {
        var unit = UnitManager.Instance.GetUnit(clickedUnitID);
        if (unit != null && HexMapView.Instance != null)
        {
            Debug.Log($"<color=cyan>[UI] 通过左下角列表选中了角色: {unit.ConfigData.characterName}</color>");
            HexMapView.Instance.ForceSelectUnit(unit);
        }
    }
    private void HideAllPopups()
    {
        if (actionPopupPanel) actionPopupPanel.SetActive(false);
        if (skillListPanel) skillListPanel.SetActive(false);
        if (skillTooltipPanel) skillTooltipPanel.SetActive(false);
        _currentFocusedUnit = null;
    }

    private void UpdatePopupPosition(RectTransform panelRect)
    {
        if (panelRect == null || _currentFocusedUnit == null || GridSystem.Instance == null) return;
        if (!panelRect.gameObject.activeSelf) return;

        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Camera uiCam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

        Vector3 unitWorldPos = GridSystem.Instance.CellToWorld(_currentFocusedUnit.GridPosition);
        Vector2 screenPos = mainCam.WorldToScreenPoint(unitWorldPos);

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, screenPos, uiCam, out Vector3 uiWorldPos))
        {
            panelRect.position = uiWorldPos;
            panelRect.localPosition += new Vector3(140f, 40f, 0f);
        }
    }

    private void OnBtnMoveClicked()
    {
        if (HexMapView.Instance != null) HexMapView.Instance.EnterMoveMode();
        HideAllPopups();
    }

    private void OnBtnAttackClicked()
    {
        if (HexMapView.Instance != null) HexMapView.Instance.EnterAttackMode();
        HideAllPopups();
    }

    // ==========================================
    // 三级弹窗：展开技能列表
    // ==========================================
    private void OpenSkillList()
    {
        if (_currentFocusedUnit == null || skillListPanel == null || skillItemPrefab == null) return;

        var skillIDs = _currentFocusedUnit.ConfigData.skillIDs;
        if (skillIDs == null || skillIDs.Count == 0)
        {
            Debug.LogWarning($"[UI] 角色 {_currentFocusedUnit.ConfigData.characterName} 没有配置任何技能！");
            return;
        }

        // 隐藏二级主指令，打开三级技能列表
        if (actionPopupPanel) actionPopupPanel.SetActive(false);
        skillListPanel.SetActive(true);
        UpdatePopupPosition(skillListPanel.GetComponent<RectTransform>());

        // 清理旧的技能按钮
        foreach (Transform child in skillListContainer) Destroy(child.gameObject);

        // 动态生成技能按钮
        foreach (int skillID in skillIDs)
        {
            SkillData skillData = DataManager.Instance.GetSkillData(skillID);
            if (skillData == null) continue;

            GameObject btnObj = Instantiate(skillItemPrefab, skillListContainer);
            UI_SkillItem skillItem = btnObj.GetComponent<UI_SkillItem>();
            if (skillItem != null)
            {
                skillItem.Init(skillData, OnSkillSelected, ShowSkillTooltip, HideSkillTooltip);
            }
        }
    }

    private void OnSkillSelected(int selectedSkillID)
    {
        SkillData skillData = DataManager.Instance.GetSkillData(selectedSkillID);
        if (skillData != null)
        {
            if (HexMapView.Instance != null) HexMapView.Instance.EnterSkillMode(selectedSkillID);
            HideAllPopups(); // 选完技能，关闭所有面板开始瞄准
        }
    }

    // ==========================================
    // 四级弹窗：鼠标悬停的 Tooltip 控制
    // ==========================================
    private void ShowSkillTooltip(SkillData data, RectTransform btnRect)
    {
        if (skillTooltipPanel == null) return;
        skillTooltipPanel.SetActive(true);

        if (txtTooltipName) txtTooltipName.text = data.skillName;
        if (txtTooltipCost) txtTooltipCost.text = $"消耗: {data.tuCost}TU / {data.mpCost}MP\n射程: {data.castRange}  范围: {data.aoeRadius}";
        if (txtTooltipDesc) txtTooltipDesc.text = data.description;

        RectTransform tooltipRect = skillTooltipPanel.GetComponent<RectTransform>();
        if (tooltipRect != null && btnRect != null)
        {
            tooltipRect.position = btnRect.position;
            tooltipRect.localPosition += new Vector3(btnRect.rect.width / 2f + tooltipRect.rect.width / 2f + 10f, 0, 0);
        }
    }

    private void HideSkillTooltip()
    {
        if (skillTooltipPanel != null) skillTooltipPanel.SetActive(false);
    }

    private void RequestMockAction(string actionName, int cost)
    {
        if (HexMapView.Instance == null || HexMapView.Instance.SelectedUnit == null) return;
        MockActionCommand cmd = new MockActionCommand(HexMapView.Instance.SelectedUnit.InstanceID, actionName, cost);
        if (cmd.Validate()) { TurnManager.Instance.AddCommand(cmd); HexMapView.Instance.CancelSelection(); }
    }

    private void SpawnTUBars()
    {
        if (tuListContainer == null || tuBarPrefab == null || UnitManager.Instance == null) return;
        foreach (Transform child in tuListContainer) Destroy(child.gameObject);
        _tuBarItems.Clear();

        var allUnits = UnitManager.Instance.GetAllUnits().ToList();
        foreach (var unit in allUnits)
        {
            if (unit.Side != 1001) continue;
            GameObject instObj = Instantiate(tuBarPrefab, tuListContainer, false);
            UI_TUBarItem itemScript = instObj.GetComponent<UI_TUBarItem>();
            if (itemScript != null)
            {
                itemScript.Init(unit.InstanceID, unit.ConfigData.characterName, true, unit.ConfigData.portraitSprite);
                _tuBarItems.Add(itemScript);
            }
        }
    }

    private void Update()
    {
        if (!_hasBoundMapEvents && HexMapView.Instance != null)
        {
            HexMapView.Instance.OnUnitSelected += ShowActionPopup;
            HexMapView.Instance.OnUnitDeselected += HideAllPopups;
            _hasBoundMapEvents = true;
        }

        if (TurnManager.Instance == null) return;

        if (txtRound) txtRound.text = $"回合: {TurnManager.Instance.CurrentRound}";
        if (txtState) txtState.text = $"阶段: {TurnManager.Instance.CurrentState}";

        int selectedUnitID = (HexMapView.Instance != null && HexMapView.Instance.SelectedUnit != null) ? HexMapView.Instance.SelectedUnit.InstanceID : -1;
        int maxTU = TurnManager.Instance.MaxTUPerTurn;

        foreach (var item in _tuBarItems)
        {
            item.UpdateState(TurnManager.Instance.GetUnitPlannedTU(item.GetBoundUnitID()), maxTU, item.GetBoundUnitID() == selectedUnitID);
        }

        bool isPlanning = (TurnManager.Instance.CurrentState == TGame.Battle.BattleState.Planning);
        if (btnEndTurn) btnEndTurn.interactable = isPlanning;
        if (btnUndo) btnUndo.interactable = isPlanning;
    }

    private void LateUpdate()
    {
        // 根据当前谁处于激活状态，让谁跟随角色移动
        if (actionPopupPanel != null && actionPopupPanel.activeSelf)
        {
            UpdatePopupPosition(actionPopupPanel.GetComponent<RectTransform>());
        }
        else if (skillListPanel != null && skillListPanel.activeSelf)
        {
            UpdatePopupPosition(skillListPanel.GetComponent<RectTransform>());
        }
    }
}