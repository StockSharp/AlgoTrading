# Martingale Breakout Strategy

## Overview

The **Martingale Breakout Strategy** is a StockSharp port of the MetaTrader expert advisor `MartinGaleBreakout.mq5`. The system
waits for abnormally large breakout candles and places a single market order in the breakout direction. While the original EA
tracks a "magic number" to manage its positions, the StockSharp implementation relies on the strategy context, so the behaviour
is effectively the same when the strategy is executed in isolation.

The algorithm focuses on two core ideas:

1. **Breakout detection** – the strategy examines the size of each finished candle and compares it to the average range of the
   previous ten candles. When the current range is three times larger than the average and the candle closes strongly in the
   direction of the breakout, a trading signal is produced.
2. **Martingale-style recovery** – the strategy keeps track of floating profit and loss. Whenever the unrealized PnL reaches the
   configured loss threshold it immediately closes all open positions and increases the next profit target so the following trade
   attempts to recover the loss. Once the increased target is met, the thresholds are reset to the original values.

The port keeps all money-management parameters from the MQL5 code, including the balance percentage reserved for margin, the
percentage-based profit and loss goals, and the multiplier that expands the take-profit distance during the recovery phase.

## Trading logic

1. Subscribe to the configured candle series and wait for finished candles.
2. Compute the candle range (`High - Low`) and maintain a fixed-size buffer with the previous ten ranges to determine the
   reference average used for breakout detection.
3. Calculate the floating PnL by tracking average entry prices for the long and short sides. If the unrealized PnL exceeds the
   profit target or breaches the stop-loss threshold, immediately close all positions and reset the recovery state as in the
   original expert advisor.
4. Skip order placement while the strategy already holds a position or when trading is not allowed by the connection state.
5. When a bullish breakout candle appears, size the order so that the expected profit matches the current target. The take-profit
   distance in price steps is multiplied during recovery, exactly like the `TP_Points_Multiplier` parameter from the EA.
6. Validate the calculated volume against the instrument limits (minimum, maximum and step) and make sure the required margin
   does not exceed the configured balance allocation or the available free funds. If the constraints are respected, submit a
   market buy order.
7. Repeat the same process for bearish breakouts, submitting a market sell order instead.

The combination of these rules recreates the behaviour of the original MetaTrader system, including the transition into and out
of the recovery mode after a stop-loss event.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `TakeProfitPoints` | Distance between the entry price and the take-profit price expressed in price steps. | `50` |
| `BalancePercentAvailable` | Maximum percentage of the account balance that can be reserved for margin on a single trade. | `50` |
| `TakeProfitPercentOfBalance` | Target profit expressed as a percentage of the current balance. | `0.1` |
| `StopLossPercentOfBalance` | Stop-loss size expressed as a percentage of the current balance. | `10` |
| `RecoveryStartFraction` | Fraction of the stop-loss used before switching into the recovery mode. | `0.1` |
| `RecoveryPointsMultiplier` | Multiplier applied to the take-profit distance while recovering. | `1` |
| `CandleType` | Candle data source used by the strategy (time frame, tick candles, etc.). | `15-minute time frame` |

## Additional notes

- The volume calculation replicates the MetaTrader helper `CalcLotWithTP`. It derives the lot size required to reach the current
  profit target for a given price move and then normalizes the result to the instrument's volume step.
- Margin checks are performed with the same spirit as `CheckVolumeValue` and the balance-percentage filter used in the MQL
  version. Orders are rejected when the required margin exceeds the allowed share of the balance or the free funds reported by
  the portfolio.
- The strategy cancels all active orders before flattening positions so the behaviour matches the `CloseAllOrders` helper from
  the original expert advisor.
- The internal range buffer stores only ten values and is equivalent to iterating over `iHigh`/`iLow` in the source EA. No
  historical data beyond the last ten candles is required.
