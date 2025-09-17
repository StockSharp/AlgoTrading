# BullsBearsEyes EA Strategy

## Overview
This strategy is a StockSharp port of the **BullsBearsEyes EA** for MetaTrader 5. It rebuilds the custom indicator by combining the classic Bulls Power and Bears Power oscillators with the same four stage IIR smoothing used in the original code. The resulting ratio oscillates between 0 and 1 and reflects the dominance of sellers or buyers. Whenever the ratio collapses to **0** the market is considered washed out by bears and the strategy prepares a long entry. When the ratio spikes to **1** the bullish pressure is considered exhausted and the strategy looks for a short entry. All calculations are performed on fully closed candles only, mirroring the MQL implementation that evaluated `custom[1]` on the birth of each new bar.

## Trading Logic
1. Subscribe to the configured candle series and bind Bulls Power and Bears Power indicators.
2. On every finished candle the indicator values are fed through the same IIR smoothing cascade (`L0` – `L3`) as the original EA.
3. The ratio `CU / (CU + CD)` is computed. A pure bearish sequence makes `CU` equal to zero while a pure bullish sequence forces `CD` to zero.
4. The strategy stores the ratio of the previous candle and uses it as the actionable signal:
   - Previous ratio equal to **0** ⇒ close short positions and open a long position.
   - Previous ratio equal to **1** ⇒ close long positions and open a short position.
   - Intermediate ratios are ignored to stay faithful to the source code.
5. Orders are sent with the current `Volume` value and automatically net out the opposite position before opening a new one.

## Risk Management
- **Stop Loss / Take Profit** – expressed in pips, translated to absolute prices with pip size detection identical to the MT5 implementation (5- and 3-digit instruments are handled via the step multiplier).
- **Trailing Stop / Trailing Step** – identical logic: once the price advances by `TrailingStop + TrailingStep` the stop is moved to keep a constant `TrailingStop` distance from the current close. Long and short positions are managed symmetrically.
- Protective levels are recalculated whenever the net position changes, ensuring the average position price is used for further calculations.
- The strategy closes the entire position whenever a protective level is breached inside the current candle range.

## Session Filter
The optional time filter matches the expert advisor inputs:
- `Use Time Control` – enables/disables the filter.
- `Start Hour` – inclusive starting hour (0–23).
- `End Hour` – exclusive ending hour (0–23). If the end hour is less than the start hour the session wraps over midnight.
During restricted hours the strategy refrains from opening new positions but still manages stops and trailing for existing trades.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `Period` | Averaging length for Bulls/Bears Power. | 13 |
| `Gamma` | Smoothing factor used by the four stage filter (0–1). | 0.6 |
| `StopLossPips` | Stop-loss distance measured in pips. | 150 |
| `TakeProfitPips` | Take-profit distance measured in pips. | 150 |
| `TrailingStopPips` | Trailing stop distance in pips (0 disables trailing). | 25 |
| `TrailingStepPips` | Minimal advance before the trailing stop can move. | 5 |
| `UseTimeControl` | Enables the trading session filter. | true |
| `StartHour` | First trading hour (inclusive). | 10 |
| `EndHour` | Last trading hour (exclusive). | 16 |
| `CandleType` | Candle type/timeframe used for analysis. | 1-hour candles |

## Additional Notes
- High level `SubscribeCandles().Bind(...)` API is used to mirror the original calculations without manually collecting candles.
- Indicator values are processed only after the candle closes (`CandleStates.Finished`).
- Pip size detection falls back to `1` if the security step is unavailable, allowing the strategy to run in synthetic testing environments.
- Inline comments in the C# file explain each logical section for easier maintenance and comparison with the MQL source.
