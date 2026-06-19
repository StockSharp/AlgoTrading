# EMA RSI 趋势反转策略
[English](README.md) | [Русский](README_ru.md)

当快速 EMA 上穿慢速 EMA 且 RSI 高于阈值时开多，快速 EMA 下穿慢速 EMA 且 RSI 低于阈值时平仓。使用百分比止盈和止损。

## 详情

- **入场条件**：
  - 多头：`FastEMA 上穿 SlowEMA 且 RSI > RsiLevel`
- **多空方向**：仅多头
- **止损**：百分比止盈和止损
- **默认值**：
  - `FastLength` = 9
  - `SlowLength` = 21
  - `RsiLength` = 14
  - `RsiLevel` = 50m
  - `TakeProfitPercent` = 2m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **过滤**：
  - 类别：趋势
  - 方向：多头
  - 指标：EMA, RSI
  - 止损：是
  - 复杂度：初级
  - 时间框架：中期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
