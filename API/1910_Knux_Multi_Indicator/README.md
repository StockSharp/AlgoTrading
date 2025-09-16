# Knux Multi-Indicator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy blends trend strength and momentum oscillators to trade breakouts. It waits for a bullish or bearish crossover of two moving averages while the Average Directional Index (ADX) signals a strong trend. The Relative Vigor Index (RVI), Commodity Channel Index (CCI) and Williams %R act as filters to ensure momentum confirms the move and that the market is not overextended.

The system can open both long and short positions. It holds the position until an opposite signal appears and does not use a dedicated stop-loss. All parameters such as indicator periods and thresholds are configurable.

## Details

- **Entry Criteria**:
  - **Long**: Fast SMA crosses above slow SMA, `ADX > AdxLevel`, `RVI` rising, `CCI < -CciLevel`, and `WPR <= -100 + WprBuyRange`.
  - **Short**: Fast SMA crosses below slow SMA, `ADX > AdxLevel`, `RVI` falling, `CCI > CciLevel`, and `WPR >= -WprSellRange`.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal (crossover in the other direction).
- **Stops**: No explicit stop-loss.
- **Default Values**:
  - `FastMaLength` = 5
  - `SlowMaLength` = 20
  - `AdxPeriod` = 14
  - `AdxLevel` = 15
  - `RviPeriod` = 20
  - `CciPeriod` = 40
  - `CciLevel` = 150
  - `WprPeriod` = 60
  - `WprBuyRange` = 15
  - `WprSellRange` = 15
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Multiple
  - Stops: None
  - Complexity: Medium
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
