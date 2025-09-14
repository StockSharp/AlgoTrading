# RSI EA 策略

该策略模拟传统的 RSI 专家顾问。当 RSI 指标跨越预设水平时进行交易，并可通过止损、止盈以及可选的跟踪止损来管理风险。

## 策略逻辑
- 使用参数 `RsiPeriod` 计算 RSI。
- 当 RSI 上穿 `BuyLevel` 且当前无多头仓位时做多。
- 当 RSI 下穿 `SellLevel` 且当前无空头仓位时做空。
- 若启用 `CloseBySignal`，则出现相反交叉时平掉现有仓位。
- 可通过以价格单位表示的 `StopLoss`、`TakeProfit` 和 `TrailingStop` 来保护仓位。
- 使用 `CandleType` 指定的 K 线数据运行。

## 参数
- `OpenBuy` – 允许开多。
- `OpenSell` – 允许开空。
- `CloseBySignal` – 相反 RSI 信号时平仓。
- `StopLoss` – 以价格单位表示的止损。
- `TakeProfit` – 以价格单位表示的止盈。
- `TrailingStop` – 以价格单位表示的跟踪止损距离。
- `RsiPeriod` – RSI 计算长度。
- `BuyLevel` – 多头信号所需的 RSI 水平。
- `SellLevel` – 空头信号所需的 RSI 水平。
- `CandleType` – 使用的 K 线类型或时间框架。

交易量由策略的 `Volume` 属性控制。
