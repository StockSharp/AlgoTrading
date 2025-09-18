# SUPERMACBOT by The Guardian Forex TV Strategy

## Overview
The **SUPERMACBOT by The Guardian Forex TV Strategy** replicates the concept of the original MetaTrader expert advisor by combining the MACD oscillator with a dual simple moving average trend filter and a trailing average exit filter. The converted StockSharp implementation works on completed candles and sends market orders whenever a bullish or bearish confluence forms. The strategy avoids tick-by-tick trading and follows the high-level API guidelines by relying on candle subscriptions and indicator bindings.

The trading engine evaluates momentum through the MACD histogram and trend alignment between two simple moving averages. A trailing moving average acts both as a trade management reference and a delayed confirmation filter, mirroring the trailing module configured in the MQL expert. The StockSharp version focuses on clarity and portability across instruments and timeframes by exposing every key value as a configurable parameter.

## Trading Logic
1. **Data source** – the strategy subscribes to a configurable candle type (timeframe). Each completed candle triggers the decision flow.
2. **Indicator preparation** – MACD (with adjustable fast, slow, and signal periods) and two SMAs are recalculated on every candle. An additional SMA replicates the trailing filter from the MQL expert.
3. **Entry rules**
   - **Long entry**
     - MACD histogram crosses above the configurable threshold.
     - The fast SMA is above the slow SMA, showing an established bullish trend.
     - Close price remains above the trailing SMA to ensure price strength.
     - The strategy has no existing long position (only one net position is maintained).
   - **Short entry**
     - MACD histogram crosses below the negative threshold.
     - The fast SMA is below the slow SMA, signalling a bearish environment.
     - Close price stays below the trailing SMA.
     - The strategy holds no short exposure.
4. **Exit rules**
   - Long positions are closed when any of the following happens: histogram turns negative, the fast SMA dips below the slow SMA, or price closes beneath the trailing SMA.
   - Short positions are closed when the histogram turns positive, the fast SMA rises above the slow SMA, or price closes above the trailing SMA.
5. **Risk handling** – the algorithm trades a single net position and never pyramids. Protective stops can be added externally by using StockSharp risk rules if desired.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Candle series processed by the strategy. | 1-minute timeframe |
| `FastMaPeriod` | Period of the fast simple moving average filter. | 12 |
| `SlowMaPeriod` | Period of the slow simple moving average filter. | 26 |
| `MacdFastPeriod` | Fast EMA period for the MACD indicator. | 12 |
| `MacdSlowPeriod` | Slow EMA period for the MACD indicator. | 24 |
| `MacdSignalPeriod` | Signal EMA period for the MACD indicator. | 9 |
| `HistogramThreshold` | Minimum absolute value required from the MACD histogram before opening a position. | 0.0 |
| `TrailingPeriod` | Period of the trailing simple moving average used for confirmations and exits. | 12 |

All parameters are exposed through `StrategyParam<T>` and can be optimized inside StockSharp Designer.

## Usage Notes
- Attach the strategy to any security and timeframe that suits your testing environment.
- Ensure a sufficient history buffer is available so that all indicators become fully formed before trading begins.
- Because the strategy works with finished candles and net positions, it is safe to run in multi-instrument portfolios without conflicting orders.
- Additional money management (lot sizing, stop losses, partial exits) can be added by composing the strategy with other StockSharp modules.

## Differences from the Original Expert
- The StockSharp conversion focuses on candle-close logic rather than the event-driven engine of the MetaTrader Expert Advisor. This keeps the behaviour deterministic across backtests and live trading.
- Lot sizing and trailing stop orders from the original Expert Advisor are replaced with a simplified, position-based exit conditioned by the trailing average.
- Signal thresholds are handled via the MACD histogram threshold parameter, allowing users to mimic the scoring system of the MQL Expert by adjusting the value.

## Disclaimer
Trading algorithms involve financial risk. Thoroughly backtest and forward-test the strategy before deploying it with real capital.
