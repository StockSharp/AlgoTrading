# Hawaiian Tsunami Surfer
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy looks for sudden momentum spikes and trades against them. It calculates the percentage change of the closing price over one bar using a Momentum indicator. When the percentage change exceeds a tiny threshold, the move is considered a "tsunami". The strategy sells after a strong upward spike and buys after a strong downward spike. Protective stop-loss and take-profit are applied in price steps through StartProtection.

## Details

- **Entry Criteria**:
  - Sell when momentum percentage > `TsunamiStrength`.
  - Buy when momentum percentage < `-TsunamiStrength`.
- **Long/Short**: Both directions.
- **Exit Criteria**: Protective stop-loss or take-profit.
- **Stops**: Yes, via StartProtection.
- **Default Values**:
  - `MomentumPeriod` = 1
  - `TsunamiStrength` = 0.24
  - `TakeProfitPoints` = 500
  - `StopLossPoints` = 700
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Momentum
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: High
