# PZ Parabolic SAR EA
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the *PZ Parabolic SAR* expert advisor. It employs two Parabolic SAR indicators with different step and maximum acceleration settings. The "trade" SAR detects trend direction for entries, while the "stop" SAR follows price more closely and triggers exits when the trend reverses.

Risk control is handled through the Average True Range (ATR). An initial ATR-based stop is set when a position opens. Optionally, a trailing stop based on ATR can tighten the stop as price moves in the trade's favor. The strategy also supports partial closing: once the profit exceeds the initial stop distance, half of the position is closed and the stop is moved to break-even.

The strategy works in both long and short directions and operates on finished candles only. It uses market orders without placing actual stop orders.

## Details

- **Entry Criteria**: Price above/below trade SAR and stop SAR in the same direction.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop SAR crossing price or ATR trailing stop hit.
- **Stops**: ATR-based stop with optional trailing and break-even.
- **Default Values**:
  - `TradeStep` = 0.002
  - `TradeMax` = 0.2
  - `StopStep` = 0.004
  - `StopMax` = 0.4
  - `AtrPeriod` = 30
  - `AtrMultiplier` = 2.5
  - `UseTrailing` = false
  - `TrailingAtrPeriod` = 30
  - `TrailingAtrMultiplier` = 1.75
  - `PartialClosing` = true
  - `PercentageToClose` = 0.5
  - `BreakEven` = true
  - `LotSize` = 0.1
  - `CandleType` = TimeFrame(5m)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Parabolic SAR, ATR
  - Stops: ATR, Trailing
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

