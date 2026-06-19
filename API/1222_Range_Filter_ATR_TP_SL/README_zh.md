# 基于ATR止盈止损的Range Filter策略
[English](README.md) | [Русский](README_ru.md)

当价格突破Range Filter上轨或下轨时开仓，并使用ATR计算的止盈止损平仓。

## 细节

- **入场条件**：价格上破上轨做多，下破下轨做空。
- **多空方向**：双向。
- **出场条件**：基于ATR的止盈或止损。
- **止损**：ATR在开仓时固定。
- **默认值**：
  - `RangeFilterLength` = 20
  - `RangeFilterMultiplier` = 1.5
  - `AtrLength` = 14
  - `TakeProfitMultiplier` = 1.5
  - `StopLossMultiplier` = 1.5
- **过滤器**：无。
- **复杂度**：中等。
- **时间框架**：可配置。
