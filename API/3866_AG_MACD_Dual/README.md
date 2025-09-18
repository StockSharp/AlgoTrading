# AG Dual MACD Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader 4 expert **AG.mq4**. The robot operates with two Moving Average Convergence Divergence (MACD) calculations that use different parameter sets. The primary MACD produces entry triggers, while the secondary (scaled) MACD acts as a directional filter to avoid counter-trend trades and to control exits. The logic mirrors the original MQL4 expert by evaluating only closed candles and by reusing the signal line sign checks that gated the original orders.

## Trading Logic
- **Indicators**
  - Primary MACD: fast EMA = `FastEmaLength`, slow EMA = `SlowEmaLength`, signal SMA = `SignalSmaLength`.
  - Secondary MACD: fast EMA = `SlowEmaLength * 2`, slow EMA = `FastEmaLength * 2`, signal SMA = `SignalSmaLength * 2`.
- **Long entry**
  - Primary MACD main line is above its signal line.
  - Primary MACD signal line is negative (below the waterline).
  - Secondary MACD main line is above its signal line.
  - Secondary MACD signal line is negative.
- **Short entry**
  - Primary MACD main line is below its signal line.
  - Primary MACD signal line is positive.
  - Secondary MACD main line is below its signal line.
  - Secondary MACD signal line is positive.
- **Exit rules**
  - Close long positions when the secondary MACD turns bearish while the primary signal line stays above zero.
  - Close short positions when the secondary MACD turns bullish while the primary signal line stays below zero.
- The strategy only reacts to finished candles and ignores unfinished bars to avoid repainting.

## Position Management
- All orders are market orders with the fixed volume defined by `OrderVolume`.
- `MaxOpenOrders` mirrors the original `ORDER` input and caps the total number of active orders plus open positions. Set it to `0` to remove the cap.
- `StartProtection()` is enabled once the strategy starts so the StockSharp risk manager can monitor open exposure.

## Parameters
| Name | Description |
| --- | --- |
| `OrderVolume` | Base lot size for new trades. |
| `FastEmaLength` | Fast EMA period of the primary MACD. |
| `SlowEmaLength` | Slow EMA period of the primary MACD. |
| `SignalSmaLength` | Signal smoothing period for both MACDs. |
| `MaxOpenOrders` | Maximum number of combined active orders and open positions. Set `0` for unlimited. |
| `CandleType` | Time frame used to build candles for both indicators. |

## Notes
- The secondary MACD keeps the same fast/slow order as in the original EA, even if the fast period becomes larger than the slow one, to preserve the author's calculations.
- The strategy does not place pending orders; it opens or closes at market as soon as the conditions appear.
- No additional stop-loss or take-profit levels are added because the original expert relied exclusively on signal reversals.
