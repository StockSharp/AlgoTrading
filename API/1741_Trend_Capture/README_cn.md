# 趋势捕捉
[English](README.md) | [Русский](README_ru.md)

结合 Parabolic SAR 和 ADX 过滤的趋势跟随策略。当价格收于 SAR 上方且 ADX 低于阈值时开多单，表明新趋势可能开始；反之则开空单。

## 详细信息

- **入场条件**：价格在 Parabolic SAR 上/下且 ADX 低于 `AdxLevel`。
- **多/空**：双向。
- **出场条件**：止损、止盈或反向信号。
- **止损类型**：固定止损、止盈并带有保本机制。
- **默认值**：
  - `SarStep` = 0.02
  - `SarMax` = 0.2
  - `AdxPeriod` = 14
  - `AdxLevel` = 20
  - `StopLoss` = 1800 点
  - `TakeProfit` = 500 点
  - `BreakEven` = 50 点
  - `CandleType` = TimeSpan.FromMinutes(1)
- **筛选**：
  - 分类：趋势
  - 方向：双向
  - 指标：Parabolic SAR, ADX
  - 止损：是
  - 复杂度：基础
  - 时间框架：日内 (1m)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
