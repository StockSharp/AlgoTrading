# RD Trend Trigger 策略
[English](README.md) | [Русский](README_ru.md)

RD Trend Trigger 策略使用 RD-TrendTrigger 振荡器，根据所选模式捕捉趋势反转或突破。在 twist 模式下，交易跟随振荡器方向的变化；在 disposition 模式下，当振荡器突破预设水平时进行交易。

## 详情

- **入场条件**：
  - **Twist 模式**：振荡器向上转折时做多，向下转折时做空。
  - **Disposition 模式**：振荡器上穿 `HighLevel` 时做多，下破 `LowLevel` 时做空。
- **多/空**：双向。
- **出场条件**：
  - 反向信号，或在 disposition 模式下振荡器上升至 `LowLevel` 以上时退出空头。
- **止损**：默认无，可额外启用保护。
- **默认值**：
  - `Regress` = 15
  - `T3Length` = 5
  - `T3VolumeFactor` = 0.7
  - `HighLevel` = 50
  - `LowLevel` = -50
  - `Mode` = Twist
  - `CandleType` = 4-hour candles
- **过滤器**：
  - 类别：趋势跟随
  - 方向：多/空
  - 指标：自定义 RD-TrendTrigger（基于高低价和 Tillson T3）
  - 止损：可选
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
