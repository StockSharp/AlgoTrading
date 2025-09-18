# Glam Trader (Multi-timeframe Confirmation)

## Overview
The strategy replicates the original MetaTrader "GLAM Trader" expert advisor by combining information from three timeframes:

- A fast **EMA(3)** on the 15-minute chart captures short-term trend bias.
- A **Laguerre filter** with gamma 0.7 applied to 5-minute candles measures whether price is trading above or below its smoothed path.
- The **Awesome Oscillator** on hourly candles supplies a momentum check aligned with Bill Williams' definition.

Only when all three components agree does the strategy open a trade, aiming to filter out noise that would appear when any single timeframe is evaluated in isolation.

## Trading Logic
1. **Data preparation**
   - 15-minute candles feed an `ExponentialMovingAverage` with length `EmaPeriod` (default 3).
   - 5-minute candles feed a `LaguerreFilter` with smoothing `LaguerreGamma`.
   - 60-minute candles feed an `AwesomeOscillator`.
   - For each timeframe the latest finished candle close is stored to reproduce the original indicator-versus-price comparison.
2. **Entry conditions**
   - **Long**: the EMA is above the current 15-minute close, Laguerre is above the latest 5-minute close, and Awesome Oscillator is above the latest hourly close.
   - **Short**: each of the three indicators must sit below its corresponding close.
3. **Risk management**
   - Separate stop-loss and take-profit distances (expressed in instrument points) for long and short trades.
   - Trailing stops activate once price travels at least the specified trailing distance beyond the entry price. The stop is ratcheted in the trend direction without backing off.
   - All protective actions (take-profit, stop-loss, trailing stop) close the entire position with market orders, mirroring the MQL implementation.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `TradeVolume` | Order size for new positions. | 0.1 |
| `PrimaryCandleType` | Timeframe used for the EMA and main signal. | 15-minute candles |
| `LaguerreCandleType` | Timeframe analysed by the Laguerre filter. | 5-minute candles |
| `AwesomeCandleType` | Timeframe analysed by the Awesome Oscillator. | 60-minute candles |
| `EmaPeriod` | EMA length on the primary timeframe. | 3 |
| `LaguerreGamma` | Gamma parameter for the Laguerre filter. | 0.7 |
| `LongStopLossPoints` | Stop-loss distance for long trades, in points. | 20 |
| `ShortStopLossPoints` | Stop-loss distance for short trades, in points. | 20 |
| `LongTakeProfitPoints` | Take-profit distance for long trades, in points. | 50 |
| `ShortTakeProfitPoints` | Take-profit distance for short trades, in points. | 50 |
| `LongTrailingPoints` | Trailing distance for long trades, in points. | 15 |
| `ShortTrailingPoints` | Trailing distance for short trades, in points. | 15 |

## Notes
- The strategy subscribes to three independent candle streams and keeps only the most recent finished values, avoiding manual history buffers.
- All comments and log messages remain in English for clarity, matching project conventions.
- Adjust the point-based risk parameters according to the instrument's `PriceStep` so that protective levels reflect the broker's tick size.
