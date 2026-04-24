using System.Collections.Generic;
using UnityEngine;
using TGame.Battle;

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

        GameObject obj = null;

        // ==========================================
        // 【🔥核心修改】直接实例化强引用的预制体
        // ==========================================
        if (logicUnit.ConfigData.characterPrefab != null)
        {
            obj = Instantiate(logicUnit.ConfigData.characterPrefab);
            obj.name = $"[View] {logicUnit.ConfigData.characterName}";
        }
        else
        {
            Debug.LogWarning($"[UnitViewManager] 角色 {logicUnit.ConfigData.characterName} 没有配置预制体！使用保底方块。");
            obj = CreateFallbackObject(logicUnit);
        }

        // 获取或自动挂载 UnitView 控制器
        UnitView view = obj.GetComponent<UnitView>();
        if (view == null) view = obj.AddComponent<UnitView>();

        view.Init(logicUnit);
        _viewDict.Add(logicUnit.InstanceID, view);
    }

    private GameObject CreateFallbackObject(RuntimeUnit logicUnit)
    {
        GameObject obj = new GameObject($"[View] {logicUnit.ConfigData.characterName}_Fallback");
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(64, 64);
        for (int i = 0; i < 4096; i++) tex.SetPixel(i % 64, i / 64, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        sr.sortingOrder = 10;
        sr.color = (logicUnit.ConfigData.characterID == 1001) ? Color.cyan : Color.red;
        obj.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
        return obj;
    }
}