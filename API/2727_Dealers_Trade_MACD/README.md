# Dealers Trade MACD Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Dealers Trade MACD strategy is a pyramiding system that was ported from the original MQL5 "Dealers Trade v7.74" expert advisor. It follows the slope of the MACD main line to decide when to accumulate positions in the trend direction. The logic is designed for swing trading on H4 and D1 charts where momentum shifts are less noisy.

## How the strategy works

- **Signal generation** – the strategy subscribes to candles of the selected timeframe and evaluates the MACD main line value on every closed bar. A rising MACD implies long bias and a falling MACD implies short bias. The signal can be inverted with the `ReverseCondition` parameter to match accounts that historically traded contrarian entries.
- **Position sizing** – the first order uses either the fixed `FixedVolume` size or, if it is set to `0`, the system allocates risk dynamically from portfolio equity using the `RiskPercent` parameter and the configured stop loss distance. Additional entries are multiplied by `VolumeMultiplier` raised to the current position count (e.g. 1.6, 1.6², 1.6³, …) and are only sent when the price has moved by at least `IntervalPoints * PriceStep` from the last fill. Orders are skipped once the net exposure would exceed `MaxVolume` or the number of entries reaches `MaxPositions`.
- **Order management** – every position keeps its own stop loss and take profit targets calculated from the entry price and the point-based offsets (`StopLossPoints`, `TakeProfitPoints`). If `TrailingStopPoints` is greater than zero the stop is pulled up (or down for shorts) once the profit exceeds `TrailingStopPoints + TrailingStepPoints`, emulating the original trailing behaviour.
- **Account protection** – when the number of open trades is greater than `PositionsForProtection` and the aggregated unrealised profit crosses `SecureProfit`, the strategy closes the most profitable leg to lock in gains before adding new exposure. This mirrors the "Account protection" block from the MQL version.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `CandleType` | H4 | Timeframe used for MACD calculations and trade decisions. |
| `FixedVolume` | 0.1 | Lot size for the first entry. Set to 0 to enable risk-based sizing. |
| `RiskPercent` | 5 | Percentage of current equity risked when `FixedVolume` is zero. |
| `StopLossPoints` | 90 | Stop loss distance expressed in price steps. Use 0 to disable hard stops. |
| `TakeProfitPoints` | 30 | Take profit distance in price steps. Use 0 to disable. |
| `TrailingStopPoints` | 15 | Trailing stop distance in price steps. Set to 0 to turn trailing off. |
| `TrailingStepPoints` | 5 | Additional distance that must be gained before the trailing stop moves again. |
| `MaxPositions` | 5 | Maximum number of simultaneously open entries. |
| `IntervalPoints` | 15 | Minimum distance in price steps required between consecutive entries. |
| `SecureProfit` | 50 | Profit threshold (in quote currency) that triggers account protection. |
| `AccountProtection` | true | Enables closing the best performing trade when the secure profit target is reached. |
| `PositionsForProtection` | 3 | Minimum number of trades that must be open before protection can trigger. |
| `ReverseCondition` | false | Inverts the MACD slope interpretation. |
| `MacdFastPeriod` | 14 | Fast EMA length for the MACD indicator. |
| `MacdSlowPeriod` | 26 | Slow EMA length for the MACD indicator. |
| `MacdSignalPeriod` | 1 | Signal EMA length for the MACD indicator (set to 1 in the original expert advisor). |
| `MaxVolume` | 5 | Upper cap for the cumulative position size. |
| `VolumeMultiplier` | 1.6 | Multiplier applied to the base size for every new entry. |

## Notes and limitations

- The original MQL expert was able to hold long and short hedged positions simultaneously. StockSharp uses netted positions by default, therefore this port closes opposite exposure before adding new trades in the other direction.
- MACD values are evaluated on closed candles only. Intrabar signals may appear later than in the tick-based MQL implementation, but the behaviour is far more stable for historical testing.
- All point-based distances are multiplied by the instrument `PriceStep`. If the security does not provide that metadata the strategy falls back to a 0.0001 step, so adjust parameters when trading instruments with different tick sizes.
- When `FixedVolume` is zero the strategy requires a non-zero stop loss distance to calculate risk-based sizing. If the stop is disabled the volume defaults to zero and no trade is sent.

