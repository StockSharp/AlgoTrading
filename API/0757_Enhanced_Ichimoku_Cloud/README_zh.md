# 增强型一目均衡云策略
[English](README.md) | [Русский](README_ru.md)

仅做多的一目均衡云策略，带有171日EMA过滤。 当span A高于span B、价格突破25根K线前的最高点、Tenkan高于Kijun且收盘价高于EMA时买入。 当Tenkan跌破Kijun时平仓。

## 细节

- **入场条件**：spanA > spanB，close > high[25]，Tenkan > Kijun，close > EMA。
- **多空方向**：仅多头。
- **出场条件**：Tenkan < Kijun。
- **止损**：无。
- **默认值**：
  - `ConversionPeriods` = 7
  - `BasePeriods` = 211
  - `LaggingSpan2Periods` = 120
  - `Displacement` = 41
  - `EmaPeriod` = 171
  - `StartDate` = 2018-01-01
  - `EndDate` = 2069-12-31
  - `CandleType` = TimeSpan.FromDays(1)
- **筛选**：
  - 类别：趋势
  - 方向：多头
  - 指标：Ichimoku, EMA
  - 止损：无
  - 复杂度：基础
  - 时间框架：日线
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
