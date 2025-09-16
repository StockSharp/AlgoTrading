# AbsolutelyNoLagLWMA Digit MMRec Strategy

## Concept

This strategy is a StockSharp port of the MetaTrader expert *Exp_AbsolutelyNoLagLwma_Digit_NN3_MMRec*. It keeps the original multi-timeframe architecture built around the "AbsolutelyNoLagLWMA" indicator and reproduces the money-management recovery rules (`MMRec`). Three independent modules (A/B/C) monitor 12-hour, 4-hour and 2-hour candles respectively. Every module can open and close its own position slice while the strategy tracks the combined exposure.

Each module computes a double weighted moving average (WMA of a WMA) of a configurable price source. The smoothed value is rounded to the requested number of digits, exactly as in the MQL indicator. A change in the slope of the smoothed line (value rises after falling or vice versa) is treated as a direction switch and generates trading actions for that module.

## Trading Rules

1. Wait for a finished candle from the module timeframe.
2. Read the applied price (close, open, median, typical, etc.).
3. Process the price through the primary WMA and feed its result into a secondary WMA to emulate "AbsolutelyNoLagLWMA".
4. Round the smoothed value to the configured number of digits and compare it with the previous value.
5. **Upward slope** (`value > previous`):
   - Close the module short leg if short exits are enabled.
   - If long entries are enabled and no long exposure is active, open a long position using the current module volume.
   - Recalculate stop-loss and take-profit levels (expressed in price steps) for the long slice.
6. **Downward slope** (`value < previous`):
   - Close the module long leg if long exits are enabled.
   - If short entries are enabled and no short exposure is active, open a short position.
   - Update the protective levels for the short slice.
7. On every candle the module checks whether the candle's high/low pierced the current stop-loss or take-profit level. If touched, the position slice is flattened at that price and the trade result is recorded for the money-management logic.
8. Money management keeps a queue of the most recent trade results for each direction. When the last *N* trades (where *N* equals the loss trigger) were losing, the next order uses the reduced volume; otherwise the normal volume is used. Losing trade detection is based on the entry price that was stored when the slice was opened and the exit price (stop/target/close) used to flatten it.

The strategy uses market orders for entries and exits and assumes fills at the candle close for signals and at the protective price for stop/target exits.

## Parameters

Every module owns an identical set of parameters. Defaults correspond to the source MQL expert.

| Parameter | Description |
|-----------|-------------|
| `ACandleType` / `BCandleType` / `CCandleType` | Time frame of the module candles (12h / 4h / 2h by default). |
| `ALength` / `BLength` / `CLength` | Length of the AbsolutelyNoLagLWMA smoothing (applied to both WMAs). |
| `AAppliedPrice` / `BAppliedPrice` / `CAppliedPrice` | Price source used in the indicator (close, open, high, low, median, typical, weighted, simple, quarter, TrendFollow1, TrendFollow2, Demark). |
| `ADigits` / `BDigits` / `CDigits` | Number of digits to round the smoothed value. |
| `ABuyOpen`, `ASellOpen`, `ABuyClose`, `ASellClose` (and module B/C equivalents) | Flags controlling whether the module is allowed to open/close long or short slices. |
| `ASmallVolume`, `ANormalVolume` | Reduced and normal order volumes. The same values are reused for short trades. |
| `ABuyLossTrigger`, `ASellLossTrigger` | Number of consecutive losing trades that activates the reduced volume for longs/shorts. |
| `AStopLossPoints`, `ATakeProfitPoints` | Protective levels expressed in price steps for the module slice. Identical parameters exist for modules B and C. |

The money-management queues are reset when the corresponding trigger is set to zero. The price-step calculations rely on `Security.Step`; if the security does not expose it, a step of `1` is used.

## Notes

- Each module manages its own internal position volume; therefore, different modules can be long or short simultaneously. The main strategy position is the sum of all module slices.
- Stop-loss and take-profit levels are checked on every finished candle using the candle's high/low to detect breaches.
- The `AppliedPrice` enumeration matches the original indicator options, including both TrendFollow formulas and the DeMark variant.
- The strategy does not add indicators to the chart; it relies on the high-level `Bind` API and keeps indicator instances private to each module as required by the guidelines.
- The logic closes and opens trades only when the slope changes direction, which prevents duplicated orders on consecutive bars with the same trend state.
