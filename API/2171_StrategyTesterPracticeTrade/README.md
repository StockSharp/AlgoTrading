# Strategy Tester Practice Trade Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Utility strategy for practicing manual trading in the Strategy Tester. The strategy monitors a directory for command files and executes market orders when they appear. Create an empty `buy.txt`, `sell.txt` or `close.txt` inside the command folder to trigger a buy, sell or position close on the next completed candle.

## Details

- **Entry Criteria**: presence of `buy.txt` or `sell.txt` in the command directory
- **Long/Short**: Both
- **Exit Criteria**: `close.txt` removes any open position
- **Stops**: No
- **Default Values**:
  - `LotSize` = 1
  - `CommandDir` = system temporary directory
  - `CandleType` = 1 minute candles
- **Filters**:
  - Category: Utility
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
