# RSI EA Strategy

This strategy emulates a classic RSI expert advisor. It trades when the Relative Strength Index crosses predefined levels and manages risk with stop loss, take profit and optional trailing stop.

## Strategy Logic
- Calculate RSI using the configurable `RsiPeriod`.
- **Long entry** when RSI rises above `BuyLevel` and no long position exists.
- **Short entry** when RSI falls below `SellLevel` and no short position exists.
- When `CloseBySignal` is enabled, an opposite cross closes the existing position.
- Positions can be protected with `StopLoss`, `TakeProfit` and `TrailingStop` measured in price units.
- Works on candle data defined by `CandleType`.

## Parameters
- `OpenBuy` – enable long entries.
- `OpenSell` – enable short entries.
- `CloseBySignal` – close by opposite RSI signal.
- `StopLoss` – loss in price units.
- `TakeProfit` – profit in price units.
- `TrailingStop` – trailing distance in price units.
- `RsiPeriod` – RSI calculation length.
- `BuyLevel` – RSI threshold for long signals.
- `SellLevel` – RSI threshold for short signals.
- `CandleType` – candle timeframe or type to subscribe.

The default trade volume is controlled by the strategy `Volume` property.
