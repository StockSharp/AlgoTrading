# Color XTRIX Histogram Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades based on the direction changes of a smoothed TRIX (triple exponential moving average momentum) calculated from logarithmic closing prices. A long position is opened when the TRIX histogram turns upward after declining, while a short position is opened when it turns downward after rising. Positions are reversed on opposite turns. No stop-loss or take-profit is used.

## Details

- **Entry Criteria**:
  - **Long**: `TRIX rising` && `previous TRIX falling`
  - **Short**: `TRIX falling` && `previous TRIX rising`
- **Long/Short**: Long and Short
- **Exit Criteria**:
  - Long: `TRIX turns downward`
  - Short: `TRIX turns upward`
- **Stops**: No
- **Default Values**:
  - `TRIX Length` = 5
  - `Smooth Length` = 5
  - `Momentum Period` = 1
  - `Candle Type` = 4h time frame
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: TRIX
  - Stops: No
  - Complexity: Low
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
