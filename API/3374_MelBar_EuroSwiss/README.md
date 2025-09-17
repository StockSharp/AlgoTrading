# MelBar EuroSwiss Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The MelBar EuroSwiss strategy reproduces the logic of the "MelBar EuroSwiss M30 500 1.85x 2Y" expert advisor. It combines Bollinger Band breakout entries with an exit filter based on the Relative Vigor Index (RVI). The default template is tuned for the EUR/CHF pair on the M30 timeframe, but the parameters can be optimized for other symbols.

At the start of each finished candle the strategy reads the Bollinger Bands and the RVI values calculated on closing prices. New positions are opened when the current bar opens beyond the envelope while the previous bar opened back inside the channel. This behaviour imitates the gap-style breakout logic of the original MQL5 robot. Long trades use the lower band as the trigger, while short trades react to the upper band. Existing positions are closed when the delayed RVI crosses above or below an absolute level, indicating momentum exhaustion in the direction of the trade. Optional protective orders are set using fixed pip distances.

The default volume is 0.2 lots, but the `TradeVolume` parameter allows fine control over the position size. Both stop loss and take profit are expressed in pips and converted to price offsets through the configurable `PipSize` parameter. The same pip size is reused to arm the protection module at start-up. All calculations rely on finished candles to avoid look-ahead bias.

## Details
- **Entry Criteria**:
  - **Long**: Current candle open < previous lower Bollinger band AND previous candle open > lower band from two candles ago.
  - **Short**: Current candle open > previous upper Bollinger band AND previous candle open < upper band from two candles ago.
- **Exit Criteria**:
  - **Long**: Close when historic RVI value exceeds +`RviLevel`.
  - **Short**: Close when historic RVI value falls below -`RviLevel`.
- **Stops**: Optional fixed stop loss and take profit distances in pips.
- **Indicators**: Bollinger Bands (period `BollingerPeriod`, deviation `BollingerDeviation`) and Relative Vigor Index (`RviPeriod`).
- **Default Values**:
  - `TradeVolume` = 0.2 lots
  - `BollingerPeriod` = 18
  - `BollingerDeviation` = 2.75
  - `RviPeriod` = 15
  - `RviLevel` = 0.30
  - `StopLossPips` = 13
  - `TakeProfitPips` = 61
  - `PipSize` = 0.0001
  - `CandleType` = TimeSpan.FromMinutes(30)
- **Other Notes**:
  - Category: Breakout reversal
  - Direction: Both long and short
  - Timeframe: Intraday (M30 by default)
  - Risk Level: Medium due to fixed pip-based risk controls
  - Trailing stop: Not enabled by default (can be implemented externally)

The provided parameters mirror the original configuration and serve as a solid starting point for walk-forward tests or optimization runs in StockSharp.
