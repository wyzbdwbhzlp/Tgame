using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TGame.Battle
{
    public class EnemyAIController
    {
        public ICommand DecideNextAction(RuntimeUnit enemy)
        {
            if (enemy == null || enemy.CurrentHP <= 0) return null;

            // 1. 获取所有存活的玩家
            var players = UnitManager.Instance.GetAllUnits()
                .Where(u => u.Side == 1001 && u.CurrentHP > 0)
                .ToList();

            if (players.Count == 0) return new WaitCommand(enemy.InstanceID);

            // 2. 寻找距离最近的玩家目标
            RuntimeUnit closestPlayer = null;
            int minDistance = int.MaxValue;

            foreach (var player in players)
            {
                int dist = GetHexDistance(enemy.GridPosition, player.GridPosition);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestPlayer = player;
                }
            }

            // 3. 策略 A：如果最近的目标已经进入了我的普攻范围，直接原地开撕！
            if (minDistance <= enemy.ConfigData.attackRange)
            {
                return new AttackCommand(enemy.InstanceID, closestPlayer.InstanceID);
            }

            // 4. 策略 B：如果够不着，尝试向他移动
            // 【🔥修复1】临时“假装”玩家不在那一格，让寻路算法能够通车
            var playerCell = GridSystem.Instance.GetCell(closestPlayer.GridPosition);
            int originalOccupant = playerCell.OccupantUnitID;
            playerCell.OccupantUnitID = -1;

            // 再次寻路
            var path = PathfindingService.GetPath(GridSystem.Instance, enemy.GridPosition, closestPlayer.GridPosition);

            // 【🔥修复1】还原玩家的占位（极其重要，不还原玩家就变成空气了）
            playerCell.OccupantUnitID = originalOccupant;

            if (path != null && path.Count > 0)
            {
                // ==========================================
                // 读取你加在表里的【最大移动距离】进行截断
                // ==========================================
                int maxMove = enemy.EnemyConfig != null ? enemy.EnemyConfig.maxMoveDistance : enemy.ConfigData.speed;

                // 寻路路径里，[0]是离怪最近的一步，[Count-1]是目标玩家脚底。
                // 我们能走的最大格子索引，不能超过“最大步数-1”，也不能超过总路径(防止踩到玩家脸上穿模)。
                int targetIndex = Mathf.Min(path.Count - 1, maxMove - 1);

                // 从我们极限能走到的最远那一格，开始“倒序”寻找没人踩的空地
                for (int i = targetIndex; i >= 0; i--)
                {
                    if (path[i].OccupantUnitID == -1) // 这一格安全，没人占
                    {
                        // 完美，生成移动指令，怪就会精准挪过去！
                        return new MoveCommand(enemy.InstanceID, enemy.GridPosition, path[i].Position);
                    }
                }
            }

            // 5. 策略 C：既打不到，周围又被堵死了走不动，直接待命不卡死进程
            return new WaitCommand(enemy.InstanceID);
        }

        private int GetHexDistance(Vector3Int a, Vector3Int b)
        {
            return (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z)) / 2;
        }
    }
}