# Exp Sinewave2 X2 Strategy

## Overview
Exp Sinewave2 X2 is a multi-timeframe trend-following strategy inspired by John Ehlers' Sinewave analysis. The higher timeframe filter defines the dominant direction, while the lower timeframe provides precise entry and exit triggers. All calculations use the reconstructed Sinewave2 indicator, which internally relies on the adaptive CyclePeriod module.

## Indicators
- **Higher timeframe Sinewave2 (lead vs. sine line)** – detects bullish or bearish bias using the lead sine cross over the main sine component.
- **Lower timeframe Sinewave2** – monitors the most recent crossover events to trigger trades aligned with the higher timeframe direction.

## Trading Logic
1. **Trend filter**
   - Compute Sinewave2 on the higher timeframe.
   - Evaluate the lead and main lines `SignalBarHigh` bars back.
   - Trend is bullish if `Lead > Sine`, bearish if `Lead < Sine`, otherwise neutral.
2. **Entry signals**
   - Wait for a finished candle on the lower timeframe.
   - Retrieve lead and sine values at offsets defined by `SignalBarLow` (current) and `SignalBarLow + 1` (previous).
   - Long entry: previous crossover was downward (`Lead > Sine` previously, `Lead <= Sine` now) while the higher timeframe trend is bullish and `EnableBuyOpen` is enabled.
   - Short entry: previous crossover was upward (`Lead < Sine` previously, `Lead >= Sine` now) while the higher timeframe trend is bearish and `EnableSellOpen` is enabled.
3. **Exit rules**
   - Lower timeframe exit booleans `EnableBuyCloseLower` and `EnableSellCloseLower` close positions on opposing crossovers.
   - Higher timeframe exit booleans `EnableBuyCloseTrend` and `EnableSellCloseTrend` close positions immediately whenever the major trend flips against the open direction.
   - Protective stop loss and take profit are evaluated each candle using intrabar highs/lows and the `StopLossPoints` / `TakeProfitPoints` distances expressed in price steps.
4. **Risk management**
   - Position reversals size new orders as `Volume + |Position|` to flatten the existing position before establishing the new one.
   - After each entry `SetRiskLevels` recalculates absolute stop/target prices using `Security.PriceStep` (fallback 1 when unavailable).

## Parameters
| Name | Description |
| --- | --- |
| `AlphaHigh` | Alpha factor for the higher timeframe Sinewave2 filter. |
| `AlphaLow` | Alpha factor for the lower timeframe Sinewave2 trigger. |
| `SignalBarHigh` | Number of bars back on the higher timeframe used to read the trend state. |
| `SignalBarLow` | Number of bars back on the lower timeframe used to read crossover states. |
| `EnableBuyOpen` / `EnableSellOpen` | Allow long/short entries from lower timeframe signals. |
| `EnableBuyCloseTrend` / `EnableSellCloseTrend` | Force exits when the higher timeframe flips against the position. |
| `EnableBuyCloseLower` / `EnableSellCloseLower` | Close positions on lower timeframe opposite crossovers. |
| `StopLossPoints` | Stop-loss distance expressed in instrument price steps. |
| `TakeProfitPoints` | Take-profit distance expressed in instrument price steps. |
| `HigherCandleType` / `LowerCandleType` | Candle data types (timeframes) for the filter and trigger streams. |

## Notes
- The strategy processes only finished candles and ignores partial updates.
- The adaptive Sinewave2 implementation uses the original CyclePeriod algorithm to remain faithful to the MQL version.
- When the higher and lower candle types are identical, both indicators share a single candle subscription to avoid redundant data requests.
- Adjust `Volume` in the base `Strategy` to control trade size before deployment.
