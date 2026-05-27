using UnityEngine;
using TGame.Battle;

namespace TGame.Core
{
    public class DamagePopupManager : MonoBehaviour
    {
        public static DamagePopupManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public void CreatePopup(Vector3 position, int damageAmount, bool isCrit)
        {
            // 动态从 Resources 加载预制体（免去拖拽烦恼）
            GameObject prefab = Resources.Load<GameObject>("DamagePopup");
            if (prefab == null)
            {
                Debug.LogWarning("[DamagePopupManager] 找不到预制体！请确保 Assets/Resources/ 下存在名为 DamagePopup 的预制体。");
                return;
            }

            // 稍微把出生点抬高一点，通常在角色头顶
            Vector3 spawnPos = position + new Vector3(0, 1.2f, 0);
            GameObject popupObj = Instantiate(prefab, spawnPos, Quaternion.identity);

            DamagePopup popup = popupObj.GetComponent<DamagePopup>();
            if (popup != null)
            {
                popup.Setup(damageAmount, isCrit);
            }
        }
    }
}