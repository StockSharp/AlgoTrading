# AI SuperTrend 策略
[English](README.md) | [Русский](README_ru.md)

AI SuperTrend 策略结合 SuperTrend 指标与价格及 SuperTrend 线的加权移动平均。 当 SuperTrend 向上反转且价格 WMA 高于 SuperTrend WMA 时开多仓；相反条件下开空仓。 持仓使用基于 ATR 的动态跟踪止损进行保护。

## 细节

- **入场条件**：
  - **多头**：SuperTrend 方向转为向上且价格 WMA 高于 SuperTrend WMA。
  - **空头**：SuperTrend 方向转为向下且价格 WMA 低于 SuperTrend WMA。
- **出场条件**：
  - 趋势反转或 ATR 跟踪止损。
- **止损**：动态 ATR 跟踪止损。
- **默认值**：
  - `AtrPeriod` = 10
  - `AtrFactor` = 3
  - `PriceWmaLength` = 20
  - `SuperWmaLength` = 100
  - `EnableLong` = true
  - `EnableShort` = true
- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：SuperTrend、WMA、ATR
  - 止损：是
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
