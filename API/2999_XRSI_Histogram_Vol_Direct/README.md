# XRSI Histogram Vol Direct Strategy

## Overview
- **Original source**: `Exp_XRSI_Histogram_Vol_Direct.mq5`
- **Converted platform**: StockSharp C# high level strategy API
- **Idea**: trade reversals when the smoothed volume-weighted RSI histogram changes slope
- **Data**: single security, single timeframe (default H4)

The strategy evaluates a custom oscillator built from RSI values multiplied by volume. When the slope of this smoothed oscillator flips, the strategy either reverses a position or opens a fresh trade in the opposite direction. The logic replicates the color-buffer approach of the original expert advisor by tracking the slope direction of the last two completed candles.

## Indicator stack and calculations
1. **RSI** (`RsiPeriod`) is calculated on the selected candle series and centered around zero by subtracting 50.
2. **Volume selection** uses either tick count or traded volume, controlled by the `Use Tick Volume` parameter.
3. **Volume-weighted oscillator** multiplies the centered RSI by the chosen volume, magnifying swings that coincide with larger activity.
4. **Smoothing** applies the selected moving average (`SMA`, `EMA`, `SMMA`, `WMA`) with period `SmoothLength` to both the oscillator and raw volume stream. The indicator is considered ready only after both smoothed values are formed.
5. **Slope detection** compares the current smoothed oscillator value with the previous one:
   - Higher value → slope color `0` (rising)
   - Lower value → slope color `1` (falling)
   - Flat → keep the previous color

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| Candle Type | H4 time frame | Target candle subscription. |
| RSI Period | 14 | Lookback for the RSI calculation. |
| Smoothing Length | 12 | Period of the moving average applied to both the oscillator and volume. |
| Smoothing Method | SMA | Moving average type (`SMA`, `EMA`, `SMMA`, `WMA`). |
| Use Tick Volume | `true` | Use tick count (`true`) or traded volume (`false`). |
| Allow Buy Open | `true` | Enable opening long positions. |
| Allow Sell Open | `true` | Enable opening short positions. |
| Allow Buy Close | `true` | Allow closing long positions on opposite signal. |
| Allow Sell Close | `true` | Allow closing short positions on opposite signal. |

> **Note**: Unlike the original MQL indicator, advanced smoothers such as JJMA or VIDYA are not available in the StockSharp framework. The strategy therefore exposes the closest built-in alternatives.

## Trading rules
1. Wait until both smoothing indicators have enough data.
2. Determine the slope color of the last two completed candles.
3. **If the older color is rising (`0`)**:
   - Close any open short position if allowed.
   - If the latest color is falling (`1`) and long entries are allowed, open a long position (mirrors the reversal logic from the EA).
4. **If the older color is falling (`1`)**:
   - Close any open long position if allowed.
   - If the latest color is rising (`0`) and short entries are allowed, open a short position.

The strategy effectively trades the “color flip” of the histogram slope, executing at the close of the newest finished candle.

## Practical tips
- The logic is sensitive to the chosen timeframe. Test several intervals to match the behaviour of the original EA.
- Because only slope direction is used, adding a stop loss or take profit via `StartProtection` can improve risk control in live trading.
- Use chart visualization in the terminal to compare the StockSharp oscillator slope with the original MT5 indicator when validating the port.

## Differences from the MQL version
- Money management helpers (`TradeAlgorithms.mqh`) are not ported; the StockSharp implementation relies on the base strategy volume.
- Only the smoothing methods supported by StockSharp are exposed. Unsupported modes default to SMA behaviour.
- Orders are sent immediately on the finished candle, so explicit time shifting (`SignalBar` / `TimeShiftSec`) is not required.
- Protective stops are not hard-coded; users can add them through `StartProtection` if needed.

## Limitations
- Requires a candle source that provides either tick counts or volume totals to reproduce the oscillator amplitude correctly.
- The strategy does not draw the custom histogram itself; it focuses on the trading logic and optional chart overlays for RSI.
