# One MA Channel Breakout Strategy

## Overview
The **One MA Channel Breakout Strategy** replicates the MetaTrader 5 expert advisor *One MA EA* using StockSharp's high-level strategy API. The system draws a shifted moving average and surrounds it with a configurable pip-based channel. When price opens outside of the channel after probing it on the same bar, the strategy opens a position in the breakout direction while optional stop-loss and take-profit protections manage risk automatically.

Key characteristics:
- Supports multiple moving-average calculation methods (SMA, EMA, SMMA, LWMA).
- Allows choosing the candle price (close, open, high, low, median, typical, weighted) that feeds the moving average.
- Applies independent shifts to the moving-average value and to the candle used for signal evaluation, matching the original EA's `Current Bar` controls.
- Converts pip distances to absolute price increments using the instrument `PriceStep` and decimal precision (3/5 decimal instruments map to classic FX pips automatically).

## Trading Logic
1. **Indicator preparation**
   - A moving average with period `MaPeriod`, method `MaMethodParam`, shift `MaShift`, and applied price `AppliedPriceType` is calculated from the subscribed candle series (`CandleType`).
   - Channel offsets are converted from pips to price increments: `ChannelHighPips` above and `ChannelLowPips` below the shifted moving average.
   - Historical buffers allow referencing earlier bars (`MaBarShift` for the MA series, `PriceBarShift` for OHLC data) exactly as in the MQL version.

2. **Signal generation**
   - **Bullish breakout**: the inspected candle's low stays between the MA baseline and the upper channel, while its open prints above the upper channel. If no long exposure exists (`Position <= 0`), the strategy buys.
   - **Bearish breakout**: the inspected candle's high remains between the MA baseline and the lower channel, while its open appears below the lower channel. If no short exposure exists (`Position >= 0`), the strategy sells.
   - Order volume equals the configured `TradeVolume` plus any quantity required to flatten an opposite position, mirroring the hedge-to-net behavior of the source expert.

3. **Risk management**
   - `StopLossPips` and `TakeProfitPips` are translated into absolute price distances and passed to `StartProtection`, enabling automated exit orders for every position.
   - With zero values the respective protective order is disabled.

No additional exit logic is applied; positions close only via the protection module or by reversing into the opposite signal.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `MaPeriod` | Moving average length. Must be > 0. |
| `MaShift` | Horizontal shift of the moving average in bars. Positive values displace the MA to the right. |
| `MaMethodParam` | Moving average calculation type (`Sma`, `Ema`, `Smma`, `Lwma`). |
| `AppliedPriceType` | Candle price fed into the MA (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). |
| `MaBarShift` | Which historical MA value to use (0 = current processed bar). |
| `PriceBarShift` | Which historical candle to inspect for OHLC values. |
| `ChannelHighPips` | Distance (in pips) from the MA to the upper channel boundary. |
| `ChannelLowPips` | Distance (in pips) from the MA to the lower channel boundary. |
| `StopLossPips` | Protective stop distance in pips. Zero disables the stop. |
| `TakeProfitPips` | Profit target distance in pips. Zero disables the target. |
| `TradeVolume` | Order size in strategy volume units (mapped to `Strategy.Volume`). |
| `CandleType` | Candle data series used for calculations and signals. |

## Implementation Notes
- Pip to price conversion uses `PriceStep` and `Decimals`. For symbols with 3 or 5 decimals the pip value equals `PriceStep * 10`, otherwise it equals `PriceStep`.
- Historical buffers are implemented with fixed-size sliding windows so the strategy can access bars by index without relying on indicator `GetValue` calls, complying with the project guidelines.
- The strategy relies solely on finished candles; unfinished candles are ignored to avoid premature signals.
- Optional chart rendering draws price candles and executed trades when a chart area is available in the host application.

## Usage Tips
- Ensure the subscribed security exposes valid `PriceStep`/`Decimals` data; otherwise adjust pip-based parameters manually.
- Optimize `MaPeriod`, channel distances, and bar shifts to adapt the breakout behavior to specific markets or timeframes.
- Combine with portfolio-level risk controls when deploying live, as the strategy is always-in/out with a single position per instrument.
