# CVD 背离
[English](README.md) | [Русский](README_ru.md)

该策略将累积成交量差（CVD）背离与Hull移动平均、RSI、MACD以及成交量过滤结合。当趋势、动量和成交量一致且CVD出现背离或延续时开仓。出现相反信号或指标交叉时平仓。

## 细节

- **入场条件**：HMA趋势一致，RSI和MACD确认，高成交量并且CVD背离/延续。
- **多空方向**：双向。
- **出场条件**：相反信号或指标交叉。
- **止损**：无显式止损。
- **默认值**：
  - `HmaFastLength` = 20
  - `HmaSlowLength` = 50
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumeMaLength` = 20
  - `VolumeMultiplier` = 1.5m
  - `CvdLength` = 14
  - `DivergenceLookback` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **筛选**：
  - 分类：背离
  - 方向：双向
  - 指标：HMA、RSI、MACD、成交量
  - 止损：否
  - 复杂度：中等
  - 周期：日内
  - 季节性：无
  - 神经网络：无
  - 背离：是
  - 风险等级：中等
