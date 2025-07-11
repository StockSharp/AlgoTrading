# Bollinger Williams R Strategy

Strategy based on Bollinger Bands and Williams %R indicators. Enters
long when price is at lower band and Williams %R is oversold (< -80)
Enters short when price is at upper band and Williams %R is overbought
(> -20)

Bollinger bands expose volatility breakouts and Williams %R ensures momentum is extreme. Positions open when price closes outside a band with a matching Williams %R reading.

Best for volatility expansion traders. ATR stops handle adverse turns.

## Details

- **Entry Criteria**:
  - Long: `Close < LowerBand && WilliamsR < -80`
  - Short: `Close > UpperBand && WilliamsR > -20`
- **Long/Short**: Both
- **Exit Criteria**: Price returns to middle band
- **Stops**: ATR-based using `AtrMultiplier`
- **Default Values**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `WilliamsRPeriod` = 14
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Bollinger Bands, Williams %R, R
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
