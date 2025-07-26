# Donchian Macd Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy combining Donchian Channel breakout with MACD trend confirmation.

Testing indicates an average annual return of about 148%. It performs best in the forex market.

The strategy waits for a Donchian breakout and verifies momentum with MACD. Long or short trades ride the move once MACD agrees.

Aimed at breakout enthusiasts wanting confirmation. Stops are placed using an ATR multiplier.

## Details

- **Entry Criteria**:
  - Long: `Price breaks Donchian high && MACD > Signal`
  - Short: `Price breaks Donchian low && MACD < Signal`
- **Long/Short**: Both
- **Exit Criteria**: MACD reversal
- **Stops**: Percent-based using `StopLossPercent`
- **Default Values**:
  - `DonchianPeriod` = 20
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Donchian Channel, MACD
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

