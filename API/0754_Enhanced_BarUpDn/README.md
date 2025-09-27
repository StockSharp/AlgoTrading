# Enhanced BarUpDn Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy looks for bullish or bearish bars combined with Bollinger Bands and trend confirmation. It enters long on bullish gaps in uptrends and short on bearish gaps in downtrends. Exits use ATR-based stop-loss and take-profit levels.

## Details

- **Entry Criteria**:
  - Long: bullish candle with gap up, close above trend MA and above lower Bollinger band.
  - Short: bearish candle with gap down, close below trend MA and below upper Bollinger band.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Price touches ATR-based stop-loss or take-profit (1.5× ATR).
- **Stops**: ATR-based stop and take-profit.
- **Default Values**:
  - `BbLength` = 20
  - `BbMultiplier` = 2
  - `MaLength` = 50
  - `AtrLength` = 14
  - `AtrMultiplierSl` = 2
  - `AtrMultiplierTp` = 3
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Bollinger Bands, SMA, ATR
  - Stops: Yes
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
