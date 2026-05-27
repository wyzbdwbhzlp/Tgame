using System;
using System.Collections.Generic;
using UnityEngine;

namespace TGame.Data
{
    [Serializable]
    public class VFXEntry
    {
        public string vfxID;     // 特效的字符串ID（例如 "Hit", "Explosion"）
        public VFXDataSO vfxData; // 对应的特效资产
    }

    [CreateAssetMenu(fileName = "VFXTable", menuName = "TGame/特效总表 (VFX Table)")]
    public class VFXTable : ScriptableObject
    {
        [Header("在此处注册游戏内的所有特效")]
        public List<VFXEntry> entries = new List<VFXEntry>();

        /// <summary>
        /// 通过字符串 ID 获取特效资产
        /// </summary>
        public VFXDataSO GetVFX(string id)
        {
            var entry = entries.Find(e => e.vfxID == id);
            return entry != null ? entry.vfxData : null;
        }
    }
}