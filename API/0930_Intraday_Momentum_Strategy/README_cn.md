# 日内动量策略

在设定的交易时段内使用 EMA 金叉、RSI 过滤和 VWAP 确认进行交易。当快速 EMA 上穿慢速 EMA 且 RSI 低于超买水平并且价格位于 VWAP 之上时做多；反之则做空。采用固定百分比的止损和止盈，在交易时段结束时平掉所有仓位。

## 参数

- **EmaFastLength**: 快速 EMA 周期。
- **EmaSlowLength**: 慢速 EMA 周期。
- **RsiLength**: RSI 周期。
- **RsiOverbought**: RSI 超买水平。
- **RsiOversold**: RSI 超卖水平。
- **StopLossPerc**: 止损百分比。
- **TakeProfitPerc**: 止盈百分比。
- **StartHour**: 开始时段的小时。
- **StartMinute**: 开始时段的分钟。
- **EndHour**: 结束时段的小时。
- **EndMinute**: 结束时段的分钟。
- **CandleType**: 蜡烛类型。

