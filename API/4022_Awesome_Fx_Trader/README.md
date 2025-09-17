# Awesome Fx Trader

This strategy reproduces the MetaTrader setup from `MQL/8539`, which consists of the custom indicators **AwesomeFxTradera.mq4** and **t_ma.mq4**. The original code paints the Bill Williams Awesome Oscillator histogram in green or red depending on whether the value is rising or falling, and overlays a 34-period linear weighted moving average (LWMA) alongside a smoothed clone of the same curve. The StockSharp port keeps the same calculations and converts the indicator colours into trading signals.

## Original MQL logic

1. **AwesomeFxTradera.mq4** computes two exponential moving averages applied to the **open price** with periods 8 and 13. Their difference is stored in `ExtBuffer0`. The buffer is painted green when the current value is higher than the previous bar and red when it is lower. This effectively encodes the direction of momentum, not only its sign.
2. **t_ma.mq4** plots a 34-period LWMA of the open price (`ExtMapBuffer1`) and a 6-period simple moving average of that LWMA (`ExtMapBuffer2`). The smoother tracks whether the trend average accelerates or decelerates.

The MetaTrader chart therefore highlights bullish momentum when the oscillator is above zero and keeps increasing while price trades above the smoothed LWMA. Bearish momentum is the opposite configuration.

## StockSharp implementation

The `AwesomeFxTraderStrategy` subscribes to a configurable candle type (default **M15**) and feeds the indicators with the candle open price to match the MetaTrader buffers.

1. The fast and slow EMAs are recalculated on every finished candle; their difference reproduces the oscillating histogram.
2. The LWMA tracks the 34-bar trend and a 6-bar SMA smooths it. Comparing both series reveals whether the trend curve is rising or falling.
3. The oscillator colour is rebuilt by comparing the current histogram value with the previous bar, following the `bool up` logic from the MQL implementation.
4. **Entry rules**:
   - Enter long when the oscillator is positive, rising (green buffer) and the LWMA is above its smoother.
   - Enter short when the oscillator is negative, falling (red buffer) and the LWMA is below its smoother.
5. **Exit/reversal rules**: an opposite signal reverses the position. The order size is automatically increased by the absolute current position so that shorts are closed before a long is established and vice versa.

No extra stop-loss or take-profit levels are defined in the source code, so the port relies solely on momentum flips for exits. Logging statements document each trade trigger together with the indicator readings.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `FastEmaPeriod` | 8 | Length of the fast EMA used in the oscillator replica. |
| `SlowEmaPeriod` | 13 | Length of the slow EMA. |
| `TrendLwmaPeriod` | 34 | Period of the LWMA trend filter taken from `t_ma.mq4`. |
| `TrendSmoothingPeriod` | 6 | Window of the SMA applied to the LWMA values. |
| `CandleType` | 15-minute time-frame | Candle data type used for both momentum and trend calculations. |

All parameters can be optimised through the StockSharp UI thanks to the `StrategyParam` metadata.

## File mapping

| MetaTrader file | StockSharp counterpart | Notes |
| --- | --- | --- |
| `MQL/8539/AwesomeFxTradera.mq4` | `CS/AwesomeFxTraderStrategy.cs` | Recreates the EMA-on-open oscillator and its rising/falling colour logic. |
| `MQL/8539/t_ma.mq4` | `CS/AwesomeFxTraderStrategy.cs` | Implements the 34-period LWMA with a 6-period SMA smoother for trend detection. |

The Python version is intentionally omitted as requested.
