# Buy On 5 Day Low
[English](README.md) | [Русский](README_ru.md)

**Buy On 5 Day Low** 策略在收盘价跌破前一根K线的5日最低价时做多，收盘价突破前一根K线的最高价时平仓。交易只在指定的时间窗口内进行。

## 细节
- **入场条件**：收盘价低于过去N根K线的最低值。
- **多空方向**：仅做多。
- **出场条件**：收盘价高于前一根K线的最高价。
- **止损**：无。
- **默认值**：
  - `LowestPeriod = 5`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
  - `StartTime = new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero)`
  - `EndTime = new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero)`
- **过滤器**：
  - 分类：均值回归
  - 方向：多头
  - 指标：Lowest, High
  - 止损：无
  - 复杂度：基础
  - 时间框架：日线
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
