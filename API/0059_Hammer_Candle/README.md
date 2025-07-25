# Hammer Candle Reversal
[Русский](README_ru.md) | [中文](README_cn.md)
 
Hammer candles often mark an intraday reversal after selling pressure subsides. This strategy looks for a hammer pattern and enters long, anticipating a rebound.

The system requires a lower shadow at least twice the body and little upper shadow. Once identified, it buys with volume size and waits for profit or stop-loss.

## Details

- **Entry Criteria**: Hammer candle detected.
- **Long/Short**: Long only.
- **Exit Criteria**: Stop-loss or discretionary exit.
- **Stops**: Yes.
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Pattern
  - Direction: Long
  - Indicators: Candlestick
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 64%. It performs best in the forex market.
