# Ichimoku Adx Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on Ichimoku Cloud and ADX indicators. Entry criteria:
Long: Price > Kumo (cloud) && Tenkan > Kijun && ADX > 25 (uptrend with strong movement) Short: Price < Kumo (cloud) && Tenkan < Kijun && ADX > 25 (downtrend with strong movement) Exit criteria: Long: Price < Kumo (price falls below cloud) Short: Price > Kumo (price rises above cloud)

This strategy blends Ichimoku Cloud signals with ADX to filter for powerful trends. Trades occur when price breaks above or below the cloud with ADX confirming.

It favors traders who prefer structured trend setups. ATR-defined stops defend against adverse swings.

## Details

- **Entry Criteria**:
  - Long: `Price > Cloud && Tenkan > Kijun && ADX > AdxThreshold`
  - Short: `Price < Cloud && Tenkan < Kijun && ADX > AdxThreshold`
- **Long/Short**: Both
- **Exit Criteria**:
  - Price crosses the cloud opposite
- **Stops**: Uses Ichimoku cloud for trailing stop
- **Default Values**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Ichimoku Cloud, ADX
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
