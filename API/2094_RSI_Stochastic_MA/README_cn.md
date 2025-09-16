# RSI 随机指标 均线 策略

该策略把简单移动平均线(SMA)作为趋势过滤器，并结合RSI与随机指标。
当价格在SMA之上时仅考虑做多；在SMA之下时仅考虑做空。RSI与随机指标的上下限
用于判断超买和超卖的入场时机。

当任一振荡指标离开极端区间时平仓，使交易始终跟随主要趋势，同时避免指标反向延伸。

## 参数
- `RsiPeriod` – RSI 计算周期。
- `RsiUpperLevel` – RSI 超买水平。
- `RsiLowerLevel` – RSI 超卖水平。
- `MaPeriod` – 趋势移动平均线周期。
- `StochKPeriod` – 随机指标 %K 周期。
- `StochDPeriod` – 随机指标 %D 平滑周期。
- `StochUpperLevel` – 随机指标超买水平。
- `StochLowerLevel` – 随机指标超卖水平。
- `Volume` – 交易量。
- `CandleType` – 计算所使用的K线类型。

## 指标
- 简单移动平均线
- 相对强弱指数
- 随机指标

## 交易规则
- **买入**：价格高于SMA，RSI低于 `RsiLowerLevel`，并且随机指标两条线都低于 `StochLowerLevel`。
- **卖出**：价格低于SMA，RSI高于 `RsiUpperLevel`，并且随机指标两条线都高于 `StochUpperLevel`。
- **多头平仓**：当RSI或随机指标突破其上限时。
- **空头平仓**：当RSI或随机指标跌破其下限时。
