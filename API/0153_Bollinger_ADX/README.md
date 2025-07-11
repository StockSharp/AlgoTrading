# Bollinger Adx Strategy

Strategy combining Bollinger Bands and ADX indicators. Looks for breakouts with strong trend confirmation.

Price movements outside Bollinger bands are filtered through ADX for strength. Trades engage when a band break coincides with high ADX.

Useful for volatility surges accompanied by strong trends. Stop size is driven by ATR.

## Details

- **Entry Criteria**:
  - Long: `Close < LowerBand && ADX > AdxThreshold`
  - Short: `Close > UpperBand && ADX > AdxThreshold`
- **Long/Short**: Both
- **Exit Criteria**:
  - Bollinger mean reversion
- **Stops**: ATR-based using `AtrMultiplier`
- **Default Values**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Bollinger Bands, ADX
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
