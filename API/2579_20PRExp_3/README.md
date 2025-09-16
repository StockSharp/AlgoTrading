# 20PRExp-3 Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The 20PRExp-3 strategy is a breakout system that compares the current trading session with the previous day's price extremes. It rebuilds the daily channel on every completed five-minute candle, confirms momentum with a 30-minute tick volume expansion, and enters only when price breaks beyond the updated session high or low. Once in the market, it mirrors the original MetaTrader 5 expert by using Parabolic SAR exits, dynamic trailing stops, and fixed risk sizing based on the distance to the protective stop.

## Concept
- **Daily channel**: Track the running high, low, and midpoint of the current trading day.
- **Breakout confirmation**: Require price to close beyond the channel boundary with a minimum daily range filter (`GapPoints`).
- **Volume expansion**: Compare the last two completed 30-minute candles and demand at least a 1.5× increase in tick volume to avoid thin breakouts.
- **Time filter**: Allow new positions only after the configured session start hour (`SessionStartHour`) to avoid the low-liquidity overnight range.
- **Risk symmetry**: Long trades use the daily low as stop loss, short trades use the daily high. Take profit and trailing offsets are measured in price points.

## Market Data
- Five-minute candles for the primary signal and Parabolic SAR calculation.
- Thirty-minute candles for the tick volume ratio filter.
- Daily high/low statistics are derived on the fly from the five-minute data, so no separate daily subscription is required.

## Entry Logic
1. Wait for a finished five-minute candle after the configured start hour.
2. Compute the current day's high/low/mid and channel width.
3. Check that the channel width exceeds `GapPoints * PriceStep`.
4. Calculate the tick volume ratio = (last completed 30-minute volume) / (previous 30-minute volume) and ensure it is greater than 1.5.
5. **Long setup**: the candle closes at or above the current daily high → buy.
6. **Short setup**: the candle closes at or below the current daily low → sell.
7. Skip new entries while a position is active (one open trade maximum).

## Exit Management
- **Initial stop**: long trades use the daily low, short trades use the daily high captured at entry.
- **Take profit**: optional; placed `TakeProfitPoints * PriceStep` away from entry on both sides of the market.
- **Parabolic SAR reversal**: closes the position if the SAR value crosses the previous candle close (original EA behaviour).
- **Trailing stop**: activates once profit exceeds `TrailingStopPoints * PriceStep` and moves by at least `TrailingStepPoints * PriceStep`.
- **Mirror trailing take**: whenever the trailing stop is updated, the take-profit level is repositioned symmetrically around the current close.

## Position Sizing
- Position volume is derived from `RiskPercent`: the strategy risks a percentage of the portfolio's current value based on the distance between entry and stop.
- If portfolio valuation is unavailable, the algorithm falls back to `Volume + |Position|` and, as a last resort, trades a single contract.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | 5-minute candles | Primary timeframe for signals and Parabolic SAR. |
| `VolumeCandleType` | 30-minute candles | Timeframe used to evaluate tick volume expansion. |
| `TakeProfitPoints` | 20 | Profit target distance expressed in price points. Set to 0 to disable. |
| `TrailingStopPoints` | 10 | Distance in points for the trailing stop activation. |
| `TrailingStepPoints` | 10 | Minimum additional progress (in points) before the trailing stop is moved again. |
| `RiskPercent` | 5 | Percentage of portfolio equity risked per trade. |
| `GapPoints` | 50 | Minimum daily channel width in points required to enable a breakout. |
| `SessionStartHour` | 7 | Hour (0–23) after which the strategy is allowed to open new positions. |

## Notes
- The Parabolic SAR parameters (step 0.005, max 0.01) match the original MQL strategy.
- Daily midpoint values are calculated for completeness and can be plotted for visual reference if desired.
- Because volume expansion is evaluated on completed 30-minute candles, the breakout confirmation uses the latest available close-to-close information, which is robust for both historical tests and live trading.
- All in-code comments are in English to align with repository guidelines.
