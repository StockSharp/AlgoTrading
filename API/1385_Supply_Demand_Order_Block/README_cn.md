# Supply Demand Order Block
[English](README.md) | [Русский](README_ru.md)

突破策略，利用 Donchian 支撑阻力配合 EMA 趋势过滤和成交量放大确认。仓位通过止损和移动止损保护。

## 细节

- **入场条件**：价格突破 Donchian 通道并满足趋势和成交量过滤。
- **多空方向**：双向。
- **出场条件**：止损或移动止损。
- **止损**：是，固定和移动。
- **默认值**：
  - `Length` = 20
  - `StopLossTicks` = 1000
  - `TrailingStartTicks` = 2000
  - `CandleType` = TimeSpan.FromMinutes(5)
