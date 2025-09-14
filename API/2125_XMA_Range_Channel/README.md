# XMA Range Channel Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that builds an upper and lower channel from moving averages of the high and low prices. A breakout above the upper band triggers a long entry, while a breakout below the lower band triggers a short entry. The model mirrors the behaviour of the original MQL "XMA Range Channel" expert.

## Details

- **Entry Criteria**:
  - Long: `Close > UpperChannel`
  - Short: `Close < LowerChannel`
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: No
- **Default Values**:
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `Length` = 7
- **Filters**:
  - Category: Channel breakout
  - Direction: Both
  - Indicators: SMA on High/Low
  - Stops: No
  - Complexity: Basic
  - Timeframe: Swing
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
