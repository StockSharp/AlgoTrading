# TTM Squeeze Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The TTM Squeeze strategy looks for periods of price compression when Bollinger Bands contract inside Keltner Channels. This "squeeze" signals a potential volatility expansion. During the squeeze the strategy monitors a linear regression momentum oscillator and RSI to gauge direction. When the squeeze releases and momentum turns, positions are taken in the direction of the move.

The method seeks explosive breakouts from quiet ranges. Trades are filtered so that long setups require momentum rising from below zero with RSI above 30, while short setups need momentum falling from positive territory with RSI below 70. An optional take-profit parameter can automatically close trades at a predefined gain.

## Details

- **Entry Criteria**:
  - Squeeze off (Bollinger Bands outside Keltner Channels).
  - **Long**: Momentum < 0 and rising, RSI > 30.
  - **Short**: Momentum > 0 and falling, RSI < 70.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Opposite signal or take-profit if enabled.
- **Stops**: None by default, optional take-profit.
- **Default Values**:
  - `SqueezeLength` = 20
  - `RsiLength` = 14
  - `UseTP` = False
  - `TpPercent` = 1.2
- **Filters**:
  - Category: Volatility breakout
  - Direction: Both
  - Indicators: Bollinger Bands, Keltner Channels, RSI, Linear Regression
  - Stops: Optional
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
