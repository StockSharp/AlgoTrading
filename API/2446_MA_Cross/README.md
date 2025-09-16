# Ma Cross Strategy

## Overview
This strategy replicates the "MA Cross" MetaTrader 5 expert advisor (file `MA Cross.mq5`) inside the StockSharp framework. The system observes two configurable moving averages and issues market orders whenever the fast average crosses the slow average. The implementation keeps the original level of flexibility by exposing the moving-average method, applied price, and indicator shift for both curves.

## Strategy Logic
1. Subscribe to a single candle stream defined by the `CandleType` parameter.
2. Compute the fast and slow moving averages on every finished candle. Each moving average can use one of four methods (simple, exponential, smoothed, or linear weighted) and reads one of the MetaTrader-style applied prices (close, open, high, low, median, typical, or weighted).
3. Store the most recent indicator values while taking the configured shift into account, so that crossover tests are performed on values from prior bars when required.
4. Detect a bullish crossover when the fast average moves from below the shifted slow average to above it. Detect a bearish crossover when the opposite movement happens.
5. Issue market orders only after both indicators are fully formed and the strategy is online. Long signals close any existing short position and open a long position of `OrderVolume`. Short signals close any existing long position and open a short position of the same size.

The strategy operates strictly on completed candles and never inspects unfinished data. Protective logic is activated through `StartProtection()` to ensure that StockSharp monitors the open position for abnormal conditions.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `FastPeriod` | 3 | Period of the fast moving average. |
| `SlowPeriod` | 13 | Period of the slow moving average. |
| `FastMethod` | Simple | Moving-average method for the fast line (simple, exponential, smoothed, or linear weighted). |
| `SlowMethod` | LinearWeighted | Moving-average method for the slow line. |
| `FastPriceType` | Close | Applied price used by the fast line (close, open, high, low, median, typical, weighted). |
| `SlowPriceType` | Median | Applied price used by the slow line. |
| `FastShift` | 0 | Number of completed bars used to shift the fast average to the left. |
| `SlowShift` | 0 | Number of completed bars used to shift the slow average to the left. |
| `OrderVolume` | 1 | Volume for each market order. |
| `CandleType` | 1-minute time frame | Candle data series processed by the strategy. |

All parameters can be optimized inside StockSharp because the constructor registers them using `StrategyParam` helpers.

## Trading Rules
- **Long entry:** Triggered when the fast average crosses above the slow average according to the shift-adjusted values. If the strategy is already short, it submits a single buy market order sized to close the short exposure and open a new long position. If no position exists, it buys exactly `OrderVolume`.
- **Short entry:** Triggered when the fast average crosses below the slow average. Existing long exposure is reversed via a single sell market order; otherwise the strategy opens a fresh short trade of `OrderVolume`.
- **No additional scaling:** Once positioned, identical-direction signals are ignored until the opposite crossover happens.
- **Execution style:** Orders are sent with `BuyMarket` or `SellMarket`. The strategy does not configure stop-loss or take-profit levels; risk management can be layered through other StockSharp modules if required.

## Conversion Notes
- Indicator creation mirrors the MetaTrader `iMA` calls. The custom `MovingAverageMethod` enumeration maps `MODE_SMA`, `MODE_EMA`, `MODE_SMMA`, and `MODE_LWMA` to StockSharp's `SimpleMovingAverage`, `ExponentialMovingAverage`, `SmoothedMovingAverage`, and `WeightedMovingAverage` respectively.
- Applied-price handling reproduces the MetaTrader `ENUM_APPLIED_PRICE` options by calculating median, typical, and weighted prices directly from the candle data.
- The shift parameters reuse the original logic: the strategy buffers indicator values and retrieves the entry and exit comparisons from earlier bars when `FastShift` or `SlowShift` are positive.
- Position-management logic matches the original approach where opposite signals first close the existing position and then establish a position in the new direction on the same bar.
