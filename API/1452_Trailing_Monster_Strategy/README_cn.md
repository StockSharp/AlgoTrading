# Trailing Monster Strategy
[English](README.md) | [Русский](README_ru.md)

该策略结合KAMA趋势判断、RSI过滤和百分比追踪止损。当RSI在KAMA趋势方向上突破极值时开仓，延迟一定的K线后启动追踪止损以保护利润。

## 细节
- **入场条件**：
  - **多头**：RSI > `RsiOverbought`，收盘价高于SMA，KAMA上升
  - **空头**：RSI < `RsiOversold`，收盘价低于SMA，KAMA下降
- **多空方向**：双向
- **出场条件**：
  - `DelayBars`后启动的百分比追踪止损
- **止损**：按百分比的追踪止损
- **默认参数**：
  - `KamaLength` = 40
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `SmaLength` = 200
  - `BarsBetweenEntries` = 3
  - `TrailingStopPct` = 12m
  - `DelayBars` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 类别：趋势
  - 方向：双向
  - 指标：KAMA、RSI、SMA
  - 止损：追踪
  - 复杂度：基础
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
