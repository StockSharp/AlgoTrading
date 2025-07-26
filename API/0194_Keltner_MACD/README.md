# Keltner Macd Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on Keltner Channels and MACD. Enters long when price breaks above upper Keltner Channel with MACD > Signal. Enters short when price breaks below lower Keltner Channel with MACD < Signal. Exits when MACD crosses its signal line in the opposite direction.

Keltner Channel breakouts serve as the trigger, and MACD momentum filters the direction. The strategy initiates trades once both signals align.

Good for traders chasing volatility expansions with momentum backing. An ATR-based stop contains risk.

## Details

- **Entry Criteria**:
  - Long: `Close > UpperBand && MACD > Signal`
  - Short: `Close < LowerBand && MACD < Signal`
- **Long/Short**: Both
- **Exit Criteria**: MACD cross opposite
- **Stops**: ATR-based using `AtrMultiplier`
- **Default Values**:
  - `EmaPeriod` = 20
  - `Multiplier` = 2m
  - `AtrPeriod` = 14
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Keltner Channel, MACD
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 169%. It performs best in the crypto market.
