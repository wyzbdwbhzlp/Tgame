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
using DG.Tweening;
using UnityEngine.EventSystems;

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
    public Button btnPopupSkill;
    public Button btnPopupItem;
    public Button btnPopupCancel;

    [Header("三级弹窗 (Skill List 技能列表)")]
    public GameObject skillListPanel;
    public RectTransform skillListContainer;
    public GameObject skillItemPrefab;
    public Button btnCloseSkillList;

    [Header("四级弹窗 (Skill Tooltip 悬浮描述)")]
    public GameObject skillTooltipPanel;
    public TextMeshProUGUI txtTooltipName;
    public TextMeshProUGUI txtTooltipCost;
    public TextMeshProUGUI txtTooltipDesc;

    [Header("时素列表")]
    public RectTransform tuListContainer;
    public GameObject tuBarPrefab;
    private List<UI_TUBarItem> _tuBarItems = new List<UI_TUBarItem>();

    [Header("左下角角色状态列表")]
    public RectTransform unitStatusListContainer;
    public GameObject unitStatusItemPrefab;

    [Header("UI 弹窗偏移设置")]
    public Vector2 popupOffset = new Vector2(100f, 50f);

    [Header("UI 月牙形弧形动画设置")]
    public float arcSpacing = 65f;
    public float arcDepth = 50f;
    public float arcDuration = 0.35f;

    [Header("选中角色特效")]
    public RectTransform selectionEffectImage;
    public float selectionEffectOffsetY = 1f;

    [Header("屏幕中央播报")]
    public TextMeshProUGUI txtBroadcast;
    // 【🔥修改】将 DOTween 序列改为协程引用
    private Coroutine _broadcastCoroutine;

    private Tweener _selectionScaleTween;
    private Tween _selectionRotateTween;

    private bool _isInitialized = false;
    private bool _hasBoundMapEvents = false;
    private RuntimeUnit _currentFocusedUnit = null;

    public static UI_BattleMain Instance { get; private set; }

    private void Awake() { Instance = this; }
    private void Start() { Instance = this; InitUIBindings(); }
    private void OnEnable() { Instance = this; }
    public override void OnInit() { base.OnInit(); Instance = this; InitUIBindings(); }

    private void InitUIBindings()
    {
        if (_isInitialized) return;
        _isInitialized = true;
        this.uiLayer = UILayer.Normal;

        HideAllPopups();
        if (txtBroadcast != null) txtBroadcast.gameObject.SetActive(false);

        if (btnEndTurn) btnEndTurn.onClick.AddListener(() => TurnManager.Instance.EndPlayerTurn());
        if (btnUndo) btnUndo.onClick.AddListener(() => TurnManager.Instance.UndoLastCommand());

        if (btnPopupMove) btnPopupMove.onClick.AddListener(OnBtnMoveClicked);
        if (btnPopupAttack) btnPopupAttack.onClick.AddListener(OnBtnAttackClicked);
        if (btnPopupItem) btnPopupItem.onClick.AddListener(() => RequestMockAction("Item", 2));
        if (btnPopupSkill) btnPopupSkill.onClick.AddListener(OpenSkillList);

        if (btnPopupCancel) btnPopupCancel.onClick.AddListener(() =>
        {
            if (HexMapView.Instance != null) HexMapView.Instance.CancelSelection();
        });

        if (btnCloseSkillList) btnCloseSkillList.onClick.AddListener(() =>
        {
            if (skillListPanel) skillListPanel.SetActive(false);
            if (skillTooltipPanel) skillTooltipPanel.SetActive(false);
            if (actionPopupPanel) actionPopupPanel.SetActive(true);

            PlayMenuArcAnimation(actionPopupPanel.transform);
        });

        BindHoverEffect(btnPopupMove);
        BindHoverEffect(btnPopupAttack);
        BindHoverEffect(btnPopupSkill);
        BindHoverEffect(btnPopupItem);
        BindHoverEffect(btnPopupCancel);
        BindHoverEffect(btnCloseSkillList);

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

        _selectionScaleTween?.Kill();
        _selectionRotateTween?.Kill();

        if (_broadcastCoroutine != null) StopCoroutine(_broadcastCoroutine);

        if (Instance == this) Instance = null;
    }

    // ==========================================
    // 【🔥绝对防弹版】原生协程动画，无视任何插件异常！
    // ==========================================
    public void ShowBroadcastMessage(string message)
    {
        Debug.Log($"<color=orange>[UI播报触发]</color> {message}");

        if (txtBroadcast == null) return;

        // 如果当前正在播报，强制停止旧动画
        if (_broadcastCoroutine != null) StopCoroutine(_broadcastCoroutine);

        // 开启新一轮播报动画
        _broadcastCoroutine = StartCoroutine(BroadcastRoutine(message));
    }

    private System.Collections.IEnumerator BroadcastRoutine(string message)
    {
        // 1. 强制激活并置顶
        txtBroadcast.gameObject.SetActive(true);
        txtBroadcast.transform.SetAsLastSibling();
        txtBroadcast.text = message;

        // 2. 获取并重置 CanvasGroup
        CanvasGroup cg = txtBroadcast.GetComponent<CanvasGroup>();
        if (cg == null) cg = txtBroadcast.gameObject.AddComponent<CanvasGroup>();

        txtBroadcast.rectTransform.anchoredPosition = new Vector2(0, 50f);
        txtBroadcast.transform.localScale = Vector3.one * 0.8f;
        cg.alpha = 0f;

        // 阶段一：放大 + 淡入 (0.2秒)
        float t = 0;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            float p = t / 0.2f;
            cg.alpha = Mathf.Lerp(0f, 1f, p);
            // 简单的曲线模拟 OutBack 回弹效果
            float scale = Mathf.Lerp(0.8f, 1.2f, Mathf.Sin(p * Mathf.PI / 2));
            txtBroadcast.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        // 阶段二：稍微回缩 (0.1秒)
        t = 0;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            float p = t / 0.1f;
            txtBroadcast.transform.localScale = Vector3.Lerp(Vector3.one * 1.2f, Vector3.one, p);
            yield return null;
        }

        // 阶段三：悬浮停留展示文字 (1.2秒)
        yield return new WaitForSeconds(1.2f);

        // 阶段四：向上飘逸 + 淡出 (0.3秒)
        t = 0;
        Vector2 startPos = txtBroadcast.rectTransform.anchoredPosition;
        Vector2 targetPos = startPos + new Vector2(0, 100f);
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float p = t / 0.3f;
            cg.alpha = Mathf.Lerp(1f, 0f, p);
            txtBroadcast.rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, p);
            yield return null;
        }

        // 最终清理
        txtBroadcast.gameObject.SetActive(false);
        _broadcastCoroutine = null;
    }

    private void BindHoverEffect(Button btn)
    {
        if (btn == null) return;

        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener((e) => {
            btn.transform.DOKill();
            btn.transform.DOScale(1.15f, 0.25f).SetEase(Ease.OutBack);
        });
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener((e) => {
            btn.transform.DOKill();
            btn.transform.DOScale(1.0f, 0.2f).SetEase(Ease.OutQuad);
        });
        trigger.triggers.Add(exit);

        EventTrigger.Entry down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener((e) => {
            btn.transform.DOKill();
            btn.transform.DOScale(0.95f, 0.1f).SetEase(Ease.OutQuad);
        });
        trigger.triggers.Add(down);
    }

    private void ShowActionPopup(RuntimeUnit unit)
    {
        if (unit != null && unit.Side != 1001) return;

        if (actionPopupPanel != null && unit != null)
        {
            HideAllPopups();

            actionPopupPanel.SetActive(true);

            if (txtPopupTitle) txtPopupTitle.text = $"[ {unit.ConfigData.characterName} ]";
            _currentFocusedUnit = unit;

            UpdatePopupPosition(actionPopupPanel.GetComponent<RectTransform>());

            PlayMenuArcAnimation(actionPopupPanel.transform);

            PlaySelectionEffect();
        }
    }

    private void PlayMenuArcAnimation(Transform container)
    {
        List<RectTransform> activeButtons = new List<RectTransform>();

        Button[] allButtons = container.GetComponentsInChildren<Button>(false);

        foreach (Button btn in allButtons)
        {
            activeButtons.Add(btn.GetComponent<RectTransform>());
        }

        int count = activeButtons.Count;
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            RectTransform btn = activeButtons[i];

            btn.anchorMin = new Vector2(0.5f, 0.5f);
            btn.anchorMax = new Vector2(0.5f, 0.5f);
            btn.pivot = new Vector2(0.5f, 0.5f);

            btn.DOKill();
            btn.localScale = Vector3.one;

            float targetY = ((count - 1) / 2f - i) * arcSpacing;

            float t = count > 1 ? (i - (count - 1) / 2f) / ((count - 1) / 2f) : 0f;
            float targetX = Mathf.Cos(t * Mathf.PI / 2f) * arcDepth;

            btn.localPosition = Vector3.zero;
            btn.DOLocalMove(new Vector3(targetX, targetY, 0), arcDuration)
               .SetEase(Ease.OutBack)
               .SetDelay(i * 0.04f);
        }
    }

    [ContextMenu("✨ 立即测试当前月牙排布 (仅在运行模式Play下有效)")]
    public void TestArcAnimation()
    {
        if (Application.isPlaying)
        {
            if (actionPopupPanel != null && actionPopupPanel.activeSelf)
                PlayMenuArcAnimation(actionPopupPanel.transform);
            if (skillListContainer != null && skillListPanel.activeSelf)
                PlayMenuArcAnimation(skillListContainer);
        }
        else
        {
            Debug.LogWarning("请先点击 Play 运行游戏，并点选一名角色展开菜单后，再使用此测试功能！");
        }
    }

    private void PlaySelectionEffect()
    {
        if (selectionEffectImage == null) return;
        UpdateSelectionEffectPosition();
        selectionEffectImage.gameObject.SetActive(true);

        _selectionScaleTween?.Kill();
        _selectionRotateTween?.Kill();

        selectionEffectImage.localScale = Vector3.zero;
        selectionEffectImage.localEulerAngles = Vector3.zero;

        _selectionScaleTween = selectionEffectImage.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);

        Sequence clockSeq = DOTween.Sequence();
        clockSeq.Append(selectionEffectImage.DORotate(new Vector3(0, 0, -30f), 0.15f, RotateMode.LocalAxisAdd)
                                            .SetEase(Ease.OutBack, 2f));
        clockSeq.AppendInterval(0.85f);
        clockSeq.SetLoops(-1);

        _selectionRotateTween = clockSeq;
    }

    private void SpawnUnitStatusList()
    {
        if (unitStatusListContainer == null || unitStatusItemPrefab == null || UnitManager.Instance == null) return;

        foreach (Transform child in unitStatusListContainer)
        {
            child.DOKill();
            Destroy(child.gameObject);
        }
        unitStatusListContainer.DetachChildren();

        var allUnits = UnitManager.Instance.GetAllUnits().ToList();
        foreach (var unit in allUnits)
        {
            GameObject instObj = Instantiate(unitStatusItemPrefab, unitStatusListContainer);
            UI_UnitStatusItem itemScript = instObj.GetComponent<UI_UnitStatusItem>();

            if (itemScript != null)
            {
                itemScript.Init(unit, OnUnitStatusClicked);
            }
        }
    }

    private void OnUnitStatusClicked(int clickedUnitID)
    {
        var unit = UnitManager.Instance.GetUnit(clickedUnitID);
        if (unit != null && HexMapView.Instance != null)
        {
            HexMapView.Instance.ForceSelectUnit(unit);
        }
    }

    private void HideAllPopups()
    {
        if (actionPopupPanel) actionPopupPanel.SetActive(false);
        if (skillListPanel) skillListPanel.SetActive(false);
        if (skillTooltipPanel) skillTooltipPanel.SetActive(false);
        _currentFocusedUnit = null;

        if (selectionEffectImage != null)
        {
            selectionEffectImage.gameObject.SetActive(false);
            _selectionScaleTween?.Kill();
            _selectionRotateTween?.Kill();
        }
    }

    private void UpdatePopupPosition(RectTransform panelRect)
    {
        if (panelRect == null || _currentFocusedUnit == null || GridSystem.Instance == null) return;
        if (!panelRect.gameObject.activeSelf) return;

        Camera mainCam = Camera.main;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (mainCam == null || canvas == null) return;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Camera uiCam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

        Vector3 unitWorldPos = GridSystem.Instance.CellToWorld(_currentFocusedUnit.GridPosition);
        Vector2 screenPos = mainCam.WorldToScreenPoint(unitWorldPos);

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, screenPos, uiCam, out Vector3 uiWorldPos))
        {
            panelRect.position = uiWorldPos;
            panelRect.localPosition += new Vector3(popupOffset.x, popupOffset.y, 0f);
        }
    }

    private void UpdateSelectionEffectPosition()
    {
        if (selectionEffectImage == null || _currentFocusedUnit == null || GridSystem.Instance == null) return;

        Camera mainCam = Camera.main;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (mainCam == null || canvas == null) return;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Camera uiCam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

        Vector3 unitWorldPos = GridSystem.Instance.CellToWorld(_currentFocusedUnit.GridPosition);
        unitWorldPos += new Vector3(0, selectionEffectOffsetY, 0);

        Vector2 screenPos = mainCam.WorldToScreenPoint(unitWorldPos);

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, screenPos, uiCam, out Vector3 uiWorldPos))
        {
            selectionEffectImage.position = uiWorldPos;
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

    private void OpenSkillList()
    {
        if (_currentFocusedUnit == null || skillListPanel == null || skillItemPrefab == null) return;

        var skillIDs = _currentFocusedUnit.ConfigData.skillIDs;
        if (skillIDs == null || skillIDs.Count == 0) return;

        if (actionPopupPanel) actionPopupPanel.SetActive(false);
        skillListPanel.SetActive(true);
        UpdatePopupPosition(skillListPanel.GetComponent<RectTransform>());

        foreach (Transform child in skillListContainer)
        {
            child.DOKill();
            Destroy(child.gameObject);
        }
        skillListContainer.DetachChildren();

        foreach (int skillID in skillIDs)
        {
            SkillData skillData = DataManager.Instance.GetSkillData(skillID);
            if (skillData == null) continue;

            GameObject btnObj = Instantiate(skillItemPrefab, skillListContainer);
            UI_SkillItem skillItem = btnObj.GetComponent<UI_SkillItem>();
            if (skillItem != null)
            {
                skillItem.Init(skillData, OnSkillSelected, ShowSkillTooltip, HideSkillTooltip);
                BindHoverEffect(btnObj.GetComponent<Button>());
            }
        }

        PlayMenuArcAnimation(skillListContainer);
    }

    private void OnSkillSelected(int selectedSkillID)
    {
        SkillData skillData = DataManager.Instance.GetSkillData(selectedSkillID);
        if (skillData != null)
        {
            if (HexMapView.Instance != null) HexMapView.Instance.EnterSkillMode(selectedSkillID);
            HideAllPopups();
        }
    }

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
            tooltipRect.localPosition += new Vector3(btnRect.rect.width / 2f + tooltipRect.rect.width / 2f + 25f, 0, 0);
        }
    }

    private void HideSkillTooltip()
    {
        if (skillTooltipPanel != null) skillTooltipPanel.SetActive(false);
    }

    private void RequestMockAction(string actionName, int cost)
    {
        if (HexMapView.Instance == null || HexMapView.Instance.SelectedUnit == null) return;

        if (!TurnManager.Instance.CanScheduleAction(HexMapView.Instance.SelectedUnit.InstanceID, cost))
        {
            ShowBroadcastMessage($"时素(TU)不足！需要 {cost} 点");
            return;
        }

        MockActionCommand cmd = new MockActionCommand(HexMapView.Instance.SelectedUnit.InstanceID, actionName, cost);
        if (cmd.Validate()) { TurnManager.Instance.AddCommand(cmd); HexMapView.Instance.CancelSelection(); }
    }

    private void SpawnTUBars()
    {
        if (tuListContainer == null || tuBarPrefab == null || UnitManager.Instance == null) return;

        foreach (Transform child in tuListContainer)
        {
            child.DOKill();
            Destroy(child.gameObject);
        }
        tuListContainer.DetachChildren();

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
            var actions = TurnManager.Instance.GetUnitScheduledActions(item.GetBoundUnitID());
            item.UpdateTimeline(actions, maxTU, item.GetBoundUnitID() == selectedUnitID);
        }

        bool isPlanning = (TurnManager.Instance.CurrentState == TGame.Battle.BattleState.Planning);
        if (btnEndTurn) btnEndTurn.interactable = isPlanning;
        if (btnUndo) btnUndo.interactable = isPlanning;
    }

    private void LateUpdate()
    {
        if (actionPopupPanel != null && actionPopupPanel.activeSelf)
        {
            UpdatePopupPosition(actionPopupPanel.GetComponent<RectTransform>());
        }
        else if (skillListPanel != null && skillListPanel.activeSelf)
        {
            UpdatePopupPosition(skillListPanel.GetComponent<RectTransform>());
        }

        if (selectionEffectImage != null && selectionEffectImage.gameObject.activeSelf)
        {
            UpdateSelectionEffectPosition();
        }
    }
}