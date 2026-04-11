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

        // 重新订阅，防止之前的订阅失效
        UnitManager.Instance.OnUnitSpawned -= CreateUnitModel;
        UnitManager.Instance.OnUnitSpawned += CreateUnitModel;

        // 强制同步现有单位
        foreach (var logicUnit in UnitManager.Instance.GetAllUnits())
            CreateUnitModel(logicUnit);
    }

    public UnitView GetView(int id)
    {
        if (_viewDict.TryGetValue(id, out var view)) return view;
        return null;
    }

    private void CreateUnitModel(RuntimeUnit logicUnit)
    {
        if (_viewDict.ContainsKey(logicUnit.InstanceID)) return;

        GameObject obj = new GameObject($"[View] {logicUnit.ConfigData.characterName}");
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();

        Texture2D tex = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();

        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        sr.sortingOrder = 10;
        sr.color = (logicUnit.ConfigData.characterID == 1001) ? Color.cyan : Color.red;

        obj.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
        UnitView view = obj.AddComponent<UnitView>();
        view.Init(logicUnit);

        _viewDict.Add(logicUnit.InstanceID, view);
    }
}