# EMA RSI Swing Trend Filter
[English](README.md) | [Русский](README_ru.md)

该策略在EMA200趋势方向上利用EMA20与EMA50的交叉进行交易。
可选的RSI过滤器在指标超买或超卖时限制开仓。

## 细节

- **入场条件**：EMA20与EMA50交叉，结合EMA200位置及可选的RSI过滤。
- **多空方向**：双向。
- **出场条件**：可选的反向EMA交叉退出。
- **止损**：无。
- **默认值**：
  - `EmaFastPeriod` = 20
  - `EmaSlowPeriod` = 50
  - `EmaTrendPeriod` = 200
  - `RsiLength` = 14
  - `UseRsiFilter` = true
  - `RsiMaxLong` = 70
  - `RsiMinShort` = 30
  - `RequireCloseConfirm` = true
  - `ExitOnOpposite` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 类别: Trend
  - 方向: Both
  - 指标: EMA, RSI
  - 止损: 无
  - 复杂度: Basic
  - 时间框架: Intraday
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: Medium
