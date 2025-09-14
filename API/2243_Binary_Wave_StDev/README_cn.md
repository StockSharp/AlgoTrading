# Binary Wave StdDev 策略
[English](README.md) | [Русский](README_ru.md)

该策略对 MA、MACD、CCI、动量、RSI 和 ADX 的信号按权重求和。
当标准差表示的波动率超过阈值时，按累计得分的方向开仓。
可选的止损和止盈以点为单位。

## 细节

- **入场条件**：
  - 多头：得分 > 0 且 StdDev >= EntryVolatility
  - 空头：得分 < 0 且 StdDev >= EntryVolatility
- **出场条件**：
  - 波动率下降到 ExitVolatility 以下
- **止损/止盈**：通过 `UseStopLoss` 和 `UseTakeProfit` 可选
- **默认值**：
  - `WeightMa` = 1
  - `WeightMacd` = 1
  - `WeightCci` = 1
  - `WeightMomentum` = 1
  - `WeightRsi` = 1
  - `WeightAdx` = 1
  - `MaPeriod` = 13
  - `FastMacd` = 12
  - `SlowMacd` = 26
  - `SignalMacd` = 9
  - `CciPeriod` = 14
  - `MomentumPeriod` = 14
  - `RsiPeriod` = 14
  - `AdxPeriod` = 14
  - `StdDevPeriod` = 9
  - `EntryVolatility` = 1.5
  - `ExitVolatility` = 1
  - `StopLossPoints` = 1000
  - `TakeProfitPoints` = 2000
  - `UseStopLoss` = false
  - `UseTakeProfit` = false
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **过滤**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：MA、MACD、CCI、Momentum、RSI、ADX、StandardDeviation
  - 止损：可选
  - 复杂度：中
  - 时间框架：中期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
