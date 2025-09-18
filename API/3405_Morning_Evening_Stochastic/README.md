# Morning Evening Stochastic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the MetaTrader 5 expert advisor **Expert_AMS_ES_Stoch** (Morning/Evening Star with Stochastic confirmation) into StockSharp. The implementation keeps the original candlestick pattern recognition and stochastic confirmation rules while using the high-level candle subscription API so every decision is made on finished bars.

## Strategy Logic
- **Indicators**
  - Standard Stochastic oscillator with configurable `%K`, `%D` and slowing periods.
  - Simple moving average of the candle body size (absolute `open-close`) to classify candles as "long" or "small" just like the MQL version.
- **Long Entry**
  - Morning Star pattern across the last three completed candles:
    1. Two bars ago: long bearish body whose size exceeds the body average.
    2. Previous bar: small-bodied candle that closes and opens below the prior candle.
    3. Current bar: bullish close above the midpoint of the first candle.
  - Stochastic signal line (`%D`) is below the oversold threshold (default `30`).
  - Existing short exposure is flattened before opening the long position.
- **Short Entry**
  - Evening Star pattern mirroring the rules above.
  - Stochastic `%D` is above the overbought threshold (default `70`).
  - Existing long exposure is closed before opening the short trade.
- **Position Exit**
  - Shorts are closed when `%D` crosses above either the fast recovery level (`20`) or the extreme level (`80`).
  - Longs are closed when `%D` crosses below either `80` or `20`.
  - These crossings reproduce the "close conditions" from the MQL signal module.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Timeframe (or other `DataType`) used for pattern detection and all indicators. |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | `%K`, `%D` and slowing periods of the stochastic oscillator. |
| `StochasticOverbought`, `StochasticOversold` | Signal-line thresholds used to confirm Evening/Morning Star entries. |
| `PatternAveragePeriod` | Number of finished candles used to average the body size (`|open-close|`). |
| `ShortExitLevel`, `LongExitLevel` | `%D` levels that force short/long exits when crossed in the opposite direction. |

## Implementation Notes
- Candles are processed through `SubscribeCandles().BindEx(...)`; the code only works with finished candles and never calls `GetValue()` on indicators.
- Body-size averaging relies on `SimpleMovingAverage` fed with absolute candle bodies to reproduce the `AvgBody()` helper from the MQL library.
- Pattern checks are implemented with dedicated helper methods to keep the decision logic readable and to mirror the original `CCandlePattern` rules.
- Before entering in the opposite direction the strategy closes any existing exposure to match the Expert Advisor's behaviour of operating one net position at a time.

## Differences from the MQL5 Expert
- Money management, trailing stop and fixed lot settings from the MetaTrader framework are not reproduced; StockSharp order volume is controlled by the strategy `Volume` property.
- The Stochastic oscillator uses StockSharp's indicator implementation; thresholds remain configurable so you can fine-tune the behaviour if the original broker feed produced slightly different values.
- Logging provides detailed explanations (in English) for every entry and exit to aid debugging and backtesting.
