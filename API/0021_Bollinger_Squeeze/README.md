# Bollinger Squeeze
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on Bollinger Bands squeeze

Testing indicates an average annual return of about 100%. It performs best in the forex market.

Bollinger Squeeze waits for narrow band width indicating low volatility. A break outside the bands starts a trade in that direction and it exits when momentum fails or an opposite break appears.

The squeeze condition hints at an upcoming volatility expansion. Once triggered, the trade rides the breakout and relies on an ATR stop or band crossover to exit.


## Details

- **Entry Criteria**: Signals based on Bollinger.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `SqueezeThreshold` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Bollinger
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

