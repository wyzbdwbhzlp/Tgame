using System.Collections.Generic;
using UnityEngine;

namespace TGame.Battle
{
    public interface ICommand
    {
        bool Validate();
        void Execute();
        int GetCost(); // 新增：获取该指令的消耗
    }

    // --- 移动指令 ---
    public class MoveCommand : ICommand
    {
        private int _unitID;
        private Vector3Int _start;
        private Vector3Int _target;
        private int _stepCost;

        public MoveCommand(int unitID, Vector3Int start, Vector3Int target)
        {
            _unitID = unitID;
            _start = start;
            _target = target;
            _stepCost = (Mathf.Abs(start.x - target.x) + Mathf.Abs(start.y - target.y) + Mathf.Abs(start.z - target.z)) / 2;
        }

        public int GetCost() => _stepCost;

        public bool Validate() => TurnManager.Instance != null && TurnManager.Instance.CanScheduleAction(_stepCost);

        public void Execute()
        {
            var unit = UnitManager.Instance.GetUnit(_unitID);
            var view = UnitViewManager.Instance.GetView(_unitID);

            if (unit != null)
            {
                Debug.Log($"<color=white>[指令执行] 角色 {unit.ConfigData.characterName} 开始从 {_start} 往 {_target} 结算</color>");
                List<GridCell> path = PathfindingService.GetPath(GridSystem.Instance, _start, _target);

                GridSystem.Instance.GetCell(_start).OccupantUnitID = -1;
                unit.GridPosition = _target;
                GridSystem.Instance.GetCell(_target).OccupantUnitID = _unitID;

                TurnManager.Instance.AdvanceTime(_stepCost);

                if (view != null && path != null && path.Count > 0)
                {
                    view.MoveAlongPath(path);
                }
            }
        }
    }

    // --- 新增：模拟动作指令 (技能、道具) ---
    public class MockActionCommand : ICommand
    {
        private string _actionName;
        private int _cost;

        public MockActionCommand(string name, int cost)
        {
            _actionName = name;
            _cost = cost;
        }

        public int GetCost() => _cost;

        public bool Validate() => TurnManager.Instance != null && TurnManager.Instance.CanScheduleAction(_cost);

        public void Execute()
        {
            // 结算时正式扣费并播报
            TurnManager.Instance.AdvanceTime(_cost);
            Debug.Log($"<color=orange>[结算] 执行动作：【{_actionName}】，结算消耗了 {_cost} TU。</color>");
        }
    }
}