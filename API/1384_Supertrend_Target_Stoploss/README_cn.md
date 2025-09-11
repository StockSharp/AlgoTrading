# Supertrend Target Stop
[English](README.md) | [Русский](README_ru.md)

当价格上穿 Supertrend 线时买入，下穿时卖出。仓位在达到固定百分比目标或止损时平仓。

## 细节

- **入场条件**：价格穿越 Supertrend。
- **多空方向**：双向。
- **出场条件**：达到目标或止损百分比。
- **止损**：是，固定百分比。
- **默认值**：
  - `Period` = 14
  - `Multiplier` = 3m
  - `TargetPct` = 0.01m
  - `StopPct` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(5)
