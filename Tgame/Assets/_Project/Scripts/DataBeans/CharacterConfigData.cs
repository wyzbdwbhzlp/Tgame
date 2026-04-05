using System;

// 这是模拟你的导表工具自动生成的 C# 数据类
[Serializable]
public class CharacterConfigData : IConfigData
{
    public int Id;
    public string Name;
    public int Hp;
    public int Attack;
    public int Defense;

    // 实现 IConfigData 接口，返回主键 ID 供 DataManager 建立字典索引
    public int GetId()
    {
        return Id;
    }
}