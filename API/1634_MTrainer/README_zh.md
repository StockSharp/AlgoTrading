# MTrainer策略
[Русский](README_ru.md) | [English](README.md)

MTrainer策略复刻MT4的MTrainer脚本。当价格达到预设的进场线时开仓，并通过止损、止盈和可选的部分平仓线进行管理。该策略用于在可视化测试器中进行手动练习。

## 详情

- **入场条件**：价格突破进场线
- **多空方向**：双向
- **出场条件**：止损、止盈或部分平仓
- **止损**：是
- **默认值**：
  - `EntryPrice` = 0
  - `TakeProfitPrice` = 0
  - `StopLossPrice` = 0
  - `PartialClosePercent` = 0
  - `PartialClosePrice` = 0
  - `Volume` = 1
- **过滤器**：
  - 类别: 工具
  - 方向: 双向
  - 指标: 无
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 任意
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 低
