# Wss Trader

Port of the "Wss_trader" MetaTrader 4 expert advisor published on forex-instruments.info. The original EA combines Camarilla-style reversal levels with classic pivot distances and opens a single trade per bar whenever price breaks the configured bands during the London session.

## Strategy logic

1. At the start of every new trading day the strategy reads the previous daily high, low, and close to build a pivot ladder:
   - `Pivot = (High + Low + Close) / 3`
   - `Long entry = Pivot + Metric × point`
   - `Short entry = Pivot − Metric × point`
   - `Long stop = Short entry`
   - `Short stop = Long entry`
   - Targets mirror the MetaTrader formulas `Close ± (High − Low) × 1.1 / 2` with the same safety clamp as the original code.
2. Trading is only allowed between `Start Hour` and `End Hour` (inclusive). Outside of the window every open position is closed immediately.
3. When a finished candle crosses above the long entry level (close >= level and previous close < level), the strategy buys once with the configured volume, attaches the pre-calculated stop and target, and blocks any further entries for that bar. A symmetric rule applies for shorts.
4. If the position moves in favour by at least `Trailing Points` price steps the stop is trailed to keep the same distance to the closing price. The stop never moves backwards.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `Working Candle` | Primary candle type used for intraday calculations. | `15 Minute` |
| `Daily Candle` | Candle type used to read the previous day for pivot levels. | `1 Day` |
| `Start Hour` | Hour (0-23) when trading is enabled. | `8` |
| `End Hour` | Hour (0-23) when trading stops accepting new entries. | `16` |
| `Metric Points` | Distance from the pivot to the breakout levels measured in price steps. | `20` |
| `Trailing Points` | Trailing stop distance in price steps. Set to `0` to disable trailing. | `20` |
| `Order Volume` | Order size that mirrors the original `lots` parameter. | `0.1` |

## Notes

- The strategy closes the current position as soon as the trading window ends, matching the behaviour of the original EA.
- Trailing is processed on finished candles. Intrabar trailing is not reproduced because StockSharp operates on candle closures in this port.
- Only one trade per candle is allowed, replicating the `tenb` flag from the MQL version.
