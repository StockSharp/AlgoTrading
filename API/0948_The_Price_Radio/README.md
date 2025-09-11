# The Price Radio Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy implements John Ehlers' Price Radio indicator. It enters long when the price derivative exceeds both amplitude and frequency thresholds, and enters short when it falls below their negative values.

## Details

- **Entry Criteria**:
  - **Long**: derivative greater than amplitude and frequency.
  - **Short**: derivative less than negative amplitude and negative frequency.
- **Long/Short**: Both sides.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `Length` = 14.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: Custom
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
