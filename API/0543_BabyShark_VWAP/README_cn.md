# BabyShark VWAP 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合了成交量加权平均价 (VWAP) 通道和基于 OBV 的 RSI 过滤。价格跌破下方偏差带并且 RSI 显示超卖时做多；价格升破上方偏差带且 RSI 表现为超买时做空。

采用小幅百分比止损，并在再次入场前等待冷却期。

## 详情

- **入场条件**：价格穿越偏差带并得到 RSI 确认。
- **多空方向**：双向。
- **出场条件**：回到 VWAP 或触发止损。
- **止损**：有。
- **默认参数**：
  - `Length` = 60
  - `RsiLength` = 5
  - `HigherLevel` = 70
  - `LowerLevel` = 30
  - `Cooldown` = 10
  - `StopLossPercent` = 0.6m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 类别：均值回归
  - 方向：双向
  - 指标：VWAP、RSI、OBV
  - 止损：有
  - 复杂度：中等
  - 周期：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
