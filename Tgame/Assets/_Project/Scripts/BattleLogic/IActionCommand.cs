using System.Collections.Generic;

// 动作命令接口
public interface IActionCommand
{
    bool Validate();
    void Execute();
    List<TimelineEvent> GenerateEvents();
}