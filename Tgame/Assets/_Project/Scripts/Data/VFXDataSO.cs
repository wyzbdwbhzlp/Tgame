using UnityEngine;

namespace TGame.Data
{
    [CreateAssetMenu(fileName = "NewVFXData", menuName = "TGame/特效数据 (VFX Data)")]
    public class VFXDataSO : ScriptableObject
    {
        [Header("序列帧图集 (按顺序全选拖入)")]
        public Sprite[] frames;

        [Header("播放设置")]
        public float frameRate = 15f; // 每秒播放多少帧
        public bool loop = false;     // 是否循环（通常受击特效不循环）

        [Header("位置与缩放")]
        public Vector3 offset = new Vector3(0, 0.5f, 0); // 默认偏移到角色胸口
        public float scale = 1f;
    }
}