# Enhanced Bollinger Bands SL TP 策略
[English](README.md) | [Русский](README_ru.md)

该策略在价格穿越布林带时使用限价单入场，并设置以点数计算的固定止损和止盈。

## 细节

- **入场条件**：
  - 多头：前一收盘价 <= 前一条下轨 且 当前收盘价 > 下轨
  - 空头：前一收盘价 >= 前一条上轨 且 当前收盘价 < 上轨
- **方向**：多空双向
- **止损**：以点数表示的绝对止损和止盈
- **默认值**：
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 2m
  - `EnableLong` = true
  - `EnableShort` = true
  - `PipValue` = 0.0001m
  - `StopLossPips` = 10m
  - `TakeProfitPips` = 20m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **筛选**：
  - 类型：反转
  - 方向：双向
  - 指标：Bollinger Bands
  - 止损：有
  - 复杂度：基础
  - 时间框架：短期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
