# NRTR Revers Strategy

## Overview

The NRTR Revers strategy is a C# conversion of the original MetaTrader 5 expert advisor `NRTR_Revers.mq5`. The system uses the Nick Rypock Trailing Reverse (NRTR) approach to alternate between long and short bias depending on how price interacts with ATR-projected support and resistance bands. Trading decisions are evaluated on the close of each finished candle coming from a single timeframe subscription.

## Trading Logic

1. **ATR projection** – The strategy calculates an Average True Range (ATR) with the configurable period. The ATR value is multiplied by the `VolatilityMultiplier` to obtain the band offset.
2. **Dynamic bands** – For the current trend direction the strategy finds:
   - The lowest low (or highest high) among the candles that align with the original MQL window configuration.
   - A secondary extreme that is shifted deeper into history. The distance between the primary band and this secondary extreme is used together with the `ReversePips` threshold to confirm strong reversals.
3. **Trend flips** – When the previous close moves outside the ATR band or the secondary extreme difference exceeds the reversal distance, the bias switches (from long to short or vice versa). If an opposite position exists it is closed first, otherwise a new position in the new direction is opened immediately.
4. **Waiting for flat** – After issuing an opposite market order to close an existing position the strategy waits until the portfolio is flat before submitting the new entry order. This behaviour mirrors the original expert advisor.
5. **Risk management** – Stop loss, take profit and trailing stop levels are defined in pips and converted to absolute prices using an adjusted point value (compatible with 3 and 5 decimal forex symbols). Trailing updates require price progress greater than `TrailingStopPips + TrailingStepPips`, matching the MT5 logic.

## Parameters

- `CandleType` – Primary timeframe to subscribe to for price data.
- `AtrPeriod` – ATR averaging length used in the band calculation.
- `VolatilityMultiplier` – Multiplier applied to the ATR value to size the offset from the extreme.
- `ReversePips` – Additional pip-based distance that must be exceeded by the secondary extreme before the bias flips.
- `StopLossPips` – Protective stop distance in pips from the entry price (set to zero to disable).
- `TakeProfitPips` – Profit target distance in pips from the entry price (set to zero to disable).
- `TrailingStopPips` – Trailing stop activation distance measured in pips (set to zero to disable trailing).
- `TrailingStepPips` – Extra pip distance required before trailing updates occur; must be positive when trailing is active.
- `TradeVolume` – Order volume used for new entries (in lots/contracts depending on the security settings).

## Notes

- The indicator computations and reversal checks only use finished candles; incomplete candles are ignored.
- The ATR value supplied by the binding is equivalent to the previous-bar ATR used in the source EA because calculations occur after candle completion.
- The adjusted point calculation automatically handles 3- and 5-decimal forex quotes to keep pip-based parameters compatible with the original script.
- No Python port is provided by request. The folder currently contains only the C# implementation and documentation.
