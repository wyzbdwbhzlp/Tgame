using UnityEngine;
using TGame.Data;

namespace TGame.Battle
{
    public class RuntimeUnit
    {
        public int InstanceID { get; private set; }
        public int Side { get; private set; } // 1001 是玩家，2001是敌人

        public CharacterData ConfigData { get; private set; }
        public EnemyData EnemyConfig { get; private set; }

        public Vector3Int GridPosition { get; private set; }

        public int CurrentHP { get; private set; }
        public int CurrentMP { get; private set; }

        public RuntimeUnit(int instanceID, int side, CharacterData configData, EnemyData enemyData, Vector3Int spawnPos)
        {
            this.InstanceID = instanceID;
            this.Side = side;
            this.ConfigData = configData;
            this.EnemyConfig = enemyData;
            this.GridPosition = spawnPos;

            this.CurrentHP = configData.maxHP;
            this.CurrentMP = configData.maxMP;
        }

        public void SetGridPosition(Vector3Int newPos)
        {
            GridPosition = newPos;
        }

        public void TakeDamage(int damage)
        {
            CurrentHP -= damage;
            if (CurrentHP < 0) CurrentHP = 0;
        }

        public void Heal(int amount)
        {
            CurrentHP += amount;
            if (CurrentHP > ConfigData.maxHP) CurrentHP = ConfigData.maxHP;
        }

        // ==========================================
        // 【🔥核心修复】新增合理合法的扣蓝方法
        // ==========================================
        public void ConsumeMP(int amount)
        {
            CurrentMP -= amount;
            if (CurrentMP < 0) CurrentMP = 0;
        }
    }
}