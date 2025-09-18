# MultiTimeframeEmaAlignmentStrategy

## Overview
The **MultiTimeframeEmaAlignmentStrategy** is a StockSharp port of the MetaTrader 4 expert advisor `1h-4h-1d.mq4` from the folder `MQL/7713`. The original robot aligns fast and slow exponential moving averages across three timeframes and applies protective money management via fixed stop loss, take profit and trailing stop levels. This C# version follows the same high-level idea while leveraging StockSharp's indicator bindings and high-level order helpers.

## Trading Logic
- The strategy subscribes to three candle series simultaneously: M1 (signal timeframe), M5 (mid-term filter) and M30 (higher timeframe trend confirmation).
- Each series feeds a pair of exponential moving averages (EMA) with configurable lengths (default 8 and 64).
- A **bullish setup** requires the fast EMA to stay above the slow EMA on all three timeframes. Additionally, the fast EMA must not lose momentum (current value greater than or equal to the previous value and also above the value `ShiftDepth` bars ago).
- A **bearish setup** requires the fast EMA to stay below the slow EMA on all three timeframes with the fast EMA decreasing in momentum.
- Orders are triggered on the close of the M1 candle when the alignment and momentum checks are satisfied. Long signals are allowed only when no long position is open (or an existing short is closed first) and vice versa.

This interpretation recreates the intent of the MT4 conditions with StockSharp's high-level API. The MQL "MA shift" comparisons are emulated through the `ShiftDepth` buffer that tracks EMA values a few candles back and ensures momentum is consistent with the entry direction.

## Risk Management
- Position size is controlled by the `TradeVolume` parameter (default 3 lots like the original EA).
- Optional stop loss and take profit distances are provided in pips. They are converted to prices through the instrument's `PriceStep` (falls back to `0.0001` when missing).
- The trailing stop replicates the EA's behaviour by moving the stop price closer to the market whenever the trade advances enough.
- Risk parameters can be toggled independently, matching the `StopLossMode`, `TakeProfitMode` and `TrailingStopMode` flags from the MQL script.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `TradeVolume` | Order volume used by `BuyMarket` / `SellMarket`. Mirrors the `Lots` input. | `3` |
| `FastLength` | EMA period for the fast line. | `8` |
| `SlowLength` | EMA period for the slow line. | `64` |
| `ShiftDepth` | Number of historical candles used to emulate the MQL moving average shift comparisons. | `3` |
| `UseStopLoss` | Enables fixed stop loss. | `true` |
| `StopLossPips` | Stop loss distance expressed in pips. | `75` |
| `UseTakeProfit` | Enables take profit. | `true` |
| `TakeProfitPips` | Take profit distance expressed in pips. | `150` |
| `UseTrailingStop` | Enables trailing stop management. | `true` |
| `TrailingStopPips` | Trailing distance in pips. | `30` |
| `M1CandleType` | Candle type for the signal timeframe (default 1 minute). | `1m` |
| `M5CandleType` | Candle type for the mid-term filter (default 5 minutes). | `5m` |
| `M30CandleType` | Candle type for the higher timeframe (default 30 minutes). | `30m` |

## Usage Notes
1. Attach the strategy to an instrument and ensure historical data is available for all three timeframes to allow the EMA buffers to populate.
2. The `ShiftDepth` parameter should remain at least `2` so the strategy can validate short-term momentum.
3. When `UseTrailingStop` is active without `UseStopLoss`, the trailing logic still initializes a stop value once the trade moves in favour.
4. Because StockSharp executes on candle close, results can differ slightly from the tick-by-tick execution of the MT4 version, especially on volatile markets. The core trend-alignment behaviour remains intact.

## Conversion Notes
- Indicator calculations rely exclusively on StockSharp's `Bind` mechanism; no manual indicator history collections are used.
- Order management is implemented with high-level helpers (`BuyMarket`, `SellMarket`) and internal price tracking instead of direct `OrderSend` calls.
- Mail notifications and slippage controls from the MQL script are omitted because they are outside StockSharp's scope.

## Files
- `CS/MultiTimeframeEmaAlignmentStrategy.cs` – main C# strategy implementation.
- `README_ru.md` – Russian documentation.
- `README_cn.md` – Chinese documentation.
