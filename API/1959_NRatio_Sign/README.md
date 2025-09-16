# NRatio Sign Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy employs the NRatio indicator, an NRTR-based oscillator that measures the normalized distance between price and a dynamic trailing level. Trading signals occur when the NRatio crosses predefined thresholds. Depending on the selected mode, the system either reacts to breakouts beyond the upper and lower bounds or to reversals back inside them.

The approach can operate on both sides of the market and uses percentage-based risk management for exits. Smoothing of the distance metric is performed with an exponential moving average, allowing the strategy to respond quickly while filtering noise.

## Details

- **Entry Criteria**:
  - **Mode In**:
    - **Long**: `NRatio` crosses above `UpLevel`.
    - **Short**: `NRatio` crosses below `DownLevel`.
  - **Mode Out**:
    - **Long**: `NRatio` crosses above `DownLevel`.
    - **Short**: `NRatio` crosses below `UpLevel`.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or protective stop.
- **Stops**: Yes, take-profit and stop-loss in percent.
- **Default Values**:
  - `CandleType` = 4-hour candles
  - `Kf` = 1
  - `Length` = 3
  - `Fast` = 2
  - `Sharp` = 2
  - `UpLevel` = 80
  - `DownLevel` = 20
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 2
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: NRTR, EMA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

