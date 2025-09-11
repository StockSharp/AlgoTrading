# Supply Demand Engulfment
[English](README.md) | [Русский](README_ru.md)

策略在 Donchian 支撑阻力区域附近交易多头和空头的吞没形态。

## 细节

- **入场条件**：区域边界出现吞没形态。
- **多空方向**：双向。
- **出场条件**：相反信号。
- **止损**：否。
- **默认值**：
  - `ZonePeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
