# Stop Loss Mover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This utility strategy monitors an open position and moves its stop-loss to the entry price when the market reaches a predefined level. It subscribes to candle data and checks each completed candle. For long positions, once the candle's high exceeds the configured `MoveSlPrice`, a stop order at the entry price is placed. For short positions, the stop is moved when the candle's low falls below the level.

The strategy does not generate new trading signals. It opens a single long position at start for demonstration purposes and then protects it by moving the stop to break-even once conditions are met. This allows traders to secure profits while letting the trade run.

## Details

- **Entry Criteria**: A long position is opened at the beginning. No additional signals are used.
- **Long/Short**: Supports both, but the sample opens a long position.
- **Exit Criteria**: Position exits when the stop order at the entry price is triggered.
- **Stops**: Stop-loss moves to the entry price when `MoveSlPrice` is reached.
- **Default Values**:
  - `MoveSlPrice` = 0 (should be adjusted before run).
  - `CandleType` = 1-minute time frame.
- **Filters**:
  - Category: Risk management
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Simple
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
