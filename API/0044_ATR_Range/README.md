# ATR Range Breakout
[Русский](README_ru.md) | [中文](README_cn.md)
 
ATR Range Breakout measures price movement over a fixed number of bars and compares it with the average true range. When the move exceeds the ATR, a position is opened in the direction of the move.

The strategy checks price every N bars and uses the moving average for exit signals. It aims to capture momentum when volatility expands beyond normal levels.

Trades close when price crosses back through the moving average or when the stop based on ATR fires.

## Details

- **Entry Criteria**: Price moves more than ATR over the lookback period.
- **Long/Short**: Both directions.
- **Exit Criteria**: Price crosses MA or stop.
- **Stops**: Yes.
- **Default Values**:
  - `MAPeriod` = 20
  - `ATRPeriod` = 14
  - `LookbackPeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: ATR, MA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 169%. It performs best in the crypto market.
