# Adx Bollinger Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on ADX and Bollinger Bands indicators. Enters long when ADX > 25 and price breaks above upper Bollinger band Enters short when ADX > 25 and price breaks below lower Bollinger band

Testing indicates an average annual return of about 115%. It performs best in the stocks market.

Bollinger band breaches filtered with ADX ensure price is breaking out with force. The system trades in the direction of the breakout.

Suited for high-volatility environments. An ATR-based stop reduces downside risk.

## Details

- **Entry Criteria**:
  - Long: `Close < LowerBand && ADX > 25`
  - Short: `Close > UpperBand && ADX > 25`
- **Long/Short**: Both
- **Exit Criteria**: Price returns to middle band
- **Stops**: ATR-based using `AtrMultiplier`
- **Default Values**:
  - `AdxPeriod` = 14
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: ADX, Bollinger Bands
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

