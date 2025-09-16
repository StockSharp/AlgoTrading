# Basic Trailing Stop
[Русский](README_ru.md) | [中文](README_cn.md)

The Basic Trailing Stop strategy combines Commodity Channel Index (CCI) and Relative Strength Index (RSI) filters with a simple trailing stop. When both indicators signal oversold or overbought conditions, the strategy opens a market position and immediately places a trailing stop measured in pips. As price moves favorably, the stop level follows the trend to lock in profits.

Testing indicates an average annual return of about 32%. It performs best in the forex market.

Because the stop level continuously trails price, risk automatically tightens when the trend extends. Exits occur only if the trailing stop is hit. The system maintains one position at a time and can trade in both directions.

## Details

- **Entry Criteria**:
  - **Long**: `CCI` between -150 and -100 and `RSI` between 0 and 30.
  - **Short**: `CCI` between 100 and 250 and `RSI` between 70 and 100.
- **Long/Short**: Both.
- **Exit Criteria**: Trailing stop hit.
- **Stops**: Trailing stop only.
- **Default Values**:
  - `StopLossPips` = 20
  - `CciPeriod` = 14
  - `RsiPeriod` = 14
  - `CandleType` = `TimeSpan.FromMinutes(1)`
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: CCI, RSI
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

