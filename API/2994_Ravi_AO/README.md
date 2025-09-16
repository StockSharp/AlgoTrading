# RAVI + Awesome Oscillator Strategy

## Overview
- Port of the MetaTrader 5 expert advisor "Ravi AO (barabashkakvn's edition)" to the StockSharp high-level API.
- Combines the Range Action Verification Index (RAVI) oscillator with the Awesome Oscillator (AO) to detect synchronized bullish and bearish momentum shifts.
- Works on any timeframe and instrument supported by StockSharp; all numeric settings are expressed in pips to stay close to the original implementation.

## Indicators
- **RAVI** – computed as `100 * (FastMA - SlowMA) / SlowMA` on the selected price series. You can choose the smoothing method (simple, exponential, smoothed, weighted), lengths, and price source (close, open, high, low, median, typical, weighted, simple, quarter, trend-follow, Demark).
- **Awesome Oscillator** – median-price momentum indicator with configurable short and long periods. The defaults match the MT5 values (5 and 34).

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Candle timeframe or data type to subscribe to. |
| `StopLossPips` | Protective stop-loss distance in pips. `0` disables the stop. |
| `TakeProfitPips` | Take-profit distance in pips. `0` disables the take profit. |
| `TrailingStopPips` | Base trailing-stop distance in pips. `0` disables trailing. |
| `TrailingStepPips` | Minimal additional profit (in pips) required before the trailing stop is tightened. Must be > 0 when trailing is enabled. |
| `FastMethod` / `FastLength` | Smoothing method and length of the fast RAVI moving average. |
| `SlowMethod` / `SlowLength` | Smoothing method and length of the slow RAVI moving average. |
| `AppliedPrice` | Price formula used by both RAVI averages (close, open, high, low, median, typical, weighted, simple, quarter, trend-follow #1/#2, Demark). |
| `AoShortPeriod` / `AoLongPeriod` | Fast and slow periods of the Awesome Oscillator. |

## Trading Rules
1. Strategy updates indicators when a candle closes (`CandleStates.Finished`).
2. A **bullish entry** is triggered when:
   - AO two bars ago `< 0` and AO one bar ago `> 0` (positive zero-cross), and
   - RAVI two bars ago `< 0` and RAVI one bar ago `> 0`.
3. A **bearish entry** is triggered when:
   - AO two bars ago `> 0` and AO one bar ago `< 0`, and
   - RAVI two bars ago `> 0` and RAVI one bar ago `< 0`.
4. Only one position can be open at a time. Signals are ignored while a position exists.

## Exit Management
- **Stop-loss**: computed from `StopLossPips` using the instrument price step (5- and 3-digit FX symbols use a 10× multiplier, matching MT5 pip definition). Triggered when candle extremes touch the stop level.
- **Take-profit**: optional target calculated the same way; disabled when `TakeProfitPips = 0`.
- **Trailing stop**: when enabled, the stop is tightened once the floating profit exceeds `TrailingStopPips + TrailingStepPips`. For longs the stop moves to `ClosePrice - TrailingStopPips`; for shorts to `ClosePrice + TrailingStopPips`.
- All exits close the full position with market orders.

## Implementation Notes
- Signals are evaluated on bar close; actual entries occur on the same candle close, whereas the MT5 version enters on the next bar open. Adjust settings if you need to compensate for this difference.
- Only StockSharp-provided moving averages are used; exotic smoothing modes from the MT5 library (JJMA, Jurik, T3, etc.) are not available.
- The MT5 indicator's visual `Shift` parameter affects only plotting; it has no trading impact and is therefore omitted.
- `AppliedPrice` formulas follow the MetaTrader definitions, including TrendFollow and Demark options.

## Usage Tips
- The strategy is trend-following; combine it with higher-timeframe filters or volatility filters to reduce whipsaws.
- Optimize lengths and pip distances per instrument, especially when switching between FX, CFDs, and futures, because pip size is derived from `Security.PriceStep`.
- Enable `Strategy.StartProtection` externally if you want broker-side stop orders instead of in-strategy exits.
