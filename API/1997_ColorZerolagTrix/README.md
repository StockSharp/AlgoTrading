# Color Zerolag TRIX Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy aggregates five TRIX indicators with different periods and weights to produce a fast and a smoothed slow line. Trades are triggered when the fast line crosses the slow line.

- **Long entry:** previous fast > previous slow and current fast < current slow.
- **Short entry:** previous fast < previous slow and current fast > current slow.
- **Position management:** optional flags allow enabling or disabling long/short entries and exits separately.
- **Parameters:** smoothing factor and five pairs of TRIX periods with corresponding weights.
- **Indicators:** TRIX (five instances) with weighted sum and smoothing.
- **Default timeframe:** 4-hour candles.

## Filters
- Category: Trend following
- Direction: Both
- Indicators: Multiple
- Stops: No
- Complexity: Medium
- Timeframe: Long term
- Seasonality: No
- Neural networks: No
- Divergence: No
- Risk level: Medium
