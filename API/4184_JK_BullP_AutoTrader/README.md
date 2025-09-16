# JK BullP AutoTrader Strategy

## Overview
JK BullP AutoTrader is a momentum-following expert advisor originally written for MetaTrader 4. It monitors the Elder Bulls Power
indicator and reacts when bullish pressure weakens or turns negative. The StockSharp port keeps the straightforward logic of the
original while providing explicit parameters, detailed trailing management, and platform-friendly risk controls.

## Trading logic
1. The strategy subscribes to a configurable candle series (1-hour candles by default) and calculates a 13-period exponential
   moving average (EMA) to replicate the Bulls Power baseline.
2. For every completed candle, Bulls Power is measured as the difference between the candle high and the EMA value.
3. Two consecutive Bulls Power readings are compared:
   - If the prior value is above the latest value and the latest value remains positive, the strategy opens a short position.
   - If the latest Bulls Power value drops below zero, the strategy opens a long position.
4. Only one position can be active at a time, mirroring the original MQL expert that blocked new orders while trades were open.

## Risk management and exits
- **Initial stop-loss / take-profit:** Distances are configured in pips and converted to price units using the security price step.
  Both protections are enabled through StockSharp's `StartProtection` helper, keeping behaviour close to the MetaTrader inputs.
- **Trailing stop:** Once floating profit exceeds the specified trailing distance, the stop level is moved candle-by-candle.
  Instead of modifying existing stop orders (as in MetaTrader), the port issues a market order to exit the position when price
  closes beyond the trailing threshold. This guarantees timely exits even when protective orders are not supported by the venue.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `OrderVolume` | Market order size used for entries. | 8.5 |
| `TakeProfitPips` | Take-profit distance in pips (converted to price units). | 500 |
| `StopLossPips` | Stop-loss distance in pips. | 20 |
| `TrailingStopPips` | Profit distance in pips that activates and maintains the trailing stop. | 10 |
| `EmaPeriod` | Length of the EMA used by the Bulls Power calculation. | 13 |
| `CandleType` | Data type of candles driving the strategy (default 1-hour timeframe). | 1-hour candles |

## Implementation notes
- The unused inputs (`Patr`, `Prange`, `Kstop`, `kts`, `Vts`) from the original script were intentionally omitted because they had
  no effect on the MetaTrader logic.
- Pip distances rely on the instrument `PriceStep`. If step data is unavailable, a value of `1` is used as a conservative default.
- The strategy uses StockSharp's high-level `Bind` API, processes only finished candles, and keeps internal state (`_previousBullsPower`)
  to match the MT4 shift-based calculations.
- Trailing logic resets automatically after each exit to avoid stale stop levels when a new position is opened.
