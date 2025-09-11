# Dynamic Breakout Master Strategy

Breakout strategy using Donchian Channels with moving average trend filter, RSI and ATR filters plus volume and time constraints.

## Strategy rules

- Long: price breaks above upper Donchian band or pulls back after breakout, MA1 > MA2, RSI between `RsiOversold` and `RsiOverbought`, ATR above `AtrMultiplier`, volume above average and within trading hours.
- Short: price breaks below lower Donchian band or pulls back after breakout, MA1 < MA2, RSI between thresholds, ATR above `AtrMultiplier`, volume above average and within trading hours.
- Exits: stop loss/trailing, take profit, RSI extreme or moving average crossover.

## Parameters

- `DonchianPeriod` – channel lookback period.
- `Ma1Length`, `Ma1IsEma` – first moving average.
- `Ma2Length`, `Ma2IsEma` – second moving average.
- `RsiLength`, `RsiOverbought`, `RsiOversold` – RSI filter.
- `AtrLength`, `AtrMultiplier` – volatility filter.
- `RiskPerTrade`, `RewardRatio`, `AccountSize` – position sizing.
- `TradingStartHour`, `TradingEndHour` – trading session.
- `CandleType` – candle timeframe.
