# Equivolume Bars 策略

该策略基于最近周期成交量之和的比率。

## 逻辑
- 计算当前成交量与`Lookback`周期成交量之和的比率。
- 当比率超过阈值且K线收阳时做多。
- 当比率超过阈值且K线收阴时做空。
- 当比率跌破阈值或K线反向时平仓。

## 参数
- `Lookback` – 成交量求和的周期。
- `Volume Threshold` – 成交量比率阈值。
- `Candle Type` – 使用的K线类型。
