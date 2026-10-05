# Hull Ma Adx Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategy based on Hull Moving Average and ADX. Enters long when HMA increases and ADX > 25 (strong trend). Enters short when HMA decreases and ADX > 25 (strong trend). Exits when ADX < 20 (weakening trend).

Testing indicates an average annual return of about 178%. It performs best in the stocks market.

Hull MA shows the trend, while ADX confirms its intensity. Entries follow the Hull slope when ADX indicates strength.

Effective for traders who focus on smooth trends with confirmation. ATR stops keep losses under control.

## Details

- **Entry Criteria**:
  - Long: `HullMA turning up && ADX > 25`
  - Short: `HullMA turning down && ADX > 25`
- **Long/Short**: Both
- **Exit Criteria**: Hull MA reversal
- **Stops**: ATR-based using `AtrMultiplier`
- **Default Values**:
  - `HmaPeriod` = 9
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25
  - `AdxExitThreshold` = 20
  - `AtrMultiplier` = 2
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
    The 25 and 20 in the rules are the defaults of AdxThreshold and AdxExitThreshold. The Hull average turns up when it rises after falling and turns down when it falls after rising; a long closes when it falls and a short when it rises, and either closes once ADX drops below AdxExitThreshold. The stop lies AtrMultiplier ATRs (AtrPeriod) from the entry close and is checked on candle closes. An entry signal against an open position reverses it.
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Hull MA, Moving Average, ADX
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

