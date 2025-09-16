# Three Line Break Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that trades reversals detected by the Three Line Break indicator.
The indicator compares the current high and low against the highest high and lowest low of the previous N completed candles.
A breakout above the recent high during a downtrend signals a new uptrend and triggers a long entry; a breakdown below the recent low during an uptrend triggers a short entry.
Positions are reversed on each signal.

## Details

- **Entry Criteria**:
  - Long: `Downtrend` switches to `Uptrend`
  - Short: `Uptrend` switches to `Downtrend`
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal (position reversal)
- **Stops**: No
- **Default Values**:
  - `LinesBreak` = 3
  - `CandleType` = TimeSpan.FromHours(12).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Highest, Lowest (Three Line Break logic)
  - Stops: No
  - Complexity: Basic
  - Timeframe: Swing
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
