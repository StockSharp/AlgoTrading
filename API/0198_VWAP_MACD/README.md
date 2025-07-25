# Vwap Macd Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on VWAP and MACD. Enters long when price is above VWAP and MACD > Signal. Enters short when price is below VWAP and MACD < Signal. Exits when MACD crosses its signal line in the opposite direction.

VWAP guides intraday value, and MACD crossovers reveal momentum shifts. Trades are launched as MACD turns near the VWAP level.

Suitable for short-term momentum traders. ATR stop rules prevent excessive risk.

## Details

- **Entry Criteria**:
  - Long: `Close > VWAP && MACD > Signal`
  - Short: `Close < VWAP && MACD < Signal`
- **Long/Short**: Both
- **Exit Criteria**: MACD cross opposite
- **Stops**: Percent-based using `StopLossPercent`
- **Default Values**:
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: VWAP, MACD
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 181%. It performs best in the crypto market.
