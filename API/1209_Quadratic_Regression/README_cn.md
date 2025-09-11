# Quadratic Regression 策略
[Русский](README_ru.md) | [English](README.md)

该策略对最近 `Length` 根K线计算二次回归线，并在价格与回归线交叉时进行交易。

## 细节

- **入场条件**：价格上穿或下穿二次回归线。
- **多/空**：双向。
- **出场条件**：反向交叉。
- **止损**：无。
- **默认值**：
  - `Length` = 54。
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()。
- **过滤器**：
  - 类别：趋势
  - 方向：双向
  - 指标：Quadratic Regression
  - 止损：无
  - 复杂度：基础
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：低
