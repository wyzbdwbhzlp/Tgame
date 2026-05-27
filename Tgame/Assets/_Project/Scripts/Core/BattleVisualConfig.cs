using UnityEngine;

namespace TGame.Battle
{
    public class BattleVisualConfig : MonoBehaviour
    {
        public static BattleVisualConfig Instance { get; private set; }

        [Header("表现层手感微调 (Game Feel)")]
        [Tooltip("所有角色攻击或技能播放完毕后，统一的定格时间（秒）")]
        [Range(0f, 2f)]
        public float globalActionHoldDuration = 0.3f; // 默认 0.3 秒

        // 以后你可以在这里继续加：
        // public float cameraShakeDuration = 0.2f;
        // public float damageTextFlySpeed = 2f;

        private void Awake()
        {
            Instance = this;
        }
    }
}