# E-Skoch-Open Strategy (StockSharp Port)

## Overview
The **E-Skoch-Open** strategy replicates the original MetaTrader 5 expert advisor that trades a simple three-candle pattern. The StockSharp implementation processes completed candles, evaluates momentum reversals in the recent closes and opens a new position when the required configuration appears. Risk is controlled by stop-loss/take-profit offsets measured in adjusted points (pips) and an equity growth target that can flatten every open position. Position sizing follows a martingale scheme: after a losing trade the next order size is multiplied by 1.6 while profitable trades reset the volume to the initial value.

## Trading Logic
1. Work with the timeframe defined by the `CandleType` parameter (default: 1 hour).
2. Wait until at least three completed candles are available.
3. **Buy setup**: if `Close[n-3] > Close[n-2]` and `Close[n-1] < Close[n-2]`, and long trades are enabled.
4. **Sell setup**: if `Close[n-3] > Close[n-2]` and `Close[n-2] < Close[n-1]`, and short trades are enabled.
5. If `CloseOnOppositeSignal` is enabled, receiving an opposite signal closes the existing position immediately and skips new entries for the current bar.
6. For every new position the strategy attaches static stop-loss and take-profit levels calculated from the current close and the configured distance in adjusted points. When the high/low of a completed candle reaches one of these levels the position is closed.
7. The strategy continuously checks the account equity. When the equity growth relative to the last flat moment exceeds `TargetProfitPercent`, all positions are closed.
8. After a trade closes with a loss the next order volume is multiplied by 1.6. After a profitable trade the volume returns to the initial size. Volumes are normalized using the instrument constraints (`VolumeStep`, `VolumeMin`, `VolumeMax`).

## Parameters
| Parameter | Description |
| --- | --- |
| `CandleType` | Timeframe used for pattern detection. Works with any candles supported by StockSharp. |
| `InitialOrderVolume` | Base lot size for the first trade in a sequence (default: 0.01). |
| `StopLossPoints` | Stop-loss distance expressed in adjusted points. For 5-digit or 3-digit instruments the point value is `PriceStep * 10`, otherwise `PriceStep`. |
| `TakeProfitPoints` | Take-profit distance using the same adjusted point convention. |
| `EnableBuySignals` / `EnableSellSignals` | Toggle long or short entries. |
| `MaxBuyTrades` / `MaxSellTrades` | Maximum number of consecutive trades allowed per direction (`-1` removes the limit). The port keeps at most one position per direction by default. |
| `TargetProfitPercent` | Equity percentage gain that triggers closing all positions (default: 1.2%). |
| `CloseOnOppositeSignal` | If enabled, a signal in the opposite direction forces a flat position before new trades are considered. |

## Risk Management Notes
- Stop-loss and take-profit levels are simulated from candle extremes. In live trading intrabar execution can differ from MetaTrader where protective orders are registered on the server.
- The martingale multiplier (1.6) can grow volumes rapidly during drawdowns. Ensure the instrument limits (`VolumeMax`) and portfolio capital can support the largest expected position.
- Equity-based profit locking works only when portfolio information is available via `Portfolio.CurrentValue`.

## Usage Tips
- Adjust `CandleType` to match the timeframe used in the original expert advisor.
- Tune `StopLossPoints` / `TakeProfitPoints` to instrument volatility; they are pip-based thanks to the adjusted point calculation.
- Disable one direction if hedging is not allowed by the broker or risk policy.
- Keep an eye on the equity target and martingale settings when running long tests to avoid unexpected liquidation.
