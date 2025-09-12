# Revolution Volatility Bands With Range Contraction Signal VII Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy builds an envelope around price using exponential moving averages and detects when the distance between the bands contracts. When contraction is observed and price breaks above or below the smoothed bands, the strategy opens a position in the direction of the breakout.

## Details

- **Entry Criteria**:
  - **Long**: Range is contracting and close price crosses above the upper smoothed band.
  - **Short**: Range is contracting and close price crosses below the lower smoothed band.
- **Exit Criteria**: opposite breakout.
- **Indicators**: EMA-based envelope.
- **Timeframe**: any.
