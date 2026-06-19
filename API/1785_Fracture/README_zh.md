# Fracture
[Русский](README_ru.md) | [English](README.md)

Fracture 将分形突破与平滑移动平均和 ADX 结合，用于在盘整和趋势市场中交易。

## 细节

- **入场条件**：当 ADX 低于阈值时，若价格也在快速 SMMA 上方/下方，则在最后一个上/下分形处做多或做空。趋势状态下（快速 SMMA 高于/低于慢速线）在价格穿越快速 SMMA 时顺势入场。
- **多/空方向**：多头和空头。
- **出场条件**：利润超过 ATR 与 `MinProfit` 的乘积时平仓。
- **止损**：基于 ATR 的利润目标。
- **默认值**：
  - `CandleType` = TimeSpan.FromMinutes(1)
  - `AtrPeriod` = 14
  - `AdxPeriod` = 22
  - `AdxLine` = 40
  - `Ma1Period` = 5
  - `Ma2Period` = 9
  - `Ma3Period` = 22
  - `RangingMultiplier` = 0.5
  - `MinProfit` = 1
- **筛选**：
  - 分类：突破
  - 方向：多空皆可
  - 指标：分形、SMMA、ATR、ADX
  - 止损：有
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
