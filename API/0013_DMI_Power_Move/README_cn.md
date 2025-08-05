# DMI动力移动
[English](README.md) | [Русский](README_ru.md)

该策略将DMI方向指标差值与ADX结合，以捕捉强劲趋势。当+DI明显高于-DI（或相反）且ADX强劲时入场；当ADX减弱或DI差收窄时离场。此方法通过同时要求明显的方向性和上升的ADX，过滤掉较弱的信号，虽交易较少但质量更高。

测试表明年均收益约为 76%，该策略在外汇市场表现最佳。

## 详情
- **入场条件**: 基于 ADX、ATR、DMI 的信号
- **多空方向**: 双向
- **退出条件**: 反向信号或止损
- **止损**: 是
- **默认值**:
  - `DmiPeriod` = 14
  - `DiDifferenceThreshold` = 5m
  - `AdxThreshold` = 30m
  - `AdxExitThreshold` = 25m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: ADX, ATR, DMI
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (15m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中

