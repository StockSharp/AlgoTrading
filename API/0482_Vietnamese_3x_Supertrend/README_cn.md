# Vietnamese 3x Supertrend 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用三个不同 ATR 参数的 SuperTrend 指标。当慢速趋势仍为下行时，在回调处逐步建立多头头寸。价格向有利方向运行后，可选择启用保本止损。

## 细节

- **入场条件**：
  - 慢速 SuperTrend 为下行趋势。
  - **Long 1**：中速趋势向上且快速趋势向下。
  - **Long 2**：中速趋势向下且价格高于快速 SuperTrend 线。
  - **Long 3**：快速趋势向下并向上突破该阶段的最高价。
- **多空方向**：仅做多。
- **出场条件**：
  - 所有 SuperTrend 转为上行且 K 线收阴。
  - 平均持仓价高于当前收盘价。
  - 可选保本止损。
- **止损**：可选保本止损。
- **默认参数**：
  - `FastAtrLength` = 10
  - `FastMultiplier` = 1
  - `MediumAtrLength` = 11
  - `MediumMultiplier` = 2
  - `SlowAtrLength` = 12
  - `SlowMultiplier` = 3
  - `UseHighestOfTwoRedCandles` = false
  - `UseEntryStopLoss` = true
  - `UseAllDowntrendExit` = true
  - `UseAvgPriceInLoss` = true
- **过滤条件**：
  - 分类：趋势跟随
  - 方向：多头
  - 指标：SuperTrend
  - 止损：可选
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
