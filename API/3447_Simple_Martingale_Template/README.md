# Simple Martingale Template Strategy

## Overview
This strategy replicates the original MetaTrader "Simple Martingale Template" idea in StockSharp. It analyzes finished candles of a configurable timeframe using a pair of simple moving averages (SMA). A breakout filter checks whether the previous candle's close breaks the high or low of an even earlier candle to confirm direction. The position size follows a martingale sequence: after each losing cycle the next trade volume is multiplied, while profitable cycles reset the volume to the configured base size.

## Trading Logic
1. Subscribe to candles of the `CandleType` timeframe. Only finished candles participate in signal generation.
2. Calculate a fast SMA and a slow SMA on the candle close.
3. Generate a **buy** signal when:
   - the last finished candle close is above the fast SMA,
   - the fast SMA is above the slow SMA,
   - on the previous candle the fast SMA was below the slow SMA, and
   - the last finished candle close is above the high of the candle two bars ago.
4. Generate a **sell** signal when the symmetric conditions occur to the downside, including the close being below the low of the candle two bars ago.
5. When a signal fires and there are no open positions or active orders, send a market order using the currently calculated martingale volume.
6. Attach synthetic stop-loss and take-profit levels by monitoring future candles. When price touches either level, close the open position.
7. After a position closes and the portfolio balance updates:
   - if the balance increased, reset volume to the `BaseVolume` value;
   - if the balance decreased, multiply the last trade volume by `Multiplier` and align it to the instrument volume step.

## Parameters
| Name | Description |
| --- | --- |
| `StopLossPoints` | Distance from entry to the protective stop in price points. |
| `TakeProfitPoints` | Distance from entry to the profit target in price points. |
| `BaseVolume` | Initial lot size for the martingale cycle. |
| `Multiplier` | Factor applied to the previous lot size after a loss. |
| `FastPeriod` | Length of the fast SMA used for directional bias. |
| `SlowPeriod` | Length of the slow SMA for trend confirmation. |
| `CandleType` | Timeframe of candles processed by the strategy. |

## Money Management
- The martingale ladder reacts strictly to realized balance changes. Small fluctuations (±0.01 monetary units) are ignored to avoid noise.
- Volumes are aligned to the instrument `VolumeStep`, `MinVolume`, and `MaxVolume` to ensure valid order sizes.
- Stop-loss and take-profit levels are monitored on candle extremes (high/low) instead of placing exchange orders, mirroring the original MQL implementation that used market exits.

## Usage Notes
- Choose a timeframe and symbol combination that produces enough historical candles for both SMAs to form before enabling trading.
- Adjust `StopLossPoints` and `TakeProfitPoints` to match the symbol's tick size; they represent point counts, not price units.
- Consider testing different multipliers and base volumes to control capital requirements because martingale sequences grow quickly.
- The strategy calls `StartProtection()` on start to integrate with StockSharp's standard risk management features.
