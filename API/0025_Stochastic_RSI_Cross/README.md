# Stochastic RSI Cross

Strategy based on Stochastic RSI crossover

Stochastic RSI Cross watches the %K and %D lines of StochRSI. Bullish crosses near oversold levels trigger buys, bearish crosses near overbought trigger sells, and opposite crosses exit.

Because StochRSI oscillates quickly, signals can be frequent. Many traders require the cross to happen near an extreme to filter out noise.


## Details

- **Entry Criteria**: Signals based on RSI, Stochastic.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `RsiPeriod` = 14
  - `StochPeriod` = 14
  - `KPeriod` = 3
  - `DPeriod` = 3
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: RSI, Stochastic
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
