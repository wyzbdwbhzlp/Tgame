using UnityEngine;

// 纯 C# 的时间轴事件数据结构，完全脱离 MonoBehaviour
[System.Serializable]
public class TimelineEvent
{
    public int unitID;              // 角色 ID
    public ActionType actionType;   // 动作类型
    public float startTime;         // 开始时素
    public float duration;          // 持续时素
    public int targetID;            // 目标 ID (-1表示无目标)
    public Vector3Int targetGrid;   // 目标地块
    public int configID;            // 配置 ID
    public bool isComposite;        // 是否为复合动作
}