# NY ORB CP 策略
[English](README.md) | [Русский](README_ru.md)

该策略在纽约开盘区间突破后结合回测确认进行交易。利用 9:30-9:45 的区间，当价格回测并继续突破方向时入场。

## 细节

- **入场条件**：
  - 多头：价格突破并回测区间高点，同时趋势和成交量确认。
  - 空头：价格跌破并回测区间低点，同时趋势和成交量确认。
- **多空方向**：双向。
- **出场条件**：
  - 盈利目标为区间 0.33 * `RiskReward`。
  - 止损为区间 0.33。
- **止损**：有，动态。
- **默认值**：
  - `MinRangePoints` = 60
  - `RiskReward` = 3
  - `MaxTradesPerSession` = 3
  - `MaxDailyLoss` = -1000
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **过滤器**：
  - 类别：突破
  - 方向：双向
  - 指标：EMA、VWAP、SMA
  - 止损：有
  - 复杂度：中等
  － 时间框架：日内
  - 季节性：有
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
