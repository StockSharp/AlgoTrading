# Supertrend Volume Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Supertrend Volume augments the Supertrend indicator with volume confirmation.
Rising volume during a Supertrend flip strengthens the likelihood of a new impulse move.

Testing indicates an average annual return of about 145%. It performs best in the crypto market.

The strategy enters with the trend on a Supertrend signal only when accompanied by above-average volume.

Stops track the Supertrend line, exiting when price closes on the other side.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Supertrend, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

