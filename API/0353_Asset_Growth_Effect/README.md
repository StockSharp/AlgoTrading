# Asset Growth Effect Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy goes long on companies with the lowest growth in total assets and shorts those with the highest asset growth. Each July the portfolio is rebalanced using the most recent fundamental data.

Testing indicates an average annual return of about 15%. It performs best in the equities market.

Asset growth is calculated from total assets reported in company filings. Stocks are ranked into quantiles and the lowest bucket is bought while the highest is sold short. Positions are sized to meet a target leverage and are adjusted annually.

## Details

- **Entry Criteria**:
  - Long: Stock in lowest asset growth quantile.
  - Short: Stock in highest asset growth quantile.
- **Long/Short**: Both.
- **Exit Criteria**: Positions adjusted on annual rebalance.
- **Stops**: No.
- **Default Values**:
  - `Quantiles` = 10
  - `Leverage` = 1m
  - `MinTradeUsd` = 50m
  - `CandleType` = TimeSpan.FromDays(1).TimeFrame()
- **Filters**:
  - Category: Fundamental
  - Direction: Both
  - Indicators: Fundamentals
  - Stops: No
  - Complexity: Medium
  - Timeframe: Long-term
  - Seasonality: Yes
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
