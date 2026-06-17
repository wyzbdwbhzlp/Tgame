using UnityEngine;
using TGame.Data;
using TGame.Core; // 引入 Core 以访问 UnitViewManager

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

        // ==========================================
        // 【🔥新增】躯干与异常状态系统
        // ==========================================
        public int CurrentPosture { get; private set; }
        public int CurrentState { get; private set; } = 0; // 0:Normal, 1:Stagger, 2:KnockUp, 3:Stun

        public RuntimeUnit(int instanceID, int side, CharacterData configData, EnemyData enemyData, Vector3Int spawnPos)
        {
            this.InstanceID = instanceID;
            this.Side = side;
            this.ConfigData = configData;
            this.EnemyConfig = enemyData;
            this.GridPosition = spawnPos;

            // 智能区分敌我数据获取
            this.CurrentHP = GetMaxHP();
            this.CurrentMP = Side == 1001 ? configData.maxMP : 0;

            // 初始化躯干
            this.CurrentPosture = GetMaxPosture();
            this.CurrentState = 0;
        }

        // ==========================================
        // 智能数据分发 (防止配置表报空指针)
        // ==========================================
        public int GetMaxHP() => Side == 1001 ? ConfigData.maxHP : EnemyConfig.maxHP;
        public int GetMaxPosture() => Side == 1001 ? ConfigData.postureValue : EnemyConfig.postureValue;

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
            int max = GetMaxHP();
            if (CurrentHP > max) CurrentHP = max;
        }

        public void ConsumeMP(int amount)
        {
            CurrentMP -= amount;
            if (CurrentMP < 0) CurrentMP = 0;
        }

        // ==========================================
        // 【🔥核心集成】接收大部头公式伤害结果并扣除
        // ==========================================
        public void ExecuteCombatDamage(DamageResult result)
        {
            if (result.isMiss) return;

            CurrentPosture = Mathf.Max(0, CurrentPosture - result.finalPostureDamage);
            CurrentHP = Mathf.Max(0, CurrentHP - result.finalHPDamage);
            CurrentState = (int)result.nextState;

            // 如果生命归零，触发死亡动画
            if (CurrentHP <= 0)
            {
                UnitView view = UnitViewManager.Instance.GetView(InstanceID);
                if (view != null)
                {
                    var anim = view.GetComponentInChildren<UnitAnimator>();
                    if (anim != null) anim.PlayDie();
                }
            }
        }

        // ==========================================
        // 【🔥核心集成】回合开始时的状态与躯干恢复机制
        // ==========================================
        public void OnRoundTurnStart()
        {
            if (CurrentHP <= 0) return;

            int maxPosture = GetMaxPosture();

            if (CurrentState == 0) // 正常状态
            {
                int restoreVal = Mathf.RoundToInt(maxPosture * 0.5f);
                CurrentPosture = Mathf.Min(maxPosture, CurrentPosture + restoreVal);
            }
            else
            {
                // 特殊状态在自身回合开始后，直到回合结束才会退出
                // 退出后重置满躯干值并恢复正常状态
                CurrentState = 0;
                CurrentPosture = maxPosture;
            }
        }
    }
}