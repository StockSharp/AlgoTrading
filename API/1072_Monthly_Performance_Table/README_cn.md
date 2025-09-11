# Monthly Performance Table 策略
[English](README.md) | [Русский](README_ru.md)

当 ADX 位于 +DI 与 -DI 之间且与 ADX 的差值超过可配置阈值时进行交易。

## 详情

- **入场条件**：
  - 多头：|+DI - ADX| ≥ `LongDifference` 且 |-DI - ADX| ≥ `LongDifference`，同时 ADX 处于 +DI 与 -DI 之间。
  - 空头：|+DI - ADX| ≥ `ShortDifference` 且 |-DI - ADX| ≥ `ShortDifference`，同时 ADX 处于 -DI 与 +DI 之间。
- **多空方向**：均可。
- **出场条件**：反向信号。
- **止损**：无。
- **默认值**：
  - `Length` = 14
  - `LongDifference` = 10
  - `ShortDifference` = 10
  - `CandleType` = TimeSpan.FromHours(1)
- **过滤器**：
  - 分类：Trend
  - 方向：双向
  - 指标：ADX, DMI
  - 止损：无
  - 复杂度：基础
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
