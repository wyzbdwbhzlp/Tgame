using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TGame.Data;
using TGame.Core;

namespace TGame.Battle
{
    public interface ICommand
    {
        bool Validate();
        void Execute();
        void Undo();
        // 【🔥核心升级】把瞬间结算，改成协程结算！
        IEnumerator SettleRoutine();
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
        private LineRenderer _myPathLine;

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
                GridSystem.Instance.GetCell(_start).OccupantUnitID = -1;
                unit.SetGridPosition(_target);
                GridSystem.Instance.GetCell(_target).OccupantUnitID = _unitID;

                if (HexMapView.Instance != null)
                {
                    HexMapView.Instance.UpdatePhantom(_unitID, _target);
                    _myPathLine = HexMapView.Instance.CreatePathLineSegment(_unitID, _start, _path);
                }
            }
        }

        public void Undo()
        {
            var unit = UnitManager.Instance.GetUnit(_unitID);
            if (unit != null)
            {
                GridSystem.Instance.GetCell(_target).OccupantUnitID = -1;
                unit.SetGridPosition(_start);
                GridSystem.Instance.GetCell(_start).OccupantUnitID = _unitID;

                if (HexMapView.Instance != null)
                {
                    if (_myPathLine != null) UnityEngine.Object.Destroy(_myPathLine.gameObject);

                    var view = UnitViewManager.Instance.GetView(_unitID);
                    if (view != null)
                    {
                        Vector3Int physicalPos = GridSystem.Instance.WorldToCell(view.transform.position);
                        if (unit.GridPosition == physicalPos) HexMapView.Instance.ClearPhantom(_unitID);
                        else HexMapView.Instance.UpdatePhantom(_unitID, _start);
                    }
                }
            }
        }

        public IEnumerator SettleRoutine()
        {
            if (CameraController.Instance != null) CameraController.Instance.FocusOnExecution(_unitID);

            var view = UnitViewManager.Instance.GetView(_unitID);
            if (view != null && _path != null && _path.Count > 0)
            {
                view.MoveAlongPath(_path);

                // 【🔥完美同步】每走一格是0.25秒，我们就在这里挂起等待这么多秒！
                float moveDuration = _path.Count * 0.25f;
                yield return new WaitForSeconds(moveDuration);
            }

            TurnManager.Instance.AdvanceTime(_unitID, _stepCost);
            if (_myPathLine != null) UnityEngine.Object.Destroy(_myPathLine.gameObject);
            if (HexMapView.Instance != null) HexMapView.Instance.ClearPhantom(_unitID);

            // 【🔥动作缓冲】移动完成后，留 0.2 秒给玩家看清楚，再换下一个人
            yield return new WaitForSeconds(0.2f);
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
        public void Execute() { }
        public void Undo() { }

        public IEnumerator SettleRoutine()
        {
            if (CameraController.Instance != null) CameraController.Instance.FocusOnExecution(_unitID);

            TurnManager.Instance.AdvanceTime(_unitID, _cost);
            UnitView casterView = UnitViewManager.Instance.GetView(_unitID);
            if (casterView != null) casterView.PlaySkillAnimation(casterView.transform.position);

            // 假动作等待时间
            yield return new WaitForSeconds(1.0f);
            yield return new WaitForSeconds(0.3f);
        }
    }

    public class WaitCommand : ICommand
    {
        private int _unitID;
        private int _cost = 2;

        public WaitCommand(int unitID) { _unitID = unitID; }
        public int GetCost() => _cost;
        public int GetUnitID() => _unitID;
        public bool Validate() => TurnManager.Instance != null && TurnManager.Instance.CanScheduleAction(_unitID, _cost);
        public void Execute() { }
        public void Undo() { }

        public IEnumerator SettleRoutine()
        {
            if (CameraController.Instance != null) CameraController.Instance.FocusOnExecution(_unitID);
            TurnManager.Instance.AdvanceTime(_unitID, _cost);

            // 发呆指令给个短暂停顿，防止瞬间跳过让人摸不着头脑
            yield return new WaitForSeconds(0.5f);
        }
    }
}