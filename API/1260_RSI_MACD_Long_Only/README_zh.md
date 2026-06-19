# RSI + MACD Long-Only 策略
[English](README.md) | [Русский](README_ru.md)

当 RSI 上穿中线并获得 MACD 多头确认，或 MACD 上穿信号线且 RSI 位于中线之上时进入多头。 当 RSI 跌破中线或 MACD 下穿信号线且柱状图 ≤ 0 时退出。 可选的 EMA 趋势过滤和超卖窗口可进一步精炼入场。

## 详情

- **入场条件**：RSI 上穿中线并伴随 MACD 多头确认，或 MACD 上穿信号线且 RSI 位于中线之上
- **多空方向**：仅做多
- **出场条件**：RSI 下穿中线或 MACD 下穿信号线且柱状图 ≤ 0
- **止损止盈**：可选百分比止盈和止损
- **默认值**：
  - `RsiLength` = 14
  - `RsiOversold` = 30
  - `RsiMidline` = 50
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `OversoldWindowBars` = 10
  - `EmaLength` = 200
  - `TakeProfitPercent` = 11.5
  - `StopLossPercent` = 2.5
- **过滤器**：
  - 分类：Trend
  - 方向：Long
  - 指标：RSI、MACD、EMA
  - 止损：有（可选）
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
