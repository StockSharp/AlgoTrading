# Bollinger Cci Strategy

Implementation of strategy #162 - Bollinger Bands + CCI. Buy when price
is below lower Bollinger Band and CCI is below -100 (oversold). Sell
when price is above upper Bollinger Band and CCI is above 100
(overbought).

Bollinger bands map volatility limits, and CCI measures the distance from the mean. Breaks beyond a band with CCI confirmation trigger trades.

Suitable for volatile markets where trends extend quickly. ATR stops are applied for safety.

## Details

- **Entry Criteria**:
  - Long: `Close < LowerBand && CCI < CciOversold`
  - Short: `Close > UpperBand && CCI > CciOverbought`
- **Long/Short**: Both
- **Exit Criteria**: Price returns to middle band
- **Stops**: ATR-based using `StopLoss`
- **Default Values**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `CciPeriod` = 20
  - `CciOversold` = -100m
  - `CciOverbought` = 100m
  - `StopLoss` = new Unit(2, UnitTypes.Absolute)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Bollinger Bands, CCI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
