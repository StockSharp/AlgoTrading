# Bull & Bear Candle Martingale Strategy

## Overview
The strategy reacts to strong bullish and bearish candles and opens market positions in the same direction. It uses an independent martingale sequence for each side: long positions scale the volume with the *Bull Multiplier*, while short positions use the *Bear Multiplier*. Protective stop-loss and take-profit distances are also configured separately for each direction, allowing precise control over the asymmetrical behaviour that the original MQL expert advisor exposes.

## Trading Logic
1. Subscribe to the configured candle type (default: 1 minute) and wait for completed candles only.
2. When there is no open position:
   - **Bullish setup:** if `Close > Open` and the candle body size exceeds the bullish body filter, buy at market.
   - **Bearish setup:** if `Close < Open` and the body size exceeds the bearish body filter, sell at market.
3. Each entry sets stop-loss and take-profit orders converted from pip distances to the instrument price step.
4. When a position closes, the realised PnL is compared with the previous baseline:
   - A negative result multiplies the respective martingale volume.
   - A positive or break-even result resets that side to the initial volume.
5. New signals are ignored while a position is open, reproducing the single-trade behaviour of the source EA.

## Money Management
- Long and short martingale cycles are tracked independently, so a losing long sequence will not affect the next short trade, and vice versa.
- Volumes are aligned with the security `VolumeStep` to avoid rejected orders.
- `StartProtection(useMarketOrders: true)` enables StockSharp protective order handling for the attached stop and take levels.

## Parameters
| Parameter | Description |
|-----------|-------------|
| **Initial Volume** | Base volume that starts each martingale cycle for both directions. |
| **Bull Multiplier** | Multiplier applied to the next bullish trade after a losing long position. |
| **Bear Multiplier** | Multiplier applied to the next bearish trade after a losing short position. |
| **Bull Stop Loss** | Stop-loss distance in pips for bullish trades. Converted to price using the instrument step. |
| **Bull Take Profit** | Take-profit distance in pips for bullish trades. |
| **Bear Stop Loss** | Stop-loss distance in pips for bearish trades. |
| **Bear Take Profit** | Take-profit distance in pips for bearish trades. |
| **Bull Body Filter** | Minimum bullish candle body in pips required to trigger a buy order. |
| **Bear Body Filter** | Minimum bearish candle body in pips required to trigger a sell order. |
| **Candle Type** | Time frame used for signal generation (default: 1-minute time frame). |

## Usage Notes
- Ensure that the connected security exposes valid `PriceStep` and `VolumeStep` values. The strategy defaults to 0.0001 when `PriceStep` is not provided.
- The martingale logic relies on realised PnL, so manual position closing will still update the sequence correctly.
- Optimisation can focus on body filters and multiplier combinations to balance responsiveness versus drawdown.
