using UnityEngine;

namespace TGame.Data
{
    [CreateAssetMenu(fileName = "MapVisualConfig", menuName = "TGame/地图美术配置表 (Map Visual Config)")]
    public class MapVisualConfigSO : ScriptableObject
    {
        [Header("地面贴图 (顺序需与画笔菜单一致)")]
        public Sprite[] groundSprites;

        [Header("障碍物贴图 (顺序需与画笔菜单一致)")]
        public Sprite[] obstacleSprites;
    }
}