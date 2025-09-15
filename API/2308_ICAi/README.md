# ICAi Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on the ICAi adaptive moving average indicator. The indicator smooths price and adapts its slope using standard deviation. Long positions are opened when the indicator turns upward, short positions when it turns downward.

The algorithm works on any market where candle data is available. Default settings use a 4-hour timeframe and a smoothing length of 12.

## Details

- **Entry Criteria**:
  - Long: `Prev < PrevPrev && Current >= Prev`
  - Short: `Prev > PrevPrev && Current <= Prev`
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: Optional fixed stop loss and take profit
- **Default Values**:
  - `Length` = 12
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: ICAi
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

