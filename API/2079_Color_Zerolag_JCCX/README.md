# Color Zerolag JCCX Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy inspired by the ColorZerolagJCCX indicator from MetaTrader. It approximates the original oscillator using two simple moving averages.
The strategy goes long when the fast average crosses below the slow average and goes short when the fast average crosses above the slow average.

## Details

- **Entry Criteria**:
  - Long: `Fast MA crosses below Slow MA`
  - Short: `Fast MA crosses above Slow MA`
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: `StartProtection()`
- **Default Values**:
  - `FastPeriod` = 8
  - `SlowPeriod` = 21
  - `CandleType` = 4-hour candles
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Moving Average
  - Stops: Optional
  - Complexity: Basic
  - Timeframe: Swing
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
