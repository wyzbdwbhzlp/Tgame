using UnityEngine;
using TGame.Data;
using TGame.Battle;

namespace TGame.Battle
{
    public class RuntimeUnit
    {
        public int InstanceID { get; private set; }
        public int Side { get; private set; } // 1001 是玩家，其他是敌人

        // 【🔥核心修改】这里把 CharacterDataSO 改成了 CharacterData
        public CharacterData ConfigData { get; private set; }

        public Vector3Int GridPosition { get; private set; }

        public int CurrentHP { get; private set; }
        public int CurrentMP { get; private set; }

        // 【🔥核心修改】构造函数里的传参也改成了 CharacterData
        public RuntimeUnit(int instanceID, int side, CharacterData configData, Vector3Int spawnPos)
        {
            this.InstanceID = instanceID;
            this.Side = side;
            this.ConfigData = configData;
            this.GridPosition = spawnPos;

            // 初始化属性
            this.CurrentHP = configData.maxHP;
            this.CurrentMP = configData.maxMP;
        }

        public void SetGridPosition(Vector3Int newPos)
        {
            GridPosition = newPos;
        }

        // ... 如果你的文件下面还有受击、扣血等其他方法，请保留它们，
        // 只要确保上面这两处把 SO 后缀去掉就行了！
    }
}