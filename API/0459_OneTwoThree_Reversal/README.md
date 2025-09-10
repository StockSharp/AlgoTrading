# One-Two-Three Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The One-Two-Three Reversal strategy looks for a bullish 1-2-3 pattern in price action. A long position is opened when today's low is below yesterday's, yesterday's low is below the low three bars ago, the low two bars ago is below the low four bars ago, and the high two bars ago is below the high three bars ago. The trade is closed after a defined number of bars or when price closes above a moving average.

## Details

- **Entry Criteria:**
  - Current low < previous low.
  - Previous low < low three bars ago.
  - Low two bars ago < low four bars ago.
  - High two bars ago < high three bars ago.
- **Long/Short:** Long only.
- **Exit Criteria:**
  - Hold for `DaysToHold` bars or close crosses above moving average.
- **Stops:** None.
- **Default Values:**
  - `DaysToHold` = 7
  - `MaLength` = 200
- **Filters:**
  - Category: Reversal
  - Direction: Long
  - Indicators: Price action, SMA
  - Stops: No
  - Complexity: Low
  - Timeframe: Daily
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
