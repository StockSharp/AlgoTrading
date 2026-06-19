# Gaussian Detrended Reversion
[English](README.md) | [Русский](README_ru.md)

Gaussian Detrended Reversion 是一种均值回归策略，使用经 ALMA 平滑的去趋势价格振荡器。当平滑后的振荡器在零线下方上穿其滞后线时开多；当振荡器在零线上方下穿滞后线时开空。振荡器反向穿越或穿越零线时平仓。

## 细节
- **数据**: 价格K线。
- **入场条件**:
  - **多头**: ALMA 平滑的 DPO 上穿滞后线且位于零下。
  - **空头**: ALMA 平滑的 DPO 下穿滞后线且位于零上。
- **离场条件**: 反向滞后线交叉或穿越零线。
- **止损**: 无。
- **默认参数**:
  - `PriceLength` = 52
  - `SmoothingLength` = 52
  - `LagLength` = 26
- **过滤器**:
  - 类型: 均值回归
  - 方向: 多空皆可
  - 指标: EMA, ALMA
  - 复杂度: 低
  - 风险级别: 中等
