# Cyclops Cycle Identifier Strategy

## Overview

This strategy ports the MetaTrader expert advisor **Cyclops v1.2** together with its proprietary *CycleIdentifier* indicator to StockSharp's high level API. The algorithm smooths closing prices with a smoothed moving average (SMMA), measures recent volatility through a long lookback average true range, and marks cycle turning points when price travels far enough from the most recent swing. Major cycle reversals generate new entries while minor reversals offer optional exit signals.

A configurable zero-lag filter validates the slope of the smoothed series. The filter can work directly on smoothed price data or on a Wilder-style RSI derived from the same series. Additional confirmation is available through a classic Momentum indicator, and trading can be limited to a specific weekday/hour window.

## Signal logic

- **Cycle detection** – The internal state machine tracks the last swing highs and lows of the smoothed price. When price travels beyond the adaptive threshold (average range × *Length*), the strategy marks a minor cycle. A larger multiple (*MajorCycleStrength*) is required to flag a major cycle.
- **Entries** – Major bullish cycles (`MajorBuy`) open longs; major bearish cycles (`MajorSell`) open shorts. Active positions are automatically closed before reversing to the opposite side.
- **Optional exits** – When *UseExitSignal* is enabled, profitable trades can close on the corresponding minor cycle signal (`MinorSellExit` for longs, `MinorBuyExit` for shorts) if no opposite major cycle is present.
- **Zero-lag filter** – If *UseCycleFilter* is enabled, a zero-lag smoothing filter must confirm the slope (rising for longs, falling for shorts). The filter source is selected by *CycleFilterMode* (smoothed price or RSI).
- **Momentum filter** – With *UseMomentumFilter* enabled, entries require `Momentum ≥ MomentumTriggerLong` for longs and `Momentum ≤ MomentumTriggerShort` for shorts.

## Trade management

- **Fixed targets** – *TakeProfitPips* and *StopLossPips* define optional fixed exits in instrument pips.
- **Break-even** – When *BreakEvenTrigger* pips of profit are reached, the stop is pulled to entry ± one pip.
- **Trailing** – *TrailingStopTrigger* activates a trailing stop that follows price at *TrailingStopPips* once the trigger distance is achieved.
- **Session control** – If *UseTimeRestriction* is true, new positions are allowed only before `DayEnd` (0=Sunday) and up to `HourEnd` (inclusive) on that day. Existing trades are still managed afterward.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `Volume` | Order volume used for entries. |
| `PriceActionFilter` | Length of the smoothed moving average applied to close price. |
| `Length` | Multiplier applied to the average range to detect minor cycles. |
| `MajorCycleStrength` | Multiplier separating major from minor swings. |
| `UseCycleFilter` | Enables the zero-lag slope confirmation. |
| `CycleFilterMode` | Selects zero-lag input: smoothed price (`Sma`) or RSI (`Rsi`). |
| `FilterStrengthSma` | Length of the zero-lag filter when the smoothed price is used. |
| `FilterStrengthRsi` | Length and RSI period when the filter relies on RSI values. |
| `UseMomentumFilter` | Turns the momentum confirmation on or off. |
| `MomentumPeriod` | Momentum indicator length. |
| `MomentumTriggerLong` | Minimum momentum required for long entries. |
| `MomentumTriggerShort` | Maximum momentum allowed for short entries. |
| `UseExitSignal` | Enables minor-cycle based exits when profitable. |
| `UseTimeRestriction` | Limits trading to the configured weekday/hour window. |
| `DayEnd` | Last day of week when new entries are permitted. |
| `HourEnd` | Last hour on the final trading day for new entries. |
| `BreakEvenTrigger` | Profit in pips required to activate the break-even stop. |
| `TrailingStopTrigger` | Profit in pips required to start trailing. |
| `TrailingStopPips` | Distance in pips maintained by the trailing stop. |
| `TakeProfitPips` | Fixed take-profit distance in pips. |
| `StopLossPips` | Fixed stop-loss distance in pips. |
| `CandleType` | Primary timeframe that feeds the strategy. |

## Differences compared to the original EA

- The average range is estimated with a 250-period Average True Range multiplied by *Length*, providing behaviour equivalent to the rolling high/low span used in MQL.
- The momentum confirmation uses the actual indicator value (the MQL script compared against the pip multiplier `bm`, effectively disabling the filter).
- Zero-lag smoothing is implemented with the same recursive coefficients but expressed in decimal arithmetic. RSI mode uses a Wilder RSI whose period equals *FilterStrengthRsi*.

## Usage notes

1. Select the instrument and bind the `CandleType` parameter to the desired timeframe.
2. Configure the risk and session settings to match your broker environment.
3. Enable *UseCycleFilter* or *UseMomentumFilter* when a stricter confirmation is required; disable them for faster but noisier entries.
4. The strategy maintains at most one open position. Opposite cycle signals close the current position before a new one is evaluated.
