using System.Collections.Generic;
using UnityEngine;

public class UnitViewManager : MonoBehaviour
{
    // ==========================================
    // 新增：单例模式与开放获取方法
    // ==========================================
    public static UnitViewManager Instance { get; private set; }

    private Dictionary<int, UnitView> _viewDict = new Dictionary<int, UnitView>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (UnitManager.Instance == null) return;

        foreach (var logicUnit in UnitManager.Instance.GetAllUnits())
            CreateUnitModel(logicUnit);

        UnitManager.Instance.OnUnitSpawned += CreateUnitModel;
    }

    private void OnDestroy()
    {
        if (UnitManager.Instance != null)
            UnitManager.Instance.OnUnitSpawned -= CreateUnitModel;

        if (Instance == this) Instance = null;
    }

    // 新增：让外部可以通过逻辑ID拿到对应的表现层组件
    public UnitView GetView(int instanceID)
    {
        _viewDict.TryGetValue(instanceID, out UnitView view);
        return view;
    }

    private void CreateUnitModel(RuntimeUnit logicUnit)
    {
        if (_viewDict.ContainsKey(logicUnit.InstanceID)) return;

        GameObject modelObj = new GameObject($"[Entity_2D] {logicUnit.ConfigData.characterName}_{logicUnit.InstanceID}");
        SpriteRenderer sr = modelObj.AddComponent<SpriteRenderer>();

        Texture2D tex = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();

        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        sr.sortingOrder = 10;

        if (logicUnit.ConfigData.characterID == 1001)
            sr.color = Color.cyan;
        else
            sr.color = Color.red;

        modelObj.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        UnitView viewScript = modelObj.AddComponent<UnitView>();
        viewScript.Init(logicUnit);

        _viewDict.Add(logicUnit.InstanceID, viewScript);
    }
}