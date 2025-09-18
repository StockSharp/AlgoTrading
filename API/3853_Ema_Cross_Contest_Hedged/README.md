# EMA Cross Contest Hedged Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy reproduces the MetaTrader expert advisor **EMA_CROSS_CONTEST_HEDGED** inside StockSharp. The robot looks for a bullish/bearish crossover between a fast and a slow exponential moving average (EMA) and optionally checks the MACD histogram as a trend confirmation. When a signal appears, the strategy immediately opens a market position and places a ladder of stop orders that hedge the trade by adding more exposure if price keeps trending.

## Trading Logic
- Calculate a short EMA and a long EMA on the configured candle series. Signals can be taken either from the previous completed bar (default) or from the current bar once the candle closes.
- Detect a **bullish crossover** when the short EMA rises above the long EMA and a **bearish crossover** when it falls below the long EMA.
- Optionally require the MACD line to be above zero for long trades and below zero for short trades, replicating the MQL filter.
- When the bullish condition is satisfied, buy at market, attach stop-loss and take-profit targets, and queue four buy-stop pending orders spaced by the hedge distance.
- When the bearish condition is satisfied, sell at market, attach risk targets, and queue four sell-stop pending orders below price.
- Pending orders are cancelled after their expiration time if they are not triggered.
- Trailing stops tighten as open profits grow, and opposite crossovers can force early exits when `Use Close` is enabled.

## Parameters
- **Candle Type** – timeframe used for all calculations.
- **Order Volume** – trade volume for the initial position and each hedge order.
- **Take Profit (pips)** – take-profit distance in pips.
- **Stop Loss (pips)** – stop-loss distance in pips.
- **Trailing Stop (pips)** – trailing stop distance (0 disables trailing).
- **Hedge Level (pips)** – spacing between the hedging pending orders.
- **Use Close** – close existing positions when an opposite crossover happens.
- **Use MACD** – require MACD confirmation for trade entries.
- **Expiration (s)** – lifetime for pending hedge orders.
- **Short EMA** – length of the fast EMA.
- **Long EMA** – length of the slow EMA (must be greater than the fast EMA).
- **Signal Bar** – choose whether to evaluate signals on the current bar (0) or the previous bar (1).

## Notes
- All comments in the code are provided in English as requested.
- The pending hedge structure follows the behaviour from the original MQL expert advisor, placing four orders at equal distance steps.
- Price conversions from pips take the symbol’s `PriceStep` and `Decimals` into account to match MetaTrader point calculations.
