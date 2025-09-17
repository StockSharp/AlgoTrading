# VHF Sliding Windows Strategy

## Overview
- Converted from the MetaTrader 5 expert advisor **"VHF EA"** by Vladimir Karputov.
- Uses the Vertical Horizontal Filter (VHF) indicator to classify the market regime as trending or ranging.
- Works on any instrument and timeframe supported by StockSharp; simply change the candle type parameter to match the desired chart.

## Trading Logic
1. Subscribe to the selected candle series and compute the VHF indicator with period `VhfPeriod` on every finished candle.
2. Maintain two sliding windows of recent VHF values:
   - **Main Window (`MainWindowSize`)** – establishes the overall VHF range and mid-point.
   - **Working Window (`WorkingWindowSize`)** – detects short-term breaks above or below the local VHF median.
3. A bullish or bearish trend regime is confirmed only when the current VHF value is greater than the mid-point of both windows.
4. While in trend regime, compare the latest closing price with the close `MainWindowSize` bars ago:
   - Close higher than the reference → default behaviour is to open/maintain a long position.
   - Close lower than the reference → default behaviour is to open/maintain a short position.
   - Enable `ReverseSignals` to invert these directions.
5. The strategy closes any open position whenever the VHF value falls back inside the ranging zone (current VHF is not above both mid-points).
6. Position flips are handled by buying/selling enough volume to both close the opposite side and open the new position in a single market order.

## Parameters
| Parameter | Description | Default | Notes |
|-----------|-------------|---------|-------|
| `MainWindowSize` | Number of VHF values in the primary sliding window. | `11` | Must be greater than `WorkingWindowSize`. |
| `WorkingWindowSize` | Number of VHF values in the secondary window. | `7` | Provides faster confirmation of breakouts. |
| `VhfPeriod` | Lookback period of the Vertical Horizontal Filter. | `9` | Determines sensitivity of the indicator. |
| `Volume` | Order volume (lots) used for new entries. | `1` | Added to the absolute current position when flipping direction. |
| `ReverseSignals` | Invert the long/short logic derived from price direction. | `true` | Matches the default behaviour of the original EA. |
| `CandleType` | Timeframe and candle type for data subscription. | `15 minute time frame` | Change to adapt the strategy to other charts. |

## Money Management and Exits
- The strategy always trades a fixed volume defined by `Volume`.
- Protective stop management is delegated to StockSharp's built-in `StartProtection()` helper, which safely closes unexpected leftover positions.
- No stop-loss or take-profit targets are coded; exits rely on the regime switch detected by VHF.

## Implementation Notes
- Uses the high-level candle subscription API with indicator binding, following the project guidelines.
- A custom Vertical Horizontal Filter indicator identical to the MQL version is embedded in the strategy.
- Logging statements describe every position change and regime transition for easier debugging.
