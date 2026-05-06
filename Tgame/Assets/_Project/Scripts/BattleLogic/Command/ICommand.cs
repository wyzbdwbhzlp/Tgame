using System.Collections.Generic;
using UnityEngine;

namespace TGame.Battle
{
    // 【🔥核心修复】这就是接口定义，必须在这里声明 Settle()，其他地方才能调用
    public interface ICommand
    {
        bool Validate();
        void Execute(); // 规划阶段：虚影和逻辑提前位移
        void Undo();    // 撤销阶段：取消虚影，退还资源
        void Settle();  // 真实结算阶段：播放真实动画，产生实际伤害
        int GetCost();
        int GetUnitID();
    }

    public class MoveCommand : ICommand
    {
        private int _unitID;
        private Vector3Int _start;
        private Vector3Int _target;
        private int _stepCost;
        private List<GridCell> _path;

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
            if (unit != null)
            {
                _path = PathfindingService.GetPath(GridSystem.Instance, _start, _target);

                // 逻辑层提前位移
                GridSystem.Instance.GetCell(_start).OccupantUnitID = -1;
                unit.SetGridPosition(_target);
                GridSystem.Instance.GetCell(_target).OccupantUnitID = _unitID;

                // 表现层生成虚影
                if (HexMapView.Instance != null)
                {
                    HexMapView.Instance.ShowPhantom(_unitID, _target, _path);
                }
                Debug.Log($"<color=cyan>[规划] {_unitID} 虚影已移动到 {_target}，消耗 {_stepCost} TU。</color>");
            }
        }

        public void Undo()
        {
            var unit = UnitManager.Instance.GetUnit(_unitID);
            if (unit != null)
            {
                // 逻辑层回退到起点
                GridSystem.Instance.GetCell(_target).OccupantUnitID = -1;
                unit.SetGridPosition(_start);
                GridSystem.Instance.GetCell(_start).OccupantUnitID = _unitID;

                // 表现层清除虚影
                if (HexMapView.Instance != null)
                {
                    HexMapView.Instance.ClearPhantom();
                }
                Debug.Log($"<color=yellow>[撤销] {_unitID} 取消了移动规划，退回 {_start}。</color>");
            }
        }

        public void Settle()
        {
            var view = UnitViewManager.Instance.GetView(_unitID);

            // 真实结算时才让模型在场景里跑动
            if (view != null && _path != null && _path.Count > 0) view.MoveAlongPath(_path);

            // 推进真实的 TU
            TurnManager.Instance.AdvanceTime(_unitID, _stepCost);

            // 结算后清理虚影
            if (HexMapView.Instance != null) HexMapView.Instance.ClearPhantom();

            Debug.Log($"<color=cyan>[结算] {_unitID} 真实移动完成。</color>");
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
            Debug.Log($"<color=orange>[规划] 角色 {_unitID} 计划执行【{_actionName}】。</color>");
        }

        public void Undo()
        {
            Debug.Log($"<color=yellow>[撤销] 角色 {_unitID} 取消了动作【{_actionName}】。</color>");
        }

        public void Settle()
        {
            TurnManager.Instance.AdvanceTime(_unitID, _cost);
            var unit = UnitManager.Instance.GetUnit(_unitID);
            string uName = unit != null ? unit.ConfigData.characterName : _unitID.ToString();
            Debug.Log($"<color=orange>[结算] 角色 {uName} 真实执行【{_actionName}】，消耗 {_cost} TU。</color>");
        }
    }
}