# Divergence For Many Indicators 策略

检测价格与 RSI 及 MACD 直方图之间的背离。当背离数量达到设定阈值时，策略按相反方向开仓。

## 参数
- `RsiPeriod` – RSI 周期。
- `MacdFastPeriod` – MACD 快速周期。
- `MacdSlowPeriod` – MACD 慢速周期。
- `MacdSignalPeriod` – MACD 信号周期。
- `MinDivergence` – 触发交易的最小背离数量。
- `CandleType` – 使用的 K 线类型。
