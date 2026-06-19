# CVD Divergence Volume HMA RSI MACD 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合 Hull 移动平均线、RSI、MACD、成交量过滤器以及累积成交量差 (CVD) 的背离来寻找趋势机会。

当 HMA20 高于 HMA50、RSI 表现出多头动能、MACD 柱状图上升、成交量高于其均值且 CVD 形成看涨背离或上升时开多；做空条件相反。

## 详情
- **入场条件**：
  - **多头**：HMA20 > HMA50 且价格高于 HMA20；RSI 介于 40 和 `RsiOverbought`；MACD 线在信号线上方且柱状图上升；成交量 > SMA * `VolumeMultiplier`；CVD 看涨背离或上升。
  - **空头**：HMA20 < HMA50 且价格低于 HMA20；RSI 介于 `RsiOversold` 和 60；MACD 线在信号线下方且柱状图下降；成交量 > SMA * `VolumeMultiplier`；CVD 看跌背离或下降。
- **多空方向**：双向。
- **离场条件**：
  - **多头**：价格跌破 HMA20 或 RSI > `RsiOverbought` 或 MACD 线下穿信号线。
  - **空头**：价格突破 HMA20 或 RSI < `RsiOversold` 或 MACD 线上穿信号线。
- **止损**：无。
- **默认参数**：
  - `Hma20Length` = 20
  - `Hma50Length` = 50
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumeMaLength` = 20
  - `VolumeMultiplier` = 1.5
  - `CvdLength` = 14
  - `DivergenceLookback` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选**：
  - 分类：混合
  - 方向：双向
  - 指标：HMA、RSI、MACD、成交量、CVD
  - 止损：无
  - 复杂度：高级
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：是
  - 风险等级：中等
