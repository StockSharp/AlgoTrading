# Liquidex 策略
[English](README.md) | [Русский](README_ru.md)

当价格突破 Keltner 通道边界时入场，并使用止损、止盈、保本与跟踪止损管理风险的突破策略。

## 详情

- **入场条件**：
  - 多头：收盘价突破上轨。
  - 空头：收盘价跌破下轨。
- **多/空**：双向。
- **出场条件**：
  - 触发止损或止盈。
  - 达到目标后将止损移至保本。
  - 跟踪止损触发。
- **止损**：有。
- **默认值**：
  - `KcPeriod` = 10
  - `UseKcFilter` = true
  - `StopLoss` = 30
  - `TakeProfit` = 0
  - `MoveToBe` = 15
  - `MoveToBeOffset` = 2
  - `TrailingDistance` = 5
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **过滤器**：
  - 分类：Channel
  - 方向：双向
  - 指标：Keltner
  - 止损：有
  - 复杂度：基础
  - 周期：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
