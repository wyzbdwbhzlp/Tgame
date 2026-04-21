using System.Collections.Generic;
using UnityEngine;

namespace TGame.Battle
{
    public interface ICommand
    {
        bool Validate();
        void Execute();
        int GetCost();
        int GetUnitID(); // 必须绑定执行者ID
    }

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
        public int GetUnitID() => _unitID;

        public bool Validate() => TurnManager.Instance != null && TurnManager.Instance.CanScheduleAction(_unitID, _stepCost);

        public void Execute()
        {
            var unit = UnitManager.Instance.GetUnit(_unitID);
            var view = UnitViewManager.Instance.GetView(_unitID);

            if (unit != null)
            {
                List<GridCell> path = PathfindingService.GetPath(GridSystem.Instance, _start, _target);

                GridSystem.Instance.GetCell(_start).OccupantUnitID = -1;
                unit.GridPosition = _target;
                GridSystem.Instance.GetCell(_target).OccupantUnitID = _unitID;

                TurnManager.Instance.AdvanceTime(_unitID, _stepCost);

                if (view != null && path != null && path.Count > 0) view.MoveAlongPath(path);

                Debug.Log($"<color=cyan>[结算] {_unitID} 移动消耗 {_stepCost} TU。</color>");
            }
        }
    }

    public class MockActionCommand : ICommand
    {
        private int _unitID;
        private string _actionName;
        private int _cost;

        public MockActionCommand(int unitID, string name, int cost)
        {
            _unitID = unitID;
            _actionName = name;
            _cost = cost;
        }

        public int GetCost() => _cost;
        public int GetUnitID() => _unitID;

        public bool Validate() => TurnManager.Instance != null && TurnManager.Instance.CanScheduleAction(_unitID, _cost);

        public void Execute()
        {
            TurnManager.Instance.AdvanceTime(_unitID, _cost);
            var unit = UnitManager.Instance.GetUnit(_unitID);
            string uName = unit != null ? unit.ConfigData.characterName : _unitID.ToString();
            Debug.Log($"<color=orange>[结算] 角色 {uName} 执行【{_actionName}】，消耗 {_cost} TU。</color>");
        }
    }
}