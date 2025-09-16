# Candels High Open Strategy
[English](README.md) | [Русский](README_ru.md)

该策略在蜡烛开盘价等于其最高价或最低价时进行交易。
当开盘价等于最低价时，预期价格上行，开多仓。
当开盘价等于最高价时，预期价格下行，开空仓。
当价格穿越抛物线SAR指标时平仓，该指标作为移动退出。

## 细节

- **入场条件**:
  - 多头: `Open == Low`
  - 空头: `Open == High`
- **多/空**: 双向
- **出场条件**: 价格穿越 Parabolic SAR 或出现反向信号
- **止损/止盈**: 使用固定止损和止盈
- **默认值**:
  - `StopLevel` = 50m
  - `TakeLevel` = 50m
  - `SarStep` = 0.02m
  - `SarMax` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `ReverseSignals` = false
- **筛选条件**:
  - 分类: 价格行为
  - 方向: 双向
  - 指标: Parabolic SAR
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
