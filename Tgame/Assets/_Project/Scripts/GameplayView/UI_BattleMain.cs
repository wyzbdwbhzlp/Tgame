using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TGame.Battle;

public class UI_BattleMain : MonoBehaviour
{
    [Header("UI引用")]
    public TextMeshProUGUI txtRound;
    public TextMeshProUGUI txtTU;
    public TextMeshProUGUI txtState;
    public Button btnEndTurn;
    public Button btnSkill;
    public Button btnItem;

    private void Start()
    {
        if (btnEndTurn) btnEndTurn.onClick.AddListener(() => TurnManager.Instance.EndPlayerTurn());
        if (btnSkill) btnSkill.onClick.AddListener(() => RequestMockAction("Skill", 3));
        if (btnItem) btnItem.onClick.AddListener(() => RequestMockAction("Item", 2));
    }

    private void Update()
    {
        if (TurnManager.Instance == null) return;

        if (txtRound) txtRound.text = $"Round: {TurnManager.Instance.CurrentRound}";

        // UI 显示【已规划】的时素，这样点击移动或按钮时条会立刻涨
        if (txtTU) txtTU.text = $"TimeUnits: {TurnManager.Instance.PlannedTimeUnitsUsed} / 13";

        if (txtState) txtState.text = $"State: {TurnManager.Instance.CurrentState}";

        bool isPlanning = (TurnManager.Instance.CurrentState == TGame.Battle.BattleState.Planning);
        if (btnEndTurn) btnEndTurn.interactable = isPlanning;
        if (btnSkill) btnSkill.interactable = isPlanning;
        if (btnItem) btnItem.interactable = isPlanning;
    }

    private void RequestMockAction(string n, int c)
    {
        if (TurnManager.Instance == null) return;

        // 1. 创建指令对象
        MockActionCommand cmd = new MockActionCommand(n, c);

        // 2. 校验并加入队列（结算报不报 Log 就看这一步）
        if (cmd.Validate())
        {
            Debug.Log($"<color=orange>[决策] 已规划{n}，消耗 {c} TU</color>");
            TurnManager.Instance.AddCommand(cmd);
        }
        else
        {
            Debug.LogWarning("时素不足！");
        }
    }
}