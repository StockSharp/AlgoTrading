# Shooting Star Pattern

The shooting star candlestick often appears after an advance and warns of a reversal. This strategy looks for a long upper shadow relative to the body with little lower shadow.

If confirmation is required, the next candle must close lower before entering short. Otherwise the trade can be taken immediately. Stops are placed above the pattern high.

## Details

- **Entry Criteria**: Shooting star detected and confirmation if enabled.
- **Long/Short**: Short only.
- **Exit Criteria**: Stop-loss or discretionary exit.
- **Stops**: Yes.
- **Default Values**:
  - `ShadowToBodyRatio` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `StopLossPercent` = 1.0m
  - `ConfirmationRequired` = true
- **Filters**:
  - Category: Pattern
  - Direction: Short
  - Indicators: Candlestick
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
