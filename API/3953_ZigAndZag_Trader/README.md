# ZigAndZag Trader Strategy

## Overview
The **ZigAndZag Trader Strategy** is the StockSharp port of the MetaTrader expert *ZigAndZag_trader.mq4*. The system layers two ZigZag-inspired swing detectors:

1. A **long-term ZigZag** (configured by `TrendDepth`) tracks the primary trend by marking major swing highs and lows.
2. A **short-term ZigZag** (configured by `ExitDepth`) identifies the latest swing pivot inside that trend and monitors the weighted price (`(5×Close + 2×Open + High + Low) / 9`).

The robot opens trades only when price moves away from the latest swing pivot in the direction of the dominant trend and closes positions when the weighted price breaks back through that pivot against the trend. This reproduces the behaviour of the original MetaTrader expert that read buffers 4–6 of the custom `ZigAndZag` indicator.

## Trading Logic
- **Trend detection** – when the long-term ZigZag confirms a new low the trend is considered *up*; a new high flips it to *down*.
- **Swing tracking** – each short-term pivot resets the internal state and stores the weighted price of that swing.
- **Entry conditions**
  - Uptrend + last pivot is a low: buy when the weighted price rises above the stored pivot by at least one pip.
  - Downtrend + last pivot is a high: sell when the weighted price falls below the stored pivot by at least one pip.
- **Exit condition** – if price moves back through the stored pivot while the trend disagrees with the active swing, all open positions are closed.
- **Order throttling** – the total absolute position size is capped by `MaxOrders × Volume`. Additional signals are ignored once that cap is reached.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | `1 Minute` | Candle type used for both ZigZag evaluations. |
| `Lots` | `0.1` | Requested trade size in lots. The final volume is aligned to the instrument volume step. |
| `TrendDepth` | `3` | Lookback (in candles) of the long-term ZigZag that defines the trend. |
| `ExitDepth` | `3` | Lookback (in candles) of the short-term ZigZag that produces swing entries and exits. |
| `MaxOrders` | `1` | Maximum number of simultaneous orders/position units. |
| `StopLossPips` | `0` | Protective stop-loss distance in pips (`0` disables the stop). |
| `TakeProfitPips` | `0` | Take-profit distance in pips (`0` disables the target). |

## Risk Management
`StartProtection` is enabled automatically. When the stop-loss or take-profit distance is set to a value greater than zero, fixed protective orders are attached to every market order using the provided pip distance and the instrument tick size.

## Visualisation
The strategy draws candlesticks and executed trades on the default chart area. No custom indicator is plotted because the entry and exit logic uses internal ZigZag trackers.

## Notes
- The weighted price formula is identical to the MetaTrader indicator and avoids direct indicator buffer access.
- The breakout threshold is equal to one instrument pip, mirroring the original code that required the move to exceed the current spread.
- The port keeps all comments and logging in English as required by the project guidelines.
