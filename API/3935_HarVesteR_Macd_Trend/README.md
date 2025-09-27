# HarVesteR MACD Trend Strategy

## Overview
The HarVesteR strategy is a trend-following system converted from the original MetaTrader advisor. It combines MACD momentum confirmation with two simple moving averages that define the trend direction and manage trailing exits. An optional ADX filter keeps trading activity focused on strong directional moves.

The default configuration mirrors the published Expert Advisor: MACD(12, 24, 9), a 50-period management SMA, a 100-period trend filter SMA, and a staged take-profit that halves the position once price travels twice the initial risk.

## Trading Logic
1. **Trend bias** – The 100-period SMA acts as a directional gate. Price closing below it arms the long setup, while closing above it arms the short setup. Once a trade is taken, the flag is reset until price crosses back to the opposite side, preventing consecutive entries without a pullback.
2. **MACD confirmation** – A signal is valid only if the MACD line is on the expected side of zero and was on the opposite side at least once within the last *Confirmation Bars* candles. This replicates the original loop that searched for a sign change inside a sliding window.
3. **Entry conditions** – Long trades require the candle close plus the configured offset (in price points) to be above both SMAs, MACD to be positive, and (if enabled) ADX to exceed 50. Short trades use the mirror logic with negative MACD and price below both SMAs.
4. **Initial stop** – The stop-loss is anchored at the lowest (for longs) or highest (for shorts) price of the last *Stop Bars* completed candles, matching the MQL `iLowest`/`iHighest` calls with a shift of one bar.
5. **Position management** – When price travels a distance equal to *Risk Multiplier* times the initial risk, half of the position is closed and the stop is moved to breakeven. The remaining half exits when price retreats enough for the 50-period SMA to cross above (long) or below (short) the offset-adjusted close.
6. **Protective exit** – Any candle that pierces the stored stop price immediately closes the entire position.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `Fast EMA` | Short EMA period used inside the MACD calculation. | 12 |
| `Slow EMA` | Long EMA period used inside the MACD calculation. | 24 |
| `Signal EMA` | Smoothing period for the MACD signal line. | 9 |
| `MACD Confirmation Bars` | Maximum candles between opposite MACD readings required before a new entry. | 6 |
| `Trend SMA` | Length of the management SMA that guards trailing exits. | 50 |
| `Filter SMA` | Length of the directional SMA used to arm long/short setups. | 100 |
| `Offset (points)` | Offset (in instrument points) added or subtracted when comparing price with the SMAs. | 10 |
| `Stop Bars` | Number of past candles considered when setting the initial stop. | 6 |
| `Risk Multiplier` | Multiplier applied to the initial risk distance to trigger the partial take-profit. | 2.0 |
| `Use ADX` | Enables the ADX>50 trend-strength filter. | Disabled |
| `ADX Period` | ADX lookback used when the filter is active. | 14 |
| `Candle Type` | Candle series supplied to the indicators (defaults to 1-hour bars). | 1H time-frame |

## Implementation Notes
- Price offsets are translated into absolute prices via `Security.Step` (or `Security.PriceStep` when available). If the security does not expose a step the strategy falls back to `0.0001`, matching the behaviour of the original FX-focused advisor.
- Partial exits use market orders sized to half of the current position, mirroring the lot reduction performed in the source MQL implementation.
- `StartProtection()` is enabled to ensure the built-in position guard is active before new trades are placed.
- The ADX filter is optional; when disabled the algorithm behaves exactly like the historical script by substituting an artificial value of 60 for ADX.

## Usage Tips
1. Configure the `Volume` property before starting the strategy; it defines the base order size used during entries and partial exits.
2. Align the `Candle Type` with your preferred timeframe. The original strategy was tuned on hourly data but shorter frames can be explored through parameter optimisation.
3. Optimising `MACD Confirmation Bars`, `Offset (points)`, and `Risk Multiplier` typically has the largest impact on win rate and trade frequency.
