# Multi-timeframe EMA + BB + RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Combines two exponential moving averages, Bollinger Bands and RSI to trade bounces. Long trades occur when price closes above the fast EMA after touching the lower band. Short trades trigger when price closes below the fast EMA after piercing the upper band and RSI is above 50.

Optional profit-taking closes the position after a user-defined number of bars if price moves favorably. The system is flexible enough for swing or intraday trading and supports enabling or disabling long and short sides independently.

## Details

- **Entry Criteria**:
  - **Long**: Close above fast EMA with a low piercing the lower Bollinger Band.
  - **Short**: Close below fast EMA with a high piercing the upper band and RSI > 50.
- **Exit Criteria**:
  - Long: RSI rises above the oversold level.
  - Short: Price closes below the lower band.
- **Indicators**:
  - Two EMAs (periods 10 and 55)
  - Bollinger Bands (length 20, multiplier 2)
  - RSI (length 14, oversold 71)
- **Stops**: Optional profit target after X bars; no fixed stop-loss.
- **Default Values**:
  - `Ma1Period` = 10
  - `Ma2Period` = 55
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `RSILength` = 14
  - `RSIOversold` = 71
  - `XBars` = 12
- **Filters**:
  - Mean reversion with trend filter
  - Timeframe: configurable
  - Indicators: EMA, Bollinger Bands, RSI
  - Stops: optional
  - Complexity: Moderate
