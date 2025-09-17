# Open Close

## Overview
Open Close is a port of the MetaTrader 5 expert advisor `Open Close.mq5` (ticket 23090). The strategy observes the relationship between the opens and closes of the two most recent finished candles. It trades a single position at a time: when the newer candle reverses relative to the previous one the strategy enters, and when both candles point in the same direction it exits. The C# version reproduces the original adaptive lot sizing that reduces exposure after a streak of losing trades.

## Strategy logic
### Candle pattern filter
* The strategy works exclusively on completed candles supplied by the configurable `CandleType` parameter.
* It keeps a rolling window of the two latest finished candles (named `previous` and `older`).
* The pattern compares both the opens and the closes of these candles:
  * **Bullish reversal** – `previous.Open > older.Open` **and** `previous.Close < older.Close`.
  * **Bearish reversal** – `previous.Open < older.Open` **and** `previous.Close > older.Close`.

### Entry rules
* If no position is open and the bullish reversal pattern appears, the strategy sends a market buy order.
* If no position is open and the bearish reversal pattern appears, it sends a market sell order.
* Only one position is allowed. Opposite signals are ignored until the active trade is closed.

### Exit rules
* When holding a long position, the strategy exits if the two tracked candles both move lower (`previous.Open < older.Open` and `previous.Close < older.Close`).
* When holding a short position, the exit trigger is symmetrical (`previous.Open > older.Open` and `previous.Close > older.Close`).
* There are no stop-loss or take-profit orders in the original advisor, so the port relies solely on the candle relationship for closing trades.

### Position sizing and loss streak handling
* Order volume is primarily determined by `MaximumRiskPercent` – the desired fraction of the portfolio value invested per trade. The raw size is `Portfolio.CurrentValue × MaximumRiskPercent ÷ referencePrice` using the latest close as the price proxy.
* If the portfolio valuation or price is unavailable, the `FallbackVolume` parameter acts as a safe default.
* After every fully closed trade the realized PnL is stored. The consecutive losing streak is counted over the last `HistoryDays` days.
  * When the streak is greater than one trade, the next order size is reduced by `volume × losses ÷ DecreaseFactor`, mimicking the MT5 logic.
* The final volume respects the security's volume step as well as minimum and maximum volume limits.

### Additional implementation notes
* The strategy reacts only to `CandleStates.Finished`, ensuring the pattern uses complete market data.
* Entry and exit checks occur at the close of the newest candle. In MetaTrader the order is submitted at the open of the next bar; the difference is negligible for higher timeframes but should be considered for very short intervals.
* Portfolio metrics in StockSharp approximate MetaTrader's account information. Adjust `MaximumRiskPercent` or `FallbackVolume` if the broker uses different contract multipliers.

## Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `MaximumRiskPercent` | `decimal` | `0.02` | Fraction of portfolio value used to size a new position (0.02 = 2%). |
| `DecreaseFactor` | `decimal` | `3` | Divisor applied to the lot size after consecutive losing trades. Larger values soften the reduction. |
| `HistoryDays` | `int` | `60` | Number of calendar days scanned when counting the latest losing streak. |
| `FallbackVolume` | `decimal` | `0.1` | Order volume used whenever the risk-based calculation cannot be performed. |
| `CandleType` | `DataType` | `TimeFrame(15m)` | Candle series that provides the open/close values for signal generation. |

## Differences from the MetaTrader version
* Account margin checks rely on StockSharp's `Portfolio.CurrentValue`; MetaTrader used `AccountFreeMargin`. The behaviour matches the original risk rule only when both platforms report similar valuations.
* Trade history is gathered from the strategy's own executions instead of the terminal-wide history. Make sure the strategy runs long enough to accumulate streak statistics.
* The port keeps the single-position model (no pyramiding) and mirrors the original lack of protective orders. Add stops externally if needed for risk control.
