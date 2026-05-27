using UnityEngine;
using TGame.Data;
using TGame.Battle;
using Unity.VisualScripting.FullSerializer;

namespace TGame.Core
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        private VFXTable _vfxTable;

        private void Awake()
        {
            Instance = this;
            _vfxTable = Resources.Load<VFXTable>("DataConfigs/VFXTable");

            if (_vfxTable == null)
            {
                Debug.LogError("[VFXManager] 找不到特效总表！请确保路径为 Assets/Resources/DataConfigs/VFXTable.asset");
            }
            else
            {
                Debug.Log($"<color=cyan>[VFXManager] 特效总表加载成功，共注册 {_vfxTable.entries.Count} 个特效。</color>");
            }
        }

        public void PlayVFX(string vfxID, Transform target)
        {
            if (_vfxTable == null) return;
            VFXDataSO data = _vfxTable.GetVFX(vfxID);
            if (data == null)
            {
                Debug.LogWarning($"[VFXManager] 播放失败：找不到 ID 为【{vfxID}】的特效！");
                return;
            }
            PlayVFX(data, target);
        }

        public void PlayVFX(VFXDataSO vfxData, Transform target)
        {
            if (vfxData == null || target == null) return;

            GameObject vfxObj = new GameObject($"[VFX] {vfxData.name}");
            vfxObj.transform.SetParent(target);
            vfxObj.transform.localPosition = vfxData.offset;

            VFXPlayer player = vfxObj.AddComponent<VFXPlayer>();
            player.Play(vfxData);
        }

        // ==========================================
        // 【🔥新增】在指定的世界坐标播放特效 (专门用于AOE或对地技能)
        // ==========================================
        public void PlayVFXAtPosition(string vfxID, Vector3 worldPosition)
        {
            if (_vfxTable == null) return;
            VFXDataSO data = _vfxTable.GetVFX(vfxID);
            if (data == null)
            {
                Debug.LogWarning($"[VFXManager] 播放失败：找不到 ID 为【{vfxID}】的特效！");
                return;
            }

            GameObject vfxObj = new GameObject($"[VFX] {data.name}");
            // 直接设置坐标，并稍微往 Z 轴拉近一点，防止被地板遮挡！
            vfxObj.transform.position = worldPosition + data.offset + new Vector3(0, 0, -1f);

            VFXPlayer player = vfxObj.AddComponent<VFXPlayer>();

            // ==========================================
            // 【🔥核心修复】变量名修正为 data
            // ==========================================
            player.Play(data);
        }
    }
}