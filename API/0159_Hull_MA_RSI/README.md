# Hull Ma Rsi Strategy

Implementation of strategy #159 - Hull Moving Average + RSI. Buy when
HMA is rising and RSI is below 30 (oversold). Sell when HMA is falling
and RSI is above 70 (overbought).

Hull MA provides a smoothed trend line and RSI highlights momentum divergences. Trades occur when RSI turns at extremes while price follows the Hull direction.

Suited to short-term swing traders who want early signals. ATR-based stops protect the trade.

## Details

- **Entry Criteria**:
  - Long: `HullMA turning up && RSI < RsiOversold`
  - Short: `HullMA turning down && RSI > RsiOverbought`
- **Long/Short**: Both
- **Exit Criteria**:
  - Hull MA change of direction
- **Stops**: ATR-based using `StopLoss`
- **Default Values**:
  - `HmaPeriod` = 9
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `StopLoss` = new Unit(2, UnitTypes.Absolute)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Hull MA, Moving Average, RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
