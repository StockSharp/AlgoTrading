# 动态突破大师策略

使用 Donchian 通道结合均线趋势过滤、RSI 与 ATR 过滤，同时考虑成交量与交易时间的突破策略。

## 策略规则

- 多头：价格向上突破 Donchian 上轨或突破后回踩，上轨；MA1 > MA2，RSI 在 `RsiOversold` 与 `RsiOverbought` 之间，ATR 高于 `AtrMultiplier`，成交量高于均值且在交易时段内。
- 空头：价格向下突破下轨或突破后回抽，MA1 < MA2，其余条件同上。
- 退出：止损/追踪止损、止盈、RSI 极值或均线反向穿越。

## 参数

- `DonchianPeriod` – 通道回溯周期。
- `Ma1Length`, `Ma1IsEma` – 第一条均线。
- `Ma2Length`, `Ma2IsEma` – 第二条均线。
- `RsiLength`, `RsiOverbought`, `RsiOversold` – RSI 过滤。
- `AtrLength`, `AtrMultiplier` – 波动率过滤。
- `RiskPerTrade`, `RewardRatio`, `AccountSize` – 仓位管理。
- `TradingStartHour`, `TradingEndHour` – 交易时间。
- `CandleType` – K线周期。
