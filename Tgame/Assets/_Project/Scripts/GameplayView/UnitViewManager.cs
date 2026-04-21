using System.Collections.Generic;
using UnityEngine;

public class UnitViewManager : MonoBehaviour
{
    public static UnitViewManager Instance { get; private set; }
    private Dictionary<int, UnitView> _viewDict = new Dictionary<int, UnitView>();

    private void Awake() { Instance = this; }

    private void Start()
    {
        if (UnitManager.Instance == null) return;
        UnitManager.Instance.OnUnitSpawned -= CreateUnitModel;
        UnitManager.Instance.OnUnitSpawned += CreateUnitModel;
        foreach (var logicUnit in UnitManager.Instance.GetAllUnits()) CreateUnitModel(logicUnit);
    }

    public UnitView GetView(int id) => _viewDict.TryGetValue(id, out var view) ? view : null;

    private void CreateUnitModel(RuntimeUnit logicUnit)
    {
        if (_viewDict.ContainsKey(logicUnit.InstanceID)) return;

        // 1. 仅创建角色身体方块（未来这里会替换成加载你做好的角色 Spine 动画或 3D 模型 Prefab）
        GameObject obj = new GameObject($"[View] {logicUnit.ConfigData.characterName}");
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(64, 64);
        for (int i = 0; i < 4096; i++) tex.SetPixel(i % 64, i / 64, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        sr.sortingOrder = 10;

        // 我方青色，敌方红色
        sr.color = (logicUnit.ConfigData.characterID == 1001) ? Color.cyan : Color.red;
        obj.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

        // 2. 挂载控制器 (头顶不再塞乱七八糟的 UI 节点了)
        UnitView view = obj.AddComponent<UnitView>();
        view.Init(logicUnit);

        _viewDict.Add(logicUnit.InstanceID, view);
    }
}