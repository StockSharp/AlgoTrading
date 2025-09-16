# XMACD Modes Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on the MACD indicator that supports four different entry modes:

- **Breakdown**: open trades when MACD crosses the zero line.
- **MacdTwist**: react to a change in MACD direction from falling to rising or vice versa.
- **SignalTwist**: use turning points of the signal line as triggers.
- **MacdDisposition**: trade on crossovers between MACD and its signal line.

The strategy subscribes to 4-hour candles and calculates a classic MACD (EMA 12/26 with a 9-period signal). It can both open and close positions on opposite signals. Risk is managed through optional stop loss and take profit expressed as percentages of the entry price.

## Details

- **Entry Criteria**: MACD-based signals depending on selected mode.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `FastEmaPeriod` = 12
  - `SlowEmaPeriod` = 26
  - `SignalPeriod` = 9
  - `CandleType` = TimeSpan.FromHours(4)
  - `Mode` = MacdDisposition
  - `StopLossPercent` = 2m
  - `TakeProfitPercent` = 4m
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MACD
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Swing (4h)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
