# RSI 趋势跟随策略
[English](README.md) | [Русский](README_ru.md)

RSI 趋势跟随策略在 RSI、随机指标、MACD 和价格位于长期 EMA 之上时做多。价格按 ATR 的倍数有利运行后，策略启用追踪止盈并跟随较短周期 EMA。

当价格跌破追踪 EMA 或触及基于 ATR 的止损时平仓。

## 细节

- **入场条件**：`K < 80 && D < 80 && MACD > Signal && RSI > 50 && Low > EMA(200)`
- **多空方向**：仅做多
- **离场条件**：价格跌破追踪 EMA 或触发止损
- **止损**：是，基于 ATR
- **默认值**：
  - `StopLossAtr` = 1.75
  - `TrailingActivationAtr` = 2.25
  - `RsiPeriod` = 14
  - `TrailingEmaLength` = 20
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
