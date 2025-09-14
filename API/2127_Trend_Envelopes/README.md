# Trend Envelopes Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trend-following strategy built on the TrendEnvelopes indicator. It combines an EMA with ATR-based bands to detect breakouts.
Long positions are opened when price breaks above the upper band and a buy signal appears. Short positions are opened on breaks below the lower band with a sell signal. Opposite bands trigger position exits.

## Details

- **Entry Criteria**:
  - Long: price closes above upper envelope and generates a buy signal
  - Short: price closes below lower envelope and generates a sell signal
- **Long/Short**: Both
- **Exit Criteria**: Opposite trend signal
- **Stops**: Yes (take profit and stop loss)
- **Default Values**:
  - `MaPeriod` = 14
  - `Deviation` = 0.2m
  - `AtrPeriod` = 15
  - `AtrSensitivity` = 0.5m
  - `TakeProfit` = 2000 points
  - `StopLoss` = 1000 points
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: EMA, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: 4h
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

